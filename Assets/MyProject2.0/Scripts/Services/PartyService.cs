using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent client facade for the server-authoritative party. It owns no membership
/// state itself: every mutation is validated by Azure Functions and then refreshed.
/// </summary>
public sealed class PartyService : MonoBehaviour
{
    [Serializable] public sealed class Member { public string playerId; public string nickname; public string avatarId; public int level; public bool ready; public bool isLeader; }
    [Serializable] public sealed class Party { public string partyId; public string leaderPlayerId; public int maxMembers; public Member[] members; }
    [Serializable] public sealed class Invitation { public string inviteId; public string partyId; public string senderPlayerId; public string targetPlayerId; public string nickname; public string avatarId; public int level; public string createdAtUtc; }
    [Serializable] private sealed class StateResponse { public Party party; public Invitation[] incoming; public Invitation[] outgoing; }
    [Serializable] private sealed class PartyResponse { public Party party; }
    [Serializable] private sealed class InviteInput { public string playerId; }
    [Serializable] private sealed class InviteActionInput { public string inviteId; }
    [Serializable] private sealed class ReadyInput { public bool ready; }

    public static PartyService Instance { get; private set; }
    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private VivoxVoiceChannelService voice;

    public Party CurrentParty { get; private set; }
    public IReadOnlyList<Invitation> Incoming => incoming;
    public IReadOnlyList<Invitation> Outgoing => outgoing;
    public bool IsInParty => CurrentParty != null && !string.IsNullOrWhiteSpace(CurrentParty.partyId);
    public bool IsLeader => IsInParty && string.Equals(CurrentParty.leaderPlayerId, LocalPlayerId, StringComparison.Ordinal);
    public bool CanInvite => !IsInParty || (IsLeader && CurrentParty.members != null && CurrentParty.members.Length < PartyStoreMaximumMembers);
    public bool CanStart => IsLeader && CurrentParty.members != null && Array.TrueForAll(CurrentParty.members, member => member.isLeader || member.ready);
    public event Action Changed;
    public event Action<string> Failed;

    private readonly List<Invitation> incoming = new();
    private readonly List<Invitation> outgoing = new();
    private string activeVoicePartyId;
    private bool refreshInFlight;
    private const int PartyStoreMaximumMembers = 8;

    private string LocalPlayerId
    {
        get
        {
            PlayerProfileService profile = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
            return profile != null && profile.Current != null ? profile.Current.playerId : string.Empty;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        if (session == null) session = GetComponent<SessionAccessService>();
        if (voice == null) voice = GetComponent<VivoxVoiceChannelService>();
        DontDestroyOnLoad(gameObject);
    }

    public void Refresh()
    {
        if (!CanUseParty()) return;
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        if (refreshInFlight) return;

        refreshInFlight = true;
        backend.Get<StateResponse>("party", response =>
        {
            refreshInFlight = false;
            ApplyState(response);
        }, error =>
        {
            refreshInFlight = false;
            Failed?.Invoke(error);
        });
    }

    public void Create(Action completed = null) => Execute("party/create", new CapivaradaBackendService.EmptyResponse(), completed);
    public void Invite(string playerId, Action completed = null)
    {
        if (string.IsNullOrWhiteSpace(playerId)) { Failed?.Invoke("Amigo inválido."); return; }
        Execute("party/invite", new InviteInput { playerId = playerId }, completed);
    }

    public void Accept(string inviteId, Action completed = null) => Execute("party/invitations/accept", new InviteActionInput { inviteId = inviteId }, completed);
    public void Reject(string inviteId, Action completed = null) => Execute("party/invitations/reject", new InviteActionInput { inviteId = inviteId }, completed);
    public void Cancel(string inviteId, Action completed = null) => Execute("party/invitations/cancel", new InviteActionInput { inviteId = inviteId }, completed);
    public void SetReady(bool ready, Action completed = null) => Execute("party/ready", new ReadyInput { ready = ready }, completed);
    public void Leave(Action completed = null)
    {
        if (!CanUseParty() || backend == null) return;

        backend.Post<CapivaradaBackendService.EmptyResponse, CapivaradaBackendService.EmptyResponse>("party/leave", new CapivaradaBackendService.EmptyResponse(), _ =>
        {
            // The server has accepted the leave request. Clear only membership
            // immediately so a reopened GroupUI creates a fresh local party
            // instead of rendering a stale Start state while Refresh is in flight.
            CurrentParty = null;
            UpdateVoiceChannel();
            Changed?.Invoke();
            completed?.Invoke();
            Refresh();
        }, Failed);
    }
    public void Kick(string playerId, Action completed = null) => Execute("party/kick", new InviteInput { playerId = playerId }, completed);

    public bool Contains(string playerId)
    {
        if (CurrentParty?.members == null || string.IsNullOrWhiteSpace(playerId)) return false;
        return Array.Exists(CurrentParty.members, member => string.Equals(member.playerId, playerId, StringComparison.Ordinal));
    }

    public bool HasOutgoingInvite(string playerId) => outgoing.Exists(invite => string.Equals(invite.targetPlayerId, playerId, StringComparison.Ordinal));

    private void Execute<T>(string route, T input, Action completed)
    {
        if (!CanUseParty() || backend == null) return;
        backend.Post<T, PartyResponse>(route, input, _ => { Refresh(); completed?.Invoke(); }, Failed);
    }

    private void ApplyState(StateResponse response)
    {
        CurrentParty = response != null ? response.party : null;
        incoming.Clear(); outgoing.Clear();
        if (response?.incoming != null) incoming.AddRange(response.incoming);
        if (response?.outgoing != null) outgoing.AddRange(response.outgoing);
        UpdateVoiceChannel();
        Changed?.Invoke();
    }

    private async void UpdateVoiceChannel()
    {
        string partyId = CurrentParty != null ? CurrentParty.partyId : string.Empty;
        if (string.Equals(activeVoicePartyId, partyId, StringComparison.Ordinal)) return;
        activeVoicePartyId = partyId;
        if (voice == null) return;
        if (string.IsNullOrWhiteSpace(partyId)) await voice.LeavePartyAsync();
        else await voice.JoinPartyAsync(partyId);
    }

    private bool CanUseParty()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (session == null || session.CanUseSocial) return true;
        Failed?.Invoke("Faça login para criar ou entrar em um grupo.");
        return false;
    }
}
