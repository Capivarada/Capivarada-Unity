using System;
using Fusion;
using UnityEngine;

public enum FusionRoundPhase
{
    Waiting,
    Countdown,
    Choosing,
    Revealing,
    Moving,
    Intermission,
    Finished
}

/// <summary>
/// Replicated round clock for the online arena. The scene object state authority
/// owns phase changes; every peer reads the exact same phase and round number.
/// It deliberately does not settle progression or rewards.
/// </summary>
public sealed class FusionRoundController : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float startCountdownSeconds = 3f;
    [SerializeField, Min(0.1f)] private float choosingSeconds = 8f;
    [SerializeField, Min(0f)] private float revealSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float movementSeconds = 8f;
    [SerializeField, Min(0f)] private float intermissionSeconds = 2f;
    [SerializeField] private FusionArenaMatchController match;

    [Networked] public FusionRoundPhase Phase { get; private set; }
    [Networked] public int RoundIndex { get; private set; }
    [Networked] private TickTimer PhaseTimer { get; set; }

    private FusionRoundPhase displayedPhase;
    private int displayedRound;
    private bool networkSpawned;
    public event Action<FusionRoundPhase, int> PhaseChanged;
    public bool IsNetworkSpawned => networkSpawned;
    public float RemainingPhaseSeconds => !networkSpawned || Runner == null || !PhaseTimer.IsRunning
        ? 0f
        : PhaseTimer.RemainingTime(Runner) ?? 0f;
    public float RevealDuration => match != null && match.MatchConfig != null
        ? match.MatchConfig.RevealDuration
        : revealSeconds;

    public override void Spawned()
    {
        networkSpawned = true;
        if (match == null) match = GetComponent<FusionArenaMatchController>();
        displayedPhase = Phase;
        displayedRound = RoundIndex;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || match == null || Phase == FusionRoundPhase.Finished) return;
        if (!match.IsMatchReady) return;

        if (Phase == FusionRoundPhase.Waiting)
        {
            SetPhase(FusionRoundPhase.Countdown, InitialCountdownSeconds);
            return;
        }

        if (!PhaseTimer.IsRunning || !PhaseTimer.Expired(Runner)) return;

        switch (Phase)
        {
            case FusionRoundPhase.Countdown:
                RoundIndex = 1;
                SetPhase(FusionRoundPhase.Choosing, ChoosingSeconds);
                break;
            case FusionRoundPhase.Choosing:
                SetPhase(FusionRoundPhase.Revealing, RevealDuration);
                break;
            case FusionRoundPhase.Revealing:
                SetPhase(FusionRoundPhase.Moving, movementSeconds);
                break;
            case FusionRoundPhase.Moving:
                SetPhase(FusionRoundPhase.Intermission, IntermissionSeconds);
                break;
            case FusionRoundPhase.Intermission:
                RoundIndex++;
                SetPhase(FusionRoundPhase.Choosing, ChoosingSeconds);
                break;
        }
    }

    public override void Render()
    {
        if (displayedPhase == Phase && displayedRound == RoundIndex) return;
        displayedPhase = Phase;
        displayedRound = RoundIndex;
        PhaseChanged?.Invoke(Phase, RoundIndex);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        networkSpawned = false;
    }

    private void SetPhase(FusionRoundPhase next, float seconds)
    {
        Phase = next;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    private float InitialCountdownSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.InitialCountdownDuration
        : startCountdownSeconds;
    private float ChoosingSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.ChoosingDuration
        : choosingSeconds;
    private float IntermissionSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.NextRoundCountdown
        : intermissionSeconds;
}
