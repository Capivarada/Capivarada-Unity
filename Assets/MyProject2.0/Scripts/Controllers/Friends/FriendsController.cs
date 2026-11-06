using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the two main areas of FriendsUI: the friends list and the request list.
/// RequestSummary and AddFriend remain children of FriendsPanel and keep their own layout.
/// </summary>
public sealed class FriendsController : MonoBehaviour
{
    private enum RequestTab { Received, Sent }

    [Header("Services")]
    [SerializeField] private FriendsService friendsService;
    [SerializeField] private OtherProfileController otherProfile;

    [Header("FriendsPanel")]
    [SerializeField] private TMP_InputField friendSearchInput;
    [SerializeField] private Transform friendsContent;
    [SerializeField] private GameObject emptyFriendsState;
    [SerializeField] private FriendRowController friendRowPrefab;
    [Tooltip("Optional badge or summary inside FriendsPanel. The controller only toggles it when there are received requests.")]
    [SerializeField] private GameObject requestNotification;

    [Header("RequestPanel")]
    [SerializeField] private TMP_InputField requestSearchInput;
    [SerializeField] private Transform requestContent;
    [SerializeField] private GameObject emptyRequestsState;
    [SerializeField] private FriendRequestRowController receivedRequestPrefab;
    [SerializeField] private FriendRequestRowController sentRequestPrefab;

    [Header("Add friend sidebar (inside FriendsPanel)")]
    [SerializeField] private TMP_InputField addFriendInput;
    [SerializeField] private Button sendRequestButton;
    [SerializeField] private TMP_Text feedbackText;

    private readonly List<GameObject> spawnedFriends = new();
    private readonly List<GameObject> spawnedRequests = new();
    private RequestTab activeRequestTab = RequestTab.Received;

    private void Awake()
    {
        if (friendsService == null)
            friendsService = ServiceHub.Instance != null ? ServiceHub.Instance.Friends : FriendsService.Instance;
    }

    private void OnEnable()
    {
        if (friendsService != null)
        {
            friendsService.Changed += Rebuild;
            friendsService.Failed += Report;
        }

        if (friendSearchInput != null) friendSearchInput.onValueChanged.AddListener(OnFriendSearchChanged);
        if (requestSearchInput != null) requestSearchInput.onValueChanged.AddListener(OnRequestSearchChanged);
        RebuildFriends();
        friendsService?.Refresh();
    }

    private void OnDisable()
    {
        if (friendsService != null)
        {
            friendsService.Changed -= Rebuild;
            friendsService.Failed -= Report;
        }

        if (friendSearchInput != null) friendSearchInput.onValueChanged.RemoveListener(OnFriendSearchChanged);
        if (requestSearchInput != null) requestSearchInput.onValueChanged.RemoveListener(OnRequestSearchChanged);
    }

    public void ShowReceivedRequests()
    {
        activeRequestTab = RequestTab.Received;
        RebuildRequests();
    }

    public void ShowSentRequests()
    {
        activeRequestTab = RequestTab.Sent;
        RebuildRequests();
    }

    public void Refresh() => friendsService?.Refresh();

    public void SendRequest()
    {
        if (friendsService == null || addFriendInput == null) return;
        SetBusy(true);
        friendsService.Send(addFriendInput.text, () =>
        {
            if (addFriendInput != null) addFriendInput.text = string.Empty;
            Report("Solicitação enviada.");
        });
    }

    private void OnFriendSearchChanged(string _) => RebuildFriends();
    private void OnRequestSearchChanged(string _) => RebuildRequests();

    private void Rebuild()
    {
        RebuildFriends();
        RebuildRequests();
        if (requestNotification != null && friendsService != null)
            requestNotification.SetActive(friendsService.Incoming.Count > 0);
        SetBusy(false);
    }

    private void RebuildFriends()
    {
        Clear(spawnedFriends);
        if (friendsService == null || friendsContent == null || friendRowPrefab == null) return;

        string filter = friendSearchInput != null ? friendSearchInput.text : string.Empty;
        int visibleCount = 0;
        foreach (FriendsService.FriendSummary friend in friendsService.Friends)
        {
            if (!Matches(filter, friend.nickname, friend.playerId)) continue;
            FriendRowController row = Instantiate(friendRowPrefab, friendsContent);
            spawnedFriends.Add(row.gameObject);
            row.Bind(friend, () => otherProfile?.Open(friend.playerId));
            visibleCount++;
        }

        if (emptyFriendsState != null) emptyFriendsState.SetActive(visibleCount == 0);
    }

    private void RebuildRequests()
    {
        Clear(spawnedRequests);
        if (friendsService == null || requestContent == null) return;

        string filter = requestSearchInput != null ? requestSearchInput.text : string.Empty;
        IReadOnlyList<FriendsService.FriendRequestSummary> source = activeRequestTab == RequestTab.Received
            ? friendsService.Incoming
            : friendsService.Outgoing;
        FriendRequestRowController prefab = activeRequestTab == RequestTab.Received
            ? receivedRequestPrefab
            : sentRequestPrefab;
        int visibleCount = 0;

        if (prefab != null)
        {
            foreach (FriendsService.FriendRequestSummary request in source)
            {
                if (!Matches(filter, request.nickname, request.playerId)) continue;
                FriendRequestRowController row = Instantiate(prefab, requestContent);
                spawnedRequests.Add(row.gameObject);
                if (activeRequestTab == RequestTab.Received)
                    row.Bind(request, () => friendsService.Accept(request.requestId), () => friendsService.Reject(request.requestId), null);
                else
                    row.Bind(request, null, null, () => friendsService.Cancel(request.requestId));
                visibleCount++;
            }
        }

        if (emptyRequestsState != null) emptyRequestsState.SetActive(visibleCount == 0);
    }

    private static bool Matches(string filter, string nickname, string playerId)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        return (!string.IsNullOrWhiteSpace(nickname) && nickname.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
               || (!string.IsNullOrWhiteSpace(playerId) && playerId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void SetBusy(bool busy)
    {
        if (sendRequestButton != null) sendRequestButton.interactable = !busy;
    }

    private void Report(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        else Debug.LogWarning(message, this);
        SetBusy(false);
    }

    private static void Clear(List<GameObject> rows)
    {
        foreach (GameObject row in rows) if (row != null) Destroy(row);
        rows.Clear();
    }
}
