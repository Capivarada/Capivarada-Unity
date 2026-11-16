using System;
using UnityEngine;

/// <summary>Client facade for the server-authoritative PlayFab party ticket.</summary>
public sealed class FusionMatchmakingService : MonoBehaviour
{
    [Serializable] public sealed class Status
    {
        public string state;
        public string error;
        public int remainingSeconds;
        public int pollAfterSeconds;
        public string matchId;
        public string address;
        public int port;
        public string token;
        public string region;
        public string photonRegion;
    }

    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private PlayerProfileService profile;

    private void Awake()
    {
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        if (authentication == null) authentication = GetComponent<PlayFabAuthManager>();
        if (profile == null) profile = GetComponent<PlayerProfileService>();
    }

    public void StartSearch(Action<Status> succeeded, Action<string> failed)
    {
        if (!CanSearch(failed)) return;
        backend.Post<CapivaradaBackendService.EmptyResponse, Status>(
            "matchmaking/start", new CapivaradaBackendService.EmptyResponse(), succeeded, failed);
    }

    public void GetStatus(Action<Status> succeeded, Action<string> failed)
    {
        if (!CanSearch(failed)) return;
        backend.Get("matchmaking/status", succeeded, failed);
    }

    public void Cancel(Action succeeded, Action<string> failed)
    {
        if (!CanSearch(failed)) return;
        backend.Post<CapivaradaBackendService.EmptyResponse, Status>(
            "matchmaking/cancel", new CapivaradaBackendService.EmptyResponse(),
            _ => succeeded?.Invoke(), failed);
    }

    public bool TryCreateDedicatedIdentity(Status status, out FusionDedicatedIdentity identity)
    {
        identity = default;
        if (status == null || !string.Equals(status.state, "ready", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(status.matchId) || string.IsNullOrWhiteSpace(status.address) ||
            string.IsNullOrWhiteSpace(status.token) || status.port < 1 || status.port > ushort.MaxValue ||
            authentication == null || string.IsNullOrWhiteSpace(authentication.PlayFabId))
            return false;

        PlayerProfileData current = profile != null ? profile.Current : null;
        identity = new FusionDedicatedIdentity
        {
            MatchId = status.matchId.Trim(),
            PlayerId = authentication.PlayFabId,
            Nickname = current != null && !string.IsNullOrWhiteSpace(current.nickname)
                ? current.nickname : authentication.Nickname,
            AvatarId = current != null && !string.IsNullOrWhiteSpace(current.avatarId)
                ? current.avatarId : "avatar1",
            JoinToken = status.token,
            Address = status.address.Trim(),
            Port = (ushort)status.port,
            PhotonRegion = status.photonRegion
        };
        return identity.IsValid;
    }

    private bool CanSearch(Action<string> failed)
    {
        if (backend != null && authentication != null && authentication.IsLoggedIn && !authentication.IsGuest)
            return true;
        failed?.Invoke("Entre em uma conta para procurar uma partida.");
        return false;
    }
}
