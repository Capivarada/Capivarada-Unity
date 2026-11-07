using UnityEngine;

/// <summary>Maps Fusion's replicated round phases to the existing MatchUI view.</summary>
public sealed class FusionMatchUIBridge : MonoBehaviour
{
    [SerializeField] private FusionRoundController rounds;
    [SerializeField] private FusionArenaMatchController match;
    [SerializeField] private MatchUI matchUI;
    private FusionRoundPhase displayedPhase = (FusionRoundPhase)(-1);
    private int displayedRound = -1;

    private void OnEnable()
    {
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (match == null) match = FindFirstObjectByType<FusionArenaMatchController>();
        if (matchUI == null) matchUI = FindFirstObjectByType<MatchUI>();
        if (rounds != null) rounds.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (rounds != null) rounds.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(FusionRoundPhase phase, int round)
    {
        if (matchUI == null) return;
        displayedPhase = phase;
        displayedRound = round;
        switch (phase)
        {
            case FusionRoundPhase.Countdown:
                matchUI.ShowStartingUI();
                matchUI.ShowNetworkPhase("A PARTIDA VAI COMEÇAR");
                break;
            case FusionRoundPhase.Choosing:
                matchUI.ShowChoosingUI();
                matchUI.ShowNetworkPhase("ESCOLHA A DIREÇÃO E A FORÇA");
                break;
            case FusionRoundPhase.Revealing:
                matchUI.ShowRevealingUI();
                matchUI.ShowNetworkPhase("DIREÇÕES REVELADAS");
                break;
            case FusionRoundPhase.Moving:
                matchUI.ShowRevealingUI();
                matchUI.ShowNetworkPhase("MOVIMENTO!");
                break;
            case FusionRoundPhase.Intermission:
                matchUI.ShowIntermissionUI();
                matchUI.ShowNetworkPhase("PRÓXIMA RODADA");
                break;
            case FusionRoundPhase.Finished:
                matchUI.ShowIntermissionUI();
                matchUI.ShowNetworkPhase("PARTIDA ENCERRADA");
                matchUI.HandleNetworkMatchFinished();
                break;
        }
    }

    private void Update()
    {
        if (rounds == null || !rounds.IsNetworkSpawned || matchUI == null) return;
        if (displayedPhase != rounds.Phase || displayedRound != rounds.RoundIndex)
            HandlePhaseChanged(rounds.Phase, rounds.RoundIndex);
        matchUI.SetNetworkRoundStatus(rounds.RoundIndex, match != null ? match.GetEliminatedCount() : 0, match != null ? match.CurrentParticipantCount : 0);
        bool showCountdown = rounds.Phase == FusionRoundPhase.Countdown ||
                             rounds.Phase == FusionRoundPhase.Choosing ||
                             rounds.Phase == FusionRoundPhase.Intermission;
        matchUI.SetNetworkCountdown(showCountdown ? rounds.RemainingPhaseSeconds : 0f);
    }
}
