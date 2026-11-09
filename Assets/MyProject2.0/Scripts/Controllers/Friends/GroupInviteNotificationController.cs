using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Raises an Inspector event when a new incoming party invitation appears. Wire this
/// to the local PanelNavigationController and GroupInviteModalController; it never
/// changes panel active state itself.
/// </summary>
public sealed class GroupInviteNotificationController : MonoBehaviour
{
    [SerializeField] private PartyService partyService;
    [SerializeField] private UnityEvent invitationReceived;
    private string lastNotifiedInviteId;

    private void Awake()
    {
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
    }

    private void OnEnable()
    {
        if (partyService == null) return;
        partyService.Changed += CheckForIncomingInvite;
        partyService.Refresh();
        CheckForIncomingInvite();
    }

    private void OnDisable()
    {
        if (partyService != null) partyService.Changed -= CheckForIncomingInvite;
    }

    public void CheckForIncomingInvite()
    {
        if (partyService == null || partyService.Incoming.Count == 0) return;
        PartyService.Invitation newest = partyService.Incoming[0];
        if (newest == null || string.IsNullOrWhiteSpace(newest.inviteId) || string.Equals(newest.inviteId, lastNotifiedInviteId, StringComparison.Ordinal)) return;
        lastNotifiedInviteId = newest.inviteId;
        invitationReceived?.Invoke();
    }
}
