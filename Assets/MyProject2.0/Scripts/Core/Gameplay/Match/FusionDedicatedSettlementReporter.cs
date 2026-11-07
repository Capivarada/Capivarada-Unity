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
    [SerializeField] private string backendBaseUrl = "https://capivarada-functions-dev-d9djf2f2dqe4b9hu.westus2-01.azurewebsites.net/api";
    [SerializeField, Min(5)] private int requestTimeoutSeconds = 20;

    private bool submitted;
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

        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        runner = FindFirstObjectByType<NetworkRunner>();
        if (rounds == null || runner == null)
        {
            Debug.LogError("[FusionDedicatedSettlementReporter] Configure FusionRoundController e NetworkRunner no servidor.", this);
            enabled = false;
            return;
        }
        rounds.MatchFinished += Submit;
    }

    private void OnDestroy()
    {
        if (rounds != null) rounds.MatchFinished -= Submit;
    }

    private void Submit(IReadOnlyList<FusionMatchResult> results, int durationSeconds)
    {
        if (submitted || runner == null || !runner.IsServer) return;
        string secret = Environment.GetEnvironmentVariable("CAPIVARADA_MATCH_SERVER_SECRET");
        string matchId = runner.SessionInfo.Name;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedSettlementReporter] Sem CAPIVARADA_MATCH_SERVER_SECRET ou MatchId; progresso não será salvo.", this);
            return;
        }

        SettlementPayload payload = new SettlementPayload(matchId, durationSeconds, results);
        submitted = true;
        StartCoroutine(Post(payload, secret));
    }

    private IEnumerator Post(SettlementPayload payload, string secret)
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

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FusionDedicatedSettlementReporter] Settlement falhou ({request.responseCode}): {request.downloadHandler.text}", this);
            submitted = false;
            yield break;
        }

        Debug.Log($"[FusionDedicatedSettlementReporter] Progresso liquidado: {request.downloadHandler.text}", this);
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
