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
    [SerializeField] private bool fillVacantSlotsWithBots = true;

    [Networked] private TickTimer StartTimer { get; set; }
    [Networked] private NetworkBool MatchStarted { get; set; }
    [Networked] private int BotSlots { get; set; }
    private bool readyPublished;

    public event Action<int, int> WaitingChanged;
    public event Action<int> BotsFilled;
    public event Action MatchReady;
    public bool IsMatchReady => MatchStarted;
    public int RequiredSlots => requiredSlots;
    public int CurrentHumanSlots => Runner == null ? 0 : CountHumans();
    public int CurrentBotSlots => BotSlots;

    public override void Spawned()
    {
        PublishWaiting();
        if (Object.HasStateAuthority && !StartTimer.IsRunning && !MatchStarted)
            StartTimer = TickTimer.CreateFromSeconds(Runner, waitForPlayersSeconds);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || MatchStarted) return;
        if (CountHumans() >= requiredSlots || (StartTimer.IsRunning && StartTimer.Expired(Runner))) BeginMatch();
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        PublishWaiting();
        if (Object.HasStateAuthority && CountHumans() >= requiredSlots && !MatchStarted) BeginMatch();
    }

    public void OnPlayerLeft(PlayerRef player) => PublishWaiting();

    private void BeginMatch()
    {
        if (MatchStarted) return;
        int humans = CountHumans();
        BotSlots = fillVacantSlotsWithBots ? Mathf.Max(0, requiredSlots - humans) : 0;
        MatchStarted = true;
        Debug.Log($"[FusionArenaMatchController] Match ready: {humans} humans, {BotSlots} bots.");
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

    private int CountHumans()
    {
        int count = 0;
        foreach (PlayerRef _ in Runner.ActivePlayers) count++;
        return count;
    }

    private void PublishWaiting() => WaitingChanged?.Invoke(CurrentHumanSlots, requiredSlots);
}
