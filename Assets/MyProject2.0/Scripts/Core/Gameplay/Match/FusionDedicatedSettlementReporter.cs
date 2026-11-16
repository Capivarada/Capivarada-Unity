using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Server-only bridge from the authoritative Fusion match to Azure settlement.
/// Attach it to the GameManager in the dedicated-server scene. It intentionally
/// does nothing in a client, the Shared prototype, Practice, or the Editor.
/// </summary>
public sealed class FusionDedicatedSettlementReporter : MonoBehaviour
{
    [SerializeField] private FusionRoundController rounds;
    [SerializeField] private FusionArenaMatchController arena;
    [SerializeField] private string backendBaseUrl = "https://capivarada-functions-dev-d9djf2f2dqe4b9hu.westus2-01.azurewebsites.net/api";
    [SerializeField, Min(5)] private int requestTimeoutSeconds = 20;
    [SerializeField, Min(0)] private float shutdownDelayAfterSettlementSeconds = 2f;
    [SerializeField, Min(1)] private int settlementAttempts = 3;
    [SerializeField, Min(.5f)] private float settlementRetryDelaySeconds = 2f;

    private bool submitted;
    private bool startReported;
    private bool shutdownScheduled;
    private bool bound;
    private NetworkRunner runner;

    private void Start()
    {
        // A normal Unity player/client must never know the server secret or
        // perform settlement. MPS dedicated builds run in batch mode.
        if (!Application.isBatchMode)
        {
            enabled = false;
            return;
        }

        StartCoroutine(BindAfterGameSceneLoads());
    }

    private IEnumerator BindAfterGameSceneLoads()
    {
        // The dedicated bootstrap starts in Boot_Scene, while rounds exist in
        // Game_Scene. Do not permanently disable settlement during that short
        // scene transition.
        for (int frame = 0; frame < 900 && !bound; frame++)
        {
            if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
            if (arena == null) arena = FindFirstObjectByType<FusionArenaMatchController>();
            if (runner == null) runner = FindFirstObjectByType<NetworkRunner>();
            if (rounds != null && runner != null)
            {
                bound = true;
                rounds.MatchFinished += Submit;
                if (arena != null)
                {
                    arena.MatchReady += ReportStarted;
                    if (arena.IsMatchReady) ReportStarted();
                }
                Debug.Log("[FusionDedicatedSettlementReporter] Settlement ligado ao servidor dedicado.", this);
                yield break;
            }
            yield return null;
        }
        Debug.LogError("[FusionDedicatedSettlementReporter] FusionRoundController ou NetworkRunner não apareceu após carregar Game_Scene; settlement indisponível.", this);
    }

    private void OnDestroy()
    {
        if (rounds != null) rounds.MatchFinished -= Submit;
        if (arena != null) arena.MatchReady -= ReportStarted;
    }

    private void ReportStarted()
    {
        if (startReported || runner == null || !runner.IsServer) return;
        string secret = FirstConfiguredEnvironmentValue(
            "PF_MPS_SECRET_CAPIVARADA-MATCH-SERVER-SECRET",
            "PF_MPS_SECRET_CAPIVARADA_MATCH_SERVER_SECRET",
            "CAPIVARADA_MATCH_SERVER_SECRET");
        string matchId = runner.SessionInfo.Name;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedSettlementReporter] Sem segredo ou MatchId; partida não foi fechada para novas entradas.", this);
            return;
        }

        startReported = true;
        StartCoroutine(PostStarted(matchId, secret));
    }

    private IEnumerator PostStarted(string matchId, string secret)
    {
        using UnityWebRequest request = new UnityWebRequest(backendBaseUrl.TrimEnd('/') + "/matches/" + UnityWebRequest.EscapeURL(matchId) + "/started", UnityWebRequest.kHttpVerbPOST)
        {
            timeout = Mathf.Max(5, requestTimeoutSeconds),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("X-Capivarada-Match-Secret", secret);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            startReported = false;
            Debug.LogError($"[FusionDedicatedSettlementReporter] Não foi possível fechar a partida para novas entradas ({request.responseCode}): {request.downloadHandler.text}", this);
        }
    }

    private void Submit(IReadOnlyList<FusionMatchResult> results, int durationSeconds)
    {
        if (submitted || runner == null || !runner.IsServer) return;
        // Locally we use the familiar variable name. In PlayFab MPS, a build
        // secret named CAPIVARADA-MATCH-SERVER-SECRET is exposed with the
        // PF_MPS_SECRET_ prefix, preserving the hyphens in its original name.
        string secret = FirstConfiguredEnvironmentValue(
            // The name after PF_MPS_SECRET_ is the exact MPS secret name.
            "PF_MPS_SECRET_CAPIVARADA-MATCH-SERVER-SECRET",
            // Supports builds whose MPS secret was named with underscores.
            "PF_MPS_SECRET_CAPIVARADA_MATCH_SERVER_SECRET",
            "CAPIVARADA_MATCH_SERVER_SECRET");
        string matchId = runner.SessionInfo.Name;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedSettlementReporter] Sem segredo do MPS/local ou MatchId; progresso não será salvo.", this);
            ScheduleShutdownAfterFailedSettlement();
            return;
        }

        SettlementPayload payload = new SettlementPayload(matchId, durationSeconds, results);
        Debug.Log($"[FusionDedicatedSettlementReporter] Enviando settlement: MatchId={matchId}, humanos={payload.participants.Count}, duração={durationSeconds}s.", this);
        submitted = true;
        StartCoroutine(PostWithRetries(payload, secret));
    }

    private IEnumerator PostWithRetries(SettlementPayload payload, string secret)
    {
        int attempts = Mathf.Max(1, settlementAttempts);
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(backendBaseUrl.TrimEnd('/') + "/matches/settle", UnityWebRequest.kHttpVerbPOST)
            {
                timeout = Mathf.Max(5, requestTimeoutSeconds),
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("X-Capivarada-Match-Secret", secret);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[FusionDedicatedSettlementReporter] Progresso liquidado: {request.downloadHandler.text}", this);
                // An Active MPS process cannot return to StandingBy. Exiting after the
                // authoritative settlement lets MPS replace it with a fresh standby
                // process instead of billing an idle, already-finished allocation.
                yield return new WaitForSeconds(shutdownDelayAfterSettlementSeconds);
                Debug.Log("[FusionDedicatedSettlementReporter] Partida concluída; encerrando processo dedicado.", this);
                Application.Quit();
                yield break;
            }

            Debug.LogWarning($"[FusionDedicatedSettlementReporter] Settlement tentativa {attempt}/{attempts} falhou ({request.responseCode}): {request.downloadHandler.text}", this);
            if (attempt < attempts)
                yield return new WaitForSeconds(settlementRetryDelaySeconds);
        }
        // Do not retain an already-finished MPS allocation forever when a
        // backend/secret outage persists. Settlement is idempotent per player,
        // so an MPS retry never duplicates rewards.
        ScheduleShutdownAfterFailedSettlement();
    }

    private void ScheduleShutdownAfterFailedSettlement()
    {
        if (shutdownScheduled) return;
        shutdownScheduled = true;
        StartCoroutine(ShutdownAfterFailedSettlement());
    }

    private IEnumerator ShutdownAfterFailedSettlement()
    {
        yield return new WaitForSeconds(shutdownDelayAfterSettlementSeconds);
        Debug.LogError("[FusionDedicatedSettlementReporter] Encerrando processo após falha de settlement para liberar a alocação MPS.", this);
        Application.Quit();
    }

    private static string FirstConfiguredEnvironmentValue(
        params string[] variableNames)
    {
        foreach (string variableName in variableNames)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    [Serializable]
    private sealed class SettlementPayload
    {
        public string matchId;
        public int durationSeconds;
        public List<SettlementParticipant> participants = new();

        public SettlementPayload(string matchId, int durationSeconds, IReadOnlyList<FusionMatchResult> results)
        {
            this.matchId = matchId;
            this.durationSeconds = durationSeconds;
            if (results == null) return;
            foreach (FusionMatchResult result in results)
            {
                // Bots participate in the arena result but never own a PlayFab
                // entity or a join token. Sending several empty entity IDs made
                // the backend reject the whole batch as duplicated players,
                // including the legitimate human settlement.
                if (result.IsBot)
                    continue;

                participants.Add(new SettlementParticipant
                {
                    entityId = result.PlayerId,
                    entityType = "title_player_account",
                    joinToken = result.JoinToken,
                    nickname = result.Nickname,
                    avatarId = result.AvatarId,
                    eliminations = result.Eliminations,
                    roundsSurvived = result.RoundsSurvived,
                    placement = result.Placement,
                    isBot = result.IsBot,
                    rewardEligible = result.CanPersist
                });
            }
        }
    }

    [Serializable]
    private sealed class SettlementParticipant
    {
        public string entityId;
        public string entityType;
        public string joinToken;
        public string nickname;
        public string avatarId;
        public int eliminations;
        public int roundsSurvived;
        public int placement;
        public bool isBot;
        public bool rewardEligible;
    }
}
