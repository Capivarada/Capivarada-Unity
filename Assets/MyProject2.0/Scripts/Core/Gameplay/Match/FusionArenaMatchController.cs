using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Scene-networked match gate for the online arena. Add it to a scene NetworkObject.
/// In Shared mode the current Shared Master owns the countdown; if it disconnects,
/// Fusion transfers the scene object's state authority and the next master continues.
/// This deliberately awards nothing: final rewards come only from the dedicated server.
/// </summary>
public sealed class FusionArenaMatchController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField, Min(1)] private int requiredSlots = 8;
    [SerializeField, Min(0f)] private float waitForPlayersSeconds = 10f;
    [SerializeField, Min(30f)] private float maxActiveWithoutHumansSeconds = 120f;
    [SerializeField] private bool fillVacantSlotsWithBots = true;
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private MatchConfig matchConfig;

    [Networked] private TickTimer StartTimer { get; set; }
    [Networked] private NetworkBool MatchStarted { get; set; }
    [Networked] private int BotSlots { get; set; }
    private bool readyPublished;
    private bool networkSpawned;
    private bool finishAfterDepartures;
    private bool humanJoined;
    private float activeWithoutHumansSince;

    public event Action<int, int> WaitingChanged;
    public event Action<int> BotsFilled;
    public event Action MatchReady;
    public bool IsNetworkSpawned => networkSpawned;
    public bool IsMatchReady => networkSpawned && MatchStarted;
    public int RequiredSlots => requiredSlots;
    public int CurrentHumanSlots => Runner == null ? 0 : CountHumans();
    public int CurrentBotSlots => networkSpawned ? BotSlots : 0;
    public PlayerConfig PlayerConfig => playerConfig;
    public MatchConfig MatchConfig => matchConfig;
    public int CurrentParticipantCount => networkSpawned && MatchStarted
        ? CurrentHumanSlots + BotSlots
        : CurrentHumanSlots;
    public float DeathHeight => matchConfig != null ? matchConfig.DeathHeight : 2.87f;
    public float RemainingWaitSeconds
    {
        get
        {
            if (!networkSpawned || Runner == null || !StartTimer.IsRunning || MatchStarted)
                return 0f;

            return StartTimer.RemainingTime(Runner) ?? 0f;
        }
    }

    public override void Spawned()
    {
        networkSpawned = true;
        activeWithoutHumansSince = Runner != null ? (float)Runner.SimulationTime : 0f;
        PublishWaiting();
        StartCountdownWhenFirstHumanJoins();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // PlayerLeft callbacks are delivered to several scene behaviours. Let
        // the player spawner record a departed result in the same tick before
        // settlement closes the match after its final human disconnects.
        if (MatchStarted)
        {
            if (finishAfterDepartures && CountHumans() == 0)
                FindFirstObjectByType<FusionRoundController>()?.FinishWhenAbandoned();
            return;
        }

        // A requested MPS server is Active even when the joining client never
        // reaches Fusion. Do not bill that orphan allocation indefinitely.
        if (Application.isBatchMode && !humanJoined && CountHumans() == 0 &&
            FindFirstObjectByType<FusionNetworkPlayer>() == null && Runner != null &&
            Runner.SimulationTime - activeWithoutHumansSince >= maxActiveWithoutHumansSeconds)
        {
            Debug.LogWarning("[FusionArenaMatchController] Nenhum humano entrou no tempo limite; encerrando servidor dedicado.");
            Application.Quit();
            return;
        }

        StartCountdownWhenFirstHumanJoins();
        if (CountHumans() >= requiredSlots || (StartTimer.IsRunning && StartTimer.Expired(Runner))) BeginMatch();
    }

    // Fusion 2.1 invokes these exact interface names (without the On prefix).
    public void PlayerJoined(PlayerRef player)
    {
        // The player-joined callback is authoritative proof that this MPS
        // allocation served a real connection. Never apply the orphan-server
        // timeout afterwards, even while Fusion is transitioning scenes.
        if (Object.HasStateAuthority)
            humanJoined = true;
        PublishWaiting();
        StartCountdownWhenFirstHumanJoins();
        if (Object.HasStateAuthority && CountHumans() >= requiredSlots && !MatchStarted) BeginMatch();
    }

    public void PlayerLeft(PlayerRef player)
    {
        PublishWaiting();
        if (!Object.HasStateAuthority || !MatchStarted || CountHumans() > 0)
            return;
        finishAfterDepartures = true;
    }

    private void BeginMatch()
    {
        if (MatchStarted) return;
        int humans = CountHumans();
        BotSlots = fillVacantSlotsWithBots ? Mathf.Max(0, requiredSlots - humans) : 0;
        MatchStarted = true;
        Debug.Log($"[FusionArenaMatchController] Match ready: {humans} humans, {BotSlots} bots.");
    }

    private void StartCountdownWhenFirstHumanJoins()
    {
        if (!Object.HasStateAuthority || MatchStarted || StartTimer.IsRunning)
            return;

        if (CountHumans() > 0)
            StartTimer = TickTimer.CreateFromSeconds(Runner, waitForPlayersSeconds);
    }

    public override void Render()
    {
        PublishWaiting();
        if (MatchStarted && !readyPublished)
        {
            readyPublished = true;
            BotsFilled?.Invoke(BotSlots);
            MatchReady?.Invoke();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        networkSpawned = false;
    }

    private int CountHumans()
    {
        int count = 0;
        foreach (PlayerRef _ in Runner.ActivePlayers) count++;
        return count;
    }

    private void PublishWaiting() => WaitingChanged?.Invoke(CurrentHumanSlots, requiredSlots);

    /// <summary>Returns the replicated elimination total used by the online HUD.</summary>
    public int GetEliminatedCount()
    {
        int eliminated = 0;
        foreach (FusionNetworkPlayer player in FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (player.IsEliminated) eliminated++;
        foreach (FusionNetworkBot bot in FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (bot.IsEliminated) eliminated++;
        return eliminated;
    }
}
