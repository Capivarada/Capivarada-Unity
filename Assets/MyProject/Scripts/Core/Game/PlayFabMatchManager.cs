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

   public void SendMatchResult(
    int placement,
    int kills,
    int roundsSurvived,
    bool won)
{
    if (PlayFabAuthManager.Instance != null &&
        PlayFabAuthManager.Instance.IsGuest)
    {
        Debug.Log(
            "[PlayFabMatchManager] " +
            "Visitante: resultado descartado. Nenhum dado foi salvo."
        );

    OnMatchProcessed.Invoke(null);
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

    // ================================================
    // PLAYFAB
    // ================================================

    if (!PlayFabAuthManager.Instance.IsLoggedIn)
    {
        Debug.LogError(
            "[PlayFabMatchManager] PlayFab não está conectado."
        );

        OnMatchProcessError?.Invoke(
            "PlayFab não está conectado."
        );

        return;
    }

    processingMatch = true;

    var request = new ExecuteCloudScriptRequest
    {
        FunctionName = "ProcessMatchResult",

        FunctionParameter = new
        {
            placement = placement,
            kills = kills,
            roundsSurvived = roundsSurvived,
            won = won
        },

        GeneratePlayStreamEvent = true
    };

    PlayFabClientAPI.ExecuteCloudScript(
        request,
        OnCloudScriptSuccess,
        OnCloudScriptError
    );
}


    private void OnCloudScriptSuccess(
        ExecuteCloudScriptResult result)
    {
        Debug.Log(
            "[PlayFabMatchManager] FunctionResult recebido:\n" +
            (result.FunctionResult == null
                ? "NULL"
                : result.FunctionResult.ToString())
        );

        try
        {
            LastMatchResult =
                ParseFunctionResult(result.FunctionResult);

            processingMatch = false;

            Debug.Log(
                "[PlayFabMatchManager] Partida salva com sucesso!"
            );

            OnMatchProcessed?.Invoke(
                LastMatchResult
            );
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

        // =====================================================
        // ESTATÍSTICAS
        // =====================================================

        JsonObject statistics = GetObject(data, "statistics");

        result.Matches = GetInt(statistics, "matches");
        result.Eliminations = GetInt(statistics, "eliminations");
        result.Wins = GetInt(statistics, "wins");


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


    // ==========================================
    // STATISTICS
    // ==========================================

    public int Matches;
    public int Eliminations;
    public int Wins;
}