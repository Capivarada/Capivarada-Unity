using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Renders the current server-authoritative party into GroupView. It deliberately
/// does not own GroupView/InviteView activation; PanelNavigationController does.
/// </summary>
public sealed class GroupController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private PartyService partyService;
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private OtherProfileController otherProfile;

    [Header("Group grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private GroupPlayerPrefabController playerGroupPrefab;
    [SerializeField] private GroupInvitePrefabController inviteGroupPrefab;
    [SerializeField, Min(1)] private int groupCapacity = 8;

    [Header("Navigation")]
    [Tooltip("The PanelNavigatorController on this GroupUI. Invite slots use it to open the configured Invite panel.")]
    [SerializeField] private PanelNavigatorController panelNavigator;
    [SerializeField] private string invitePanelId = "Invite";
    [SerializeField] private LobbyNavigationController lobbyNavigation;

    [Header("Actions")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button groupChatButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private UnityEvent startMatchmakingRequested;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
        if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
        if (lobbyNavigation == null) lobbyNavigation = GetComponentInParent<LobbyNavigationController>();
    }

    private void OnEnable()
    {
        if (partyService != null)
        {
            partyService.Changed += Rebuild;
            partyService.Failed += Report;
            if (partyService.IsInParty) partyService.Refresh();
            else partyService.Create();
        }
        Rebuild();
    }

    private void OnDisable()
    {
        if (partyService != null)
        {
            partyService.Changed -= Rebuild;
            partyService.Failed -= Report;
        }
    }

    public void ToggleReady()
    {
        if (partyService == null || !partyService.IsInParty || partyService.IsLeader) return;
        PartyService.Member local = FindLocalMember();
        if (local == null) return;
        partyService.SetReady(!local.ready);
    }

    public void LeaveParty()
    {
        if (partyService == null) return;
        partyService.Leave(() => lobbyNavigation?.BackToMenu());
    }

    /// <summary>Called by the Start button. Queue ownership remains in the matchmaking service.</summary>
    public void RequestStartMatchmaking()
    {
        if (partyService == null || !partyService.CanStart)
        {
            Report("Aguarde todos os integrantes ficarem prontos.");
            return;
        }
        startMatchmakingRequested?.Invoke();
    }

    private void Rebuild()
    {
        Clear();
        PartyService.Party party = partyService != null ? partyService.CurrentParty : null;
        PartyService.Member[] members = party != null && party.members != null ? party.members : Array.Empty<PartyService.Member>();
        int capacity = party != null && party.maxMembers > 0 ? party.maxMembers : groupCapacity;

        for (int index = 0; index < members.Length && index < capacity; index++)
        {
            if (playerGroupPrefab == null || gridContent == null) break;
            PartyService.Member member = members[index];
            GroupPlayerPrefabController row = Instantiate(playerGroupPrefab, gridContent);
            spawned.Add(row.gameObject);
            row.Bind(member, () => otherProfile?.Open(member.playerId));
        }

        for (int index = members.Length; index < capacity; index++)
        {
            if (inviteGroupPrefab == null || gridContent == null) break;
            GroupInvitePrefabController slot = Instantiate(inviteGroupPrefab, gridContent);
            spawned.Add(slot.gameObject);
            if (slot.InviteButton == null) continue;

            slot.InviteButton.onClick.RemoveAllListeners();
            slot.InviteButton.onClick.AddListener(OpenInvitePanel);
            slot.InviteButton.interactable = partyService != null && partyService.CanInvite && panelNavigator != null && !string.IsNullOrWhiteSpace(invitePanelId);
        }

        PartyService.Member local = FindLocalMember();
        bool leader = partyService != null && partyService.IsLeader;
        if (readyButton != null) readyButton.gameObject.SetActive(party != null && !leader);
        if (readyButtonText != null && local != null) readyButtonText.text = local.ready ? "Cancelar pronto" : "Pronto";
        if (startButton != null) startButton.interactable = partyService != null && partyService.CanStart;
        if (groupChatButton != null) groupChatButton.interactable = partyService != null && partyService.IsInParty;
    }

    /// <summary>
    /// Opens the Invite entry through the GroupUI navigator. This does not own
    /// panel visibility: the PanelNavigatorController remains the sole owner.
    /// </summary>
    public void OpenInvitePanel()
    {
        if (panelNavigator == null)
        {
            Report("Configure o PanelNavigatorController do GroupUI no GroupController.");
            return;
        }

        if (string.IsNullOrWhiteSpace(invitePanelId))
        {
            Report("Configure o ID do painel de convite no GroupController.");
            return;
        }

        panelNavigator.Show(invitePanelId);
    }

    private PartyService.Member FindLocalMember()
    {
        if (partyService?.CurrentParty?.members == null || profileService?.Current == null) return null;
        return Array.Find(partyService.CurrentParty.members, member => member != null && string.Equals(member.playerId, profileService.Current.playerId, StringComparison.Ordinal));
    }

    private void Report(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        else Debug.LogWarning(message, this);
    }

    private void Clear()
    {
        foreach (GameObject item in spawned) if (item != null) Destroy(item);
        spawned.Clear();
    }
}
