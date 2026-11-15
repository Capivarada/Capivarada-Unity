using System;
using System.Collections.Generic;
using UnityEngine;

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

        // Results are settled by Fusion MPS through Azure /matches/settle.
        // This legacy component remains only so existing scenes keep their
        // serialized reference while they migrate; it must never send a
        // client-authoritative CloudScript event.
        pendingEvents.Dequeue();
        processingMatch = false;
        if (next.Type == MatchEventType.Finish)
        {
            LastMatchResult = null;
            activeMatchId = null;
            OnMatchProcessed?.Invoke(null);
        }
        ProcessNextEvent();
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
}
