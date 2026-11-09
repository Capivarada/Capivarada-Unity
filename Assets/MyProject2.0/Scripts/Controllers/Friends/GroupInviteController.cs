using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Renders a searchable subset of accepted friends that may receive a party invite.</summary>
public sealed class GroupInviteController : MonoBehaviour
{
    [SerializeField] private FriendsService friendsService;
    [SerializeField] private PartyService partyService;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private FriendGroupInvitePrefabController friendInvitePrefab;
    [SerializeField] private TMP_Text feedbackText;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (friendsService == null) friendsService = ServiceHub.Instance != null ? ServiceHub.Instance.Friends : FriendsService.Instance;
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
    }

    private void OnEnable()
    {
        if (friendsService != null) friendsService.Changed += Rebuild;
        if (partyService != null)
        {
            partyService.Changed += Rebuild;
            partyService.Failed += Report;
        }
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
        friendsService?.Refresh();
        partyService?.Refresh();
        Rebuild();
    }

    private void OnDisable()
    {
        if (friendsService != null) friendsService.Changed -= Rebuild;
        if (partyService != null)
        {
            partyService.Changed -= Rebuild;
            partyService.Failed -= Report;
        }
        if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
    }

    private void OnSearchChanged(string _) => Rebuild();

    private void Rebuild()
    {
        Clear();
        if (friendsService == null || partyService == null || content == null || friendInvitePrefab == null) return;
        string filter = searchInput != null ? searchInput.text : string.Empty;
        int shown = 0;
        foreach (FriendsService.FriendSummary friend in friendsService.Friends)
        {
            if (!Matches(filter, friend)) continue;
            if (partyService.Contains(friend.playerId)) continue;
            bool pending = partyService.HasOutgoingInvite(friend.playerId);
            FriendGroupInvitePrefabController row = Instantiate(friendInvitePrefab, content);
            spawned.Add(row.gameObject);
            row.Bind(friend, partyService.CanInvite, pending, () => otherProfile?.Open(friend.playerId), () => partyService.Invite(friend.playerId));
            shown++;
        }
        if (emptyState != null) emptyState.SetActive(shown == 0);
    }

    private static bool Matches(string filter, FriendsService.FriendSummary friend)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        return (!string.IsNullOrWhiteSpace(friend.nickname) && friend.nickname.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
               || (!string.IsNullOrWhiteSpace(friend.playerId) && friend.playerId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
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
