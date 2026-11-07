using System;
using UnityEngine;

/// <summary>
/// Obtains a short-lived, match-scoped proof from Azure for an authenticated
/// player. The proof can travel through Fusion to the dedicated server but is
/// not a PlayFab credential and cannot call normal game APIs.
/// </summary>
public sealed class FusionMatchJoinTokenService : MonoBehaviour
{
    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private PlayerProfileService profile;

    private void Awake()
    {
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        if (authentication == null) authentication = GetComponent<PlayFabAuthManager>();
        if (profile == null) profile = GetComponent<PlayerProfileService>();
    }

    public void Request(string matchId, Action<FusionDedicatedIdentity> succeeded, Action<string> failed)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            failed?.Invoke("A partida dedicada não forneceu um identificador válido.");
            return;
        }
        if (backend == null || authentication == null || !authentication.IsLoggedIn || authentication.IsGuest)
        {
            failed?.Invoke("Entre em uma conta para participar de uma partida com progressão.");
            return;
        }

        backend.Post<JoinTokenRequest, JoinTokenResponse>("matches/join-token", new JoinTokenRequest { matchId = matchId.Trim() }, response =>
        {
            if (response == null || string.IsNullOrWhiteSpace(response.token))
            {
                failed?.Invoke("O servidor não retornou o token de entrada da partida.");
                return;
            }

            PlayerProfileData current = profile != null ? profile.Current : null;
            succeeded?.Invoke(new FusionDedicatedIdentity
            {
                MatchId = matchId.Trim(),
                PlayerId = authentication.PlayFabId,
                Nickname = current != null && !string.IsNullOrWhiteSpace(current.nickname) ? current.nickname : authentication.Nickname,
                AvatarId = current != null && !string.IsNullOrWhiteSpace(current.avatarId) ? current.avatarId : "avatar1",
                JoinToken = response.token
            });
        }, failed);
    }

    [Serializable] private sealed class JoinTokenRequest { public string matchId; }
    [Serializable] private sealed class JoinTokenResponse { public string token; public int expiresInSeconds; }
}
