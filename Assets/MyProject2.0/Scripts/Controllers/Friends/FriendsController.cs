using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controller for FriendsUI. Each instantiated prefab owns its own serialized visual references.</summary>
public sealed class FriendsController : MonoBehaviour
{
    [SerializeField] private FriendsService friendsService;
    [SerializeField] private Transform friendsContent, receivedContent, sentContent;
    [SerializeField] private FriendRowController friendRowPrefab;
    [SerializeField] private FriendRequestRowController receivedRequestPrefab, sentRequestPrefab;
    [SerializeField] private TMP_InputField addFriendInput;
    [SerializeField] private TMP_Text friendsTabText, requestsTabText, feedbackText;
    [SerializeField] private GameObject friendsView, requestsView, receivedRequestsView, sentRequestsView, requestNotification, emptyFriendsState, emptyRequestsState;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private Button sendRequestButton;
    private readonly List<GameObject> spawned = new();

    private void Awake() { if (friendsService == null) friendsService = ServiceHub.Instance != null ? ServiceHub.Instance.Friends : FriendsService.Instance; }
    private void OnEnable() { if (friendsService == null) return; friendsService.Changed += Rebuild; friendsService.Failed += Report; friendsService.Refresh(); }
    private void OnDisable() { if (friendsService == null) return; friendsService.Changed -= Rebuild; friendsService.Failed -= Report; }
    public void ShowFriends() { if (friendsView != null) friendsView.SetActive(true); if (requestsView != null) requestsView.SetActive(false); }
    public void ShowRequests()
    {
        if (friendsView != null) friendsView.SetActive(false);
        if (requestsView != null) requestsView.SetActive(true);
        ShowReceivedRequests();
    }
    public void ShowReceivedRequests()
    {
        if (receivedRequestsView != null) receivedRequestsView.SetActive(true);
        if (sentRequestsView != null) sentRequestsView.SetActive(false);
    }
    public void ShowSentRequests()
    {
        if (receivedRequestsView != null) receivedRequestsView.SetActive(false);
        if (sentRequestsView != null) sentRequestsView.SetActive(true);
    }
    public void Refresh() => friendsService?.Refresh();
    public void SendRequest()
    {
        if (friendsService == null || addFriendInput == null) return;
        SetBusy(true);
        friendsService.Send(addFriendInput.text, () => { if (addFriendInput != null) addFriendInput.text = string.Empty; SetBusy(false); Report("Solicitação enviada."); });
    }
    private void Rebuild()
    {
        Clear(); if (friendsService == null) return;
        foreach (FriendsService.FriendSummary friend in friendsService.Friends)
        {
            if (friendRowPrefab == null || friendsContent == null) continue;
            FriendRowController row = Instantiate(friendRowPrefab, friendsContent); spawned.Add(row.gameObject); row.Bind(friend, () => otherProfile?.Open(friend.playerId));
        }
        foreach (FriendsService.FriendRequestSummary request in friendsService.Incoming)
        {
            if (receivedRequestPrefab == null || receivedContent == null) continue;
            FriendRequestRowController row = Instantiate(receivedRequestPrefab, receivedContent); spawned.Add(row.gameObject);
            row.Bind(request, () => friendsService.Accept(request.requestId), () => friendsService.Reject(request.requestId), null);
        }
        foreach (FriendsService.FriendRequestSummary request in friendsService.Outgoing)
        {
            if (sentRequestPrefab == null || sentContent == null) continue;
            FriendRequestRowController row = Instantiate(sentRequestPrefab, sentContent); spawned.Add(row.gameObject);
            row.Bind(request, null, null, () => friendsService.Cancel(request.requestId));
        }
        if (friendsTabText != null) friendsTabText.text = "Amigos · " + friendsService.Friends.Count;
        int count = friendsService.Incoming.Count + friendsService.Outgoing.Count;
        if (requestsTabText != null) requestsTabText.text = "Solicitações · " + count;
        if (requestNotification != null) requestNotification.SetActive(friendsService.Incoming.Count > 0);
        if (emptyFriendsState != null) emptyFriendsState.SetActive(friendsService.Friends.Count == 0);
        if (emptyRequestsState != null) emptyRequestsState.SetActive(friendsService.Incoming.Count == 0 && friendsService.Outgoing.Count == 0);
        SetBusy(false);
    }
    private void SetBusy(bool busy) { if (sendRequestButton != null) sendRequestButton.interactable = !busy; }
    private void Report(string message) { if (feedbackText != null) feedbackText.text = message; else Debug.LogWarning(message, this); SetBusy(false); }
    private void Clear() { foreach (GameObject item in spawned) if (item != null) Destroy(item); spawned.Clear(); }
}
