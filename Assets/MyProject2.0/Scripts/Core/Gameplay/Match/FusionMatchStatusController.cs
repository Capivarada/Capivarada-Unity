using TMPro;
using UnityEngine;

/// <summary>Optional UI adapter for the online scene's waiting label.</summary>
public sealed class FusionMatchStatusController : MonoBehaviour
{
    [SerializeField] private FusionArenaMatchController match;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private GameObject waitingRoot;

    private void OnEnable()
    {
        if (match == null) return;
        match.WaitingChanged += RefreshWaiting;
        match.MatchReady += HideWaiting;
    }

    private void OnDisable()
    {
        if (match == null) return;
        match.WaitingChanged -= RefreshWaiting;
        match.MatchReady -= HideWaiting;
    }

    private void RefreshWaiting(int current, int required)
    {
        if (waitingRoot != null) waitingRoot.SetActive(true);
        if (waitingText != null) waitingText.text = $"Aguardando jogadores {current}/{required}";
    }

    private void HideWaiting()
    {
        if (waitingRoot != null) waitingRoot.SetActive(false);
    }
}
