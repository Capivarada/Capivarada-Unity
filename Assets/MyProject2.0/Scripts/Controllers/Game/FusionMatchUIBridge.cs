using UnityEngine;

/// <summary>Maps Fusion's replicated round phases to the existing MatchUI view.</summary>
public sealed class FusionMatchUIBridge : MonoBehaviour
{
    [SerializeField] private FusionRoundController rounds;
    [SerializeField] private MatchUI matchUI;

    private void OnEnable()
    {
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (matchUI == null) matchUI = FindFirstObjectByType<MatchUI>();
        if (rounds != null) rounds.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (rounds != null) rounds.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(FusionRoundPhase phase, int _)
    {
        if (matchUI == null) return;
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
        }
    }
}
