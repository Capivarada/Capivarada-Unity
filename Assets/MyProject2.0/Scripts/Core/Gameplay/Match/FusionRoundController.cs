using System;
using System.Collections.Generic;
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
    [SerializeField] private FusionArenaMatchController match;
    [SerializeField] private ArenaBounds arenaBounds;

    [Networked] public FusionRoundPhase Phase { get; private set; }
    [Networked] public int RoundIndex { get; private set; }
    [Networked] private TickTimer PhaseTimer { get; set; }
    [Networked] private TickTimer StopTimer { get; set; }
    [Networked] private Vector3 ArenaTargetScale { get; set; }
    [Networked] private Vector3 ArenaStartScale { get; set; }
    [Networked] private TickTimer ArenaShrinkTimer { get; set; }

    private FusionRoundPhase displayedPhase;
    private int displayedRound;
    private bool networkSpawned;
    private bool arenaScaleInitialized;
    private bool hasRenderedArenaScale;
    private Vector3 renderedArenaScale;
    private readonly List<GameObject> livingParticipants = new();
    public event Action<FusionRoundPhase, int> PhaseChanged;
    public bool IsNetworkSpawned => networkSpawned;
    public float RemainingPhaseSeconds => !networkSpawned || Runner == null || !PhaseTimer.IsRunning
        ? 0f
        : PhaseTimer.RemainingTime(Runner) ?? 0f;
    public float RevealDuration => match != null && match.MatchConfig != null
        ? match.MatchConfig.RevealDuration
        : 1.5f;
    public bool IsFinished => networkSpawned && Phase == FusionRoundPhase.Finished;

    public override void Spawned()
    {
        networkSpawned = true;
        if (match == null) match = GetComponent<FusionArenaMatchController>();
        if (arenaBounds == null) arenaBounds = FindFirstObjectByType<ArenaBounds>();

        // A scene can be reused by Fusion after a runner restart. Restore the
        // authored arena before the replicated target scale arrives, then let
        // Render drive it from the networked round state.
        if (arenaBounds != null)
        {
            arenaBounds.ResetArena();
            hasRenderedArenaScale = false;
            renderedArenaScale = Vector3.zero;
        }
        displayedPhase = Phase;
        displayedRound = RoundIndex;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || match == null || Phase == FusionRoundPhase.Finished) return;
        if (!match.IsMatchReady) return;
        InitializeArenaScale();

        if (Phase == FusionRoundPhase.Waiting)
        {
            SetPhase(FusionRoundPhase.Countdown, InitialCountdownSeconds);
            return;
        }

        // The online match has no legacy MatchManager watching PlayerDeath.
        // Once the first playable round has started, the replicated elimination
        // flags are the single source of truth for match completion.
        if (HasRoundStarted() && CountLivingParticipants() <= 1)
        {
            FinishMatch();
            return;
        }

        if (Phase == FusionRoundPhase.Moving)
        {
            AdvanceWhenEveryoneStops();
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
                SetPhase(FusionRoundPhase.Moving, 0f);
                StopTimer = default;
                break;
            case FusionRoundPhase.Intermission:
                RoundIndex++;
                SetPhase(FusionRoundPhase.Choosing, ChoosingSeconds);
                break;
        }
    }

    public override void Render()
    {
        ApplyArenaPresentation();

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

    private void FinishMatch()
    {
        Phase = FusionRoundPhase.Finished;
        PhaseTimer = default;
        StopTimer = default;
        Debug.Log("[FusionRoundController] Partida encerrada: restou um ou nenhum sobrevivente.", this);
    }

    private float InitialCountdownSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.InitialCountdownDuration
        : 3f;
    private float ChoosingSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.ChoosingDuration
        : 8f;
    private float IntermissionSeconds => match != null && match.MatchConfig != null
        ? match.MatchConfig.NextRoundCountdown
        : 2f;

    private void InitializeArenaScale()
    {
        if (arenaScaleInitialized || arenaBounds == null) return;
        arenaScaleInitialized = true;
        ArenaTargetScale = arenaBounds.InitialArenaScale;
        ArenaStartScale = ArenaTargetScale;
    }

    private void BeginArenaShrinkForNextRound()
    {
        if (arenaBounds == null) return;
        float shrink = match != null && match.MatchConfig != null ? match.MatchConfig.ShrinkPerRound : 2.5f;
        ArenaStartScale = arenaBounds.CurrentArenaScale;
        ArenaTargetScale = arenaBounds.GetScaleForRound(RoundIndex + 1, shrink);
        ArenaShrinkTimer = TickTimer.CreateFromSeconds(Runner, IntermissionSeconds);
        Debug.Log($"[FusionRoundController] Arena: rodada {RoundIndex + 1}, escala {ArenaStartScale.x:0.##} -> {ArenaTargetScale.x:0.##}.", this);
    }

    private void AdvanceWhenEveryoneStops()
    {
        if (!AllLivingParticipantsStopped())
        {
            StopTimer = default;
            return;
        }

        if (!StopTimer.IsRunning)
        {
            float delay = match != null && match.MatchConfig != null ? match.MatchConfig.StopCheckDelay : 0.75f;
            StopTimer = TickTimer.CreateFromSeconds(Runner, delay);
            return;
        }

        if (StopTimer.Expired(Runner))
        {
            BeginArenaShrinkForNextRound();
            SetPhase(FusionRoundPhase.Intermission, IntermissionSeconds);
        }
    }

    private void ApplyArenaPresentation()
    {
        if (arenaBounds == null) arenaBounds = FindFirstObjectByType<ArenaBounds>();
        if (arenaBounds == null || ArenaTargetScale.sqrMagnitude <= 0.001f) return;

        Vector3 scale = ArenaTargetScale;
        if (Phase == FusionRoundPhase.Intermission && ArenaShrinkTimer.IsRunning)
        {
            float duration = Mathf.Max(0.01f, IntermissionSeconds);
            float remaining = ArenaShrinkTimer.RemainingTime(Runner) ?? 0f;
            float progress = Mathf.Clamp01(1f - remaining / duration);
            scale = Vector3.Lerp(ArenaStartScale, ArenaTargetScale, progress);
        }

        if (hasRenderedArenaScale && (renderedArenaScale - scale).sqrMagnitude <= 0.000001f) return;
        hasRenderedArenaScale = true;
        renderedArenaScale = scale;
        arenaBounds.ApplyNetworkScale(scale, GetLivingParticipants());
    }

    private IReadOnlyList<GameObject> GetLivingParticipants()
    {
        livingParticipants.Clear();
        foreach (FusionNetworkPlayer player in FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (player != null && !player.IsEliminated) livingParticipants.Add(player.gameObject);

        foreach (FusionNetworkBot bot in FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (bot != null && !bot.IsEliminated) livingParticipants.Add(bot.gameObject);

        return livingParticipants;
    }

    private bool HasRoundStarted()
    {
        return Phase == FusionRoundPhase.Choosing ||
               Phase == FusionRoundPhase.Revealing ||
               Phase == FusionRoundPhase.Moving ||
               Phase == FusionRoundPhase.Intermission;
    }

    private int CountLivingParticipants()
    {
        int alive = 0;
        foreach (FusionNetworkPlayer player in FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (player != null && !player.IsEliminated) alive++;

        foreach (FusionNetworkBot bot in FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (bot != null && !bot.IsEliminated) alive++;

        return alive;
    }

    private bool AllLivingParticipantsStopped()
    {
        float threshold = match != null && match.MatchConfig != null ? match.MatchConfig.StopVelocityThreshold : 0.15f;
        bool foundLivingParticipant = false;
        foreach (FusionNetworkPlayer player in FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player == null || player.IsEliminated) continue;
            foundLivingParticipant = true;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null && new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude > threshold) return false;
        }
        foreach (FusionNetworkBot bot in FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (bot == null || bot.IsEliminated) continue;
            foundLivingParticipant = true;
            Rigidbody body = bot.GetComponent<Rigidbody>();
            if (body != null && new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude > threshold) return false;
        }
        return foundLivingParticipant;
    }
}
