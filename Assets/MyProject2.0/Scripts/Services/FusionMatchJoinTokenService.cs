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
            failed?.Invoke("A partida não informou endpoint dedicado. Use AllocateAndRequest para uma partida nova.");
        }, failed);
    }

    /// <summary>Allocates a MPS server and returns only the short-lived direct endpoint/token needed by Fusion.</summary>

public void AllocateAndRequest(
    Action<FusionDedicatedIdentity> succeeded,
    Action<string> failed,
    params string[] preferredRegions)
{
    if (backend == null ||
        authentication == null ||
        !authentication.IsLoggedIn ||
        authentication.IsGuest)
    {
        failed?.Invoke(
            "Entre em uma conta para participar de uma partida.");
        return;
    }

    string[] regions =
        preferredRegions != null && preferredRegions.Length > 0
            ? preferredRegions
            : new[] { "EastUs" };

    Debug.Log(
        "[MPS] Solicitando alocação nas regiões: " +
        string.Join(", ", regions));

    backend.Post<AllocateRequest, AllocateResponse>(
        "matches/allocate",
        new AllocateRequest
        {
            preferredRegions = regions
        },
        response =>
        {
            if (response == null ||
                string.IsNullOrWhiteSpace(response.matchId) ||
                string.IsNullOrWhiteSpace(response.address) ||
                string.IsNullOrWhiteSpace(response.token) ||
                response.port < 1 ||
                response.port > ushort.MaxValue)
            {
                failed?.Invoke(
                    "PlayFab retornou dados de conexão inválidos.");
                return;
            }

            if (response.expiresInSeconds <= 0)
            {
                failed?.Invoke(
                    "O token de entrada não possui validade.");
                return;
            }

            PlayerProfileData current =
                profile != null ? profile.Current : null;

            Debug.Log(
                $"[MPS] Servidor alocado. " +
                $"MatchId={response.matchId}, " +
                $"Region={response.region}, " +
                $"Endpoint={response.address}:{response.port}");

            succeeded?.Invoke(new FusionDedicatedIdentity
            {
                MatchId = response.matchId.Trim(),
                PlayerId = authentication.PlayFabId,
                Nickname =
                    current != null &&
                    !string.IsNullOrWhiteSpace(current.nickname)
                        ? current.nickname
                        : authentication.Nickname,
                AvatarId =
                    current != null &&
                    !string.IsNullOrWhiteSpace(current.avatarId)
                        ? current.avatarId
                        : "avatar1",
                JoinToken = response.token,
                Address = response.address.Trim(),
                Port = (ushort)response.port,
                PhotonRegion = response.photonRegion
            });
        },
        error =>
        {
            Debug.LogError("[MPS] Falha de alocação: " + error);
            failed?.Invoke(error);
        });
}


    [Serializable] private sealed class JoinTokenRequest { public string matchId; }
    [Serializable] private sealed class JoinTokenResponse { public string token; public int expiresInSeconds; }
    [Serializable] private sealed class AllocateRequest { public string[] preferredRegions; }
    [Serializable] private sealed class AllocateResponse { public string matchId; public string address; public int port; public string token; public int expiresInSeconds; public string region; public string photonRegion; }
}
