using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

public class PlayFabMatchManager : MonoBehaviour
{
    public static PlayFabMatchManager Instance { get; private set; }

    public event Action<MatchProcessResult> OnMatchProcessed;
    public event Action<string> OnMatchProcessError;

    private bool processingMatch;
    private string activeMatchId;
    private readonly Queue<MatchEvent> pendingEvents = new();

    // Resultado da ÚLTIMA partida.
    // É apenas memória/runtime. Não é salvo localmente.
    public MatchProcessResult LastMatchResult { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BeginMatch()
    {
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsGuest) return;
        if (PlayFabAuthManager.Instance == null || !PlayFabAuthManager.Instance.IsLoggedIn)
        {
            OnMatchProcessError?.Invoke("PlayFab não está conectado.");
            return;
        }
        if (!string.IsNullOrEmpty(activeMatchId)) return;
        activeMatchId = Guid.NewGuid().ToString("N");
        pendingEvents.Enqueue(new MatchEvent(MatchEventType.Start));
        ProcessNextEvent();
    }

    public void RecordKill() => QueueProgress(MatchEventType.Kill);
    public void RecordRoundSurvived() => QueueProgress(MatchEventType.Round);

    private void QueueProgress(MatchEventType type)
    {
        if (string.IsNullOrEmpty(activeMatchId)) return;
        pendingEvents.Enqueue(new MatchEvent(type));
        ProcessNextEvent();
    }

    public void CompleteMatch(int placement, bool won, int durationSeconds)
    {
    if (PlayFabAuthManager.Instance != null &&
        PlayFabAuthManager.Instance.IsGuest)
    {
        Debug.Log(
            "[PlayFabMatchManager] " +
            "Visitante: resultado descartado. Nenhum dado foi salvo."
        );

        OnMatchProcessed?.Invoke(null);
        return;
    }

    if (PlayFabAuthManager.Instance == null ||
        !PlayFabAuthManager.Instance.IsLoggedIn)
    {
        Debug.LogError(
            "[PlayFabMatchManager] PlayFab não está conectado."
        );

        OnMatchProcessError?.Invoke(
            "PlayFab não está conectado."
        );

        return;
    }

        if (string.IsNullOrEmpty(activeMatchId)) BeginMatch();
        pendingEvents.Enqueue(new MatchEvent(MatchEventType.Finish, placement, won, durationSeconds));
        ProcessNextEvent();
    }

    private void ProcessNextEvent()
    {
        if (processingMatch || pendingEvents.Count == 0) return;
        if (string.IsNullOrEmpty(activeMatchId)) return;
        MatchEvent next = pendingEvents.Peek();
        processingMatch = true;
        string functionName = next.Type == MatchEventType.Start ? "StartMatchTracking" :
            next.Type == MatchEventType.Finish ? "ProcessMatchResult" : "RecordMatchEvent";
        object parameters = next.Type == MatchEventType.Start ? new { matchId = activeMatchId } :
            next.Type == MatchEventType.Finish ? new { matchId = activeMatchId, placement = next.Placement, won = next.Won, durationSeconds = Mathf.Clamp(next.DurationSeconds, 0, 14400) } :
            new { matchId = activeMatchId, type = next.Type == MatchEventType.Kill ? "kill" : "round" };
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            FunctionName = functionName,
            FunctionParameter = parameters,
            GeneratePlayStreamEvent = true
        }, result => OnEventSuccess(next, result), OnCloudScriptError);
    }

    private void OnEventSuccess(MatchEvent completed, ExecuteCloudScriptResult response)
    {
        try
        {
            pendingEvents.Dequeue();
            processingMatch = false;
            if (completed.Type == MatchEventType.Finish)
            {
                LastMatchResult = ParseFunctionResult(response.FunctionResult);
                activeMatchId = null;
                OnMatchProcessed?.Invoke(LastMatchResult);
            }
            ProcessNextEvent();
        }
        catch (Exception e)
        {
            processingMatch = false;

            Debug.LogError(
                "[PlayFabMatchManager] " +
                "Erro ao interpretar resultado do CloudScript: " +
                e
            );

            OnMatchProcessError?.Invoke(
                "Resultado recebido, mas não foi possível interpretá-lo."
            );
        }
    }

    private MatchProcessResult ParseFunctionResult(object functionResult)
    {
        if (functionResult == null)
            throw new Exception("FunctionResult é NULL.");

        if (functionResult is not JsonObject data)
        {
            throw new Exception(
                $"FunctionResult inválido. Tipo recebido: {functionResult.GetType()}"
            );
        }

        var result = new MatchProcessResult();

        // =====================================================
        // RESULTADO BÁSICO
        // =====================================================

        result.Success = GetBool(data, "success");
        result.Won = GetBool(data, "won");
        result.Placement = GetInt(data, "placement");


        // =====================================================
        // XP
        // =====================================================

        JsonObject xp = GetObject(data, "xp");

result.PlacementXP = GetInt(xp, "placement");
result.KillsXP = GetInt(xp, "kills");
result.RoundsXP = GetInt(xp, "rounds");

result.XPEarned = GetInt(xp, "earned");

result.PreviousXP = GetInt(xp, "previous");
result.TotalXP = GetInt(xp, "total");

result.XPRequired = GetInt(xp, "required");


        // =====================================================
        // LEVEL
        // =====================================================

        JsonObject level = GetObject(data, "level");

        result.PreviousLevel = GetInt(level, "previous");
        result.CurrentLevel = GetInt(level, "current");

// =====================================================
// DADOS DA PARTIDA
// =====================================================

JsonObject match = GetObject(data, "match");

result.Kills = GetInt(match, "kills");
result.RoundsSurvived = GetInt(match, "roundsSurvived");


// =====================================================
// MOEDAS
// =====================================================

JsonObject coins = GetObject(data, "coins");

result.MatchCoins = GetInt(coins, "match");
result.VictoryBonusCoins = GetInt(coins, "victoryBonus");
result.LevelUpCoins = GetInt(coins, "levelUp");
result.TotalCoinsEarned = GetInt(coins, "total");
result.PointsBalance = GetInt(coins, "points");

        // =====================================================
        // ESTATÍSTICAS
        // =====================================================

        JsonObject statistics = GetObject(data, "statistics");

        result.Matches = GetInt(statistics, "matches");
        result.Eliminations = GetInt(statistics, "eliminations");
        result.Wins = GetInt(statistics, "wins");
        result.PlayTimeSeconds = GetInt(statistics, "playTimeSeconds");


        return result;
    }


    private JsonObject GetObject(
        JsonObject data,
        string key)
    {
        if (!data.TryGetValue(key, out object value))
        {
            throw new Exception(
                $"Campo '{key}' não encontrado."
            );
        }

        if (value is JsonObject jsonObject)
            return jsonObject;

        throw new Exception(
            $"Campo '{key}' não é um JsonObject."
        );
    }


    private int GetInt(
        JsonObject data,
        string key)
    {
        if (!data.TryGetValue(key, out object value) ||
            value == null)
        {
            return 0;
        }

        return Convert.ToInt32(value);
    }


    private bool GetBool(
        JsonObject data,
        string key)
    {
        if (!data.TryGetValue(key, out object value) ||
            value == null)
        {
            return false;
        }

        return Convert.ToBoolean(value);
    }


    private void OnCloudScriptError(
        PlayFabError error)
    {
        processingMatch = false;

        string message =
            error.GenerateErrorReport();

        Debug.LogError(
            "[PlayFabMatchManager] " +
            message
        );

        OnMatchProcessError?.Invoke(message);
    }

    private enum MatchEventType { Start, Kill, Round, Finish }
    private readonly struct MatchEvent
    {
        public readonly MatchEventType Type; public readonly int Placement; public readonly bool Won; public readonly int DurationSeconds;
        public MatchEvent(MatchEventType type, int placement = 0, bool won = false, int durationSeconds = 0)
        { Type = type; Placement = placement; Won = won; DurationSeconds = durationSeconds; }
    }
}

[Serializable]
public class MatchProcessResult
{
    // ==========================================
    // RESULTADO
    // ==========================================

    public bool Success;
    public bool Won;

    public int Placement;


    // ==========================================
    // XP
    // ==========================================

    public int PlacementXP;
    public int KillsXP;
    public int RoundsXP;

    // XP ganho nesta partida
    public int XPEarned;

    // XP que o jogador tinha antes da partida
    public int PreviousXP;

    // XP acumulado depois da partida
    public int TotalXP;

    // XP necessário para completar um nível
    public int XPRequired;


    // ==========================================
    // LEVEL
    // ==========================================

    public int PreviousLevel;
    public int CurrentLevel;


    // ==========================================
    // MATCH DATA
    // ==========================================

    public int Kills;
    public int RoundsSurvived;


    // ==========================================
    // COINS
    // ==========================================

    public int MatchCoins;
    public int VictoryBonusCoins;
    public int LevelUpCoins;
    public int TotalCoinsEarned;
    public int PointsBalance;


    // ==========================================
    // STATISTICS
    // ==========================================

    public int Matches;
    public int Eliminations;
    public int Wins;
    public int PlayTimeSeconds;
}
