using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

/// <summary>Owns the social state displayed by FriendsUI. Requests are server-authoritative; accepted friendships are also read from PlayFab.</summary>
public sealed class FriendsService : MonoBehaviour
{
    [Serializable] public class FriendSummary { public string playerId; public string nickname; public string avatarId; public int level; public string presence; public bool canInvite; }
    [Serializable] public class FriendRequestSummary { public string requestId; public string playerId; public string nickname; public string avatarId; public int level; public string createdAtUtc; }
    [Serializable] private class FriendListResponse { public FriendSummary[] friends; }
    [Serializable] private class RequestListResponse { public FriendRequestSummary[] incoming; public FriendRequestSummary[] outgoing; }
    [Serializable] private class RequestInput { public string playerId; public string nicknameOrId; public string requestId; }

    [SerializeField] private CapivaradaBackendService backend;
    public static FriendsService Instance { get; private set; }
    public IReadOnlyList<FriendSummary> Friends => friends;
    public IReadOnlyList<FriendRequestSummary> Incoming => incoming;
    public IReadOnlyList<FriendRequestSummary> Outgoing => outgoing;
    public event Action Changed;
    public event Action<string> Failed;
    private readonly List<FriendSummary> friends = new();
    private readonly List<FriendRequestSummary> incoming = new();
    private readonly List<FriendRequestSummary> outgoing = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        DontDestroyOnLoad(gameObject);
    }

    public void Refresh()
    {
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        backend.Get<FriendListResponse>("friends", result =>
        {
            friends.Clear(); if (result?.friends != null) friends.AddRange(result.friends); Changed?.Invoke();
        }, Failed);
        backend.Get<RequestListResponse>("friends/requests", result =>
        {
            incoming.Clear(); outgoing.Clear();
            if (result?.incoming != null) incoming.AddRange(result.incoming);
            if (result?.outgoing != null) outgoing.AddRange(result.outgoing);
            Changed?.Invoke();
        }, Failed);
    }

    public void Send(string nicknameOrId, Action completed = null)
    {
        if (string.IsNullOrWhiteSpace(nicknameOrId)) { Failed?.Invoke("Informe o apelido ou ID do jogador."); return; }
        Execute("friends/request", new RequestInput { nicknameOrId = nicknameOrId.Trim() }, completed);
    }
    public void Accept(string requestId, Action completed = null) => Execute("friends/accept", new RequestInput { requestId = requestId }, completed);
    public void Reject(string requestId, Action completed = null) => Execute("friends/reject", new RequestInput { requestId = requestId }, completed);
    public void Cancel(string requestId, Action completed = null) => Execute("friends/cancel", new RequestInput { requestId = requestId }, completed);
    public void Remove(string playerId, Action completed = null) => Execute("friends/remove", new RequestInput { playerId = playerId }, completed);

    public bool IsFriend(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return false;
        return friends.Exists(friend => string.Equals(friend.playerId, playerId, StringComparison.Ordinal));
    }

    public bool HasPendingRequest(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return false;
        return incoming.Exists(request => string.Equals(request.playerId, playerId, StringComparison.Ordinal))
               || outgoing.Exists(request => string.Equals(request.playerId, playerId, StringComparison.Ordinal));
    }

    /// <summary>Direct PlayFab fallback only returns established friendships, never pending requests.</summary>
    public void RefreshAcceptedFromPlayFab(Action completed = null)
    {
        PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest { ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true } }, result =>
        {
            friends.Clear();
            if (result.Friends != null) foreach (FriendInfo item in result.Friends)
                friends.Add(new FriendSummary { playerId = item.FriendPlayFabId, nickname = string.IsNullOrWhiteSpace(item.TitleDisplayName) ? item.Username : item.TitleDisplayName, presence = "Offline" });
            Changed?.Invoke(); completed?.Invoke();
        }, error => Failed?.Invoke(error.ErrorMessage));
    }

    private void Execute(string route, RequestInput input, Action completed)
    {
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        backend.Post<RequestInput, CapivaradaBackendService.EmptyResponse>(route, input, _ => { Refresh(); completed?.Invoke(); }, Failed);
    }
}
