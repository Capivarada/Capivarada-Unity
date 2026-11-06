using UnityEngine;

/// <summary>Confirmation wrapper; LeaveGame remains the only component that exits the application.</summary>
public sealed class LeaveGameConfirmation : MonoBehaviour
{
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private LeaveGame leaveGame;
    public void RequestLeave() { if (confirmationPanel != null) confirmationPanel.SetActive(true); }
    public void Cancel() { if (confirmationPanel != null) confirmationPanel.SetActive(false); }
    public void Confirm() { if (leaveGame != null) leaveGame.Leave(); }
}
