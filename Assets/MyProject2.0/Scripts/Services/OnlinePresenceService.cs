using System;
using System.Collections;
using UnityEngine;

/// <summary>Authenticated-player heartbeat and MPS warmup gate. Visitors never use it.</summary>
public sealed class OnlinePresenceService : MonoBehaviour
{
    [SerializeField, Min(10f)] private float heartbeatSeconds = 30f;
    [SerializeField, Min(2f)] private float retrySeconds = 5f;
    private PlayFabAuthManager authentication;
    private CapivaradaBackendService backend;
    private Coroutine heartbeat;
    private bool requesting;
    public bool IsReady { get; private set; }
    public string Status { get; private set; } = "idle";

    private void Start()
    {
        ResolveServices();
        if (authentication != null) authentication.OnLoginSuccess += HandleLogin;
        if (authentication != null && authentication.IsLoggedIn && !authentication.IsGuest) HandleLogin();
    }

    private void OnDestroy()
    {
        if (authentication != null) authentication.OnLoginSuccess -= HandleLogin;
    }

    private void ResolveServices()
    {
        if (authentication == null) authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : PlayFabAuthManager.Instance;
        if (backend == null) backend = ServiceHub.Instance != null ? ServiceHub.Instance.Backend : CapivaradaBackendService.Instance;
    }

    private void HandleLogin()
    {
        ResolveServices();
        if (authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { IsReady = true; Status = "guest"; return; }
        IsReady = false;
        if (heartbeat != null) StopCoroutine(heartbeat);
        heartbeat = StartCoroutine(HeartbeatLoop());
    }

    public IEnumerator WaitUntilReady(Action<string> update, Action<string> failed = null)
    {
        ResolveServices();
        if (authentication == null || authentication.IsGuest) yield break;
        if (heartbeat == null) HandleLogin();
        while (!IsReady)
        {
            update?.Invoke(Status == "warming" ? "Preparando arena..." : "Verificando arena...");
            if (backend == null) { failed?.Invoke("Serviço de arena indisponível."); yield break; }
            yield return new WaitForSecondsRealtime(Mathf.Max(2f, retrySeconds));
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (authentication != null && authentication.IsLoggedIn && !authentication.IsGuest)
        {
            bool completed = false;
            requesting = true;
            backend.Post<CapivaradaBackendService.EmptyResponse, CapacityResponse>("presence/heartbeat", new CapivaradaBackendService.EmptyResponse(), response =>
            {
                IsReady = response.ready;
                Status = string.IsNullOrWhiteSpace(response.status) ? (IsReady ? "ready" : "warming") : response.status;
                completed = true;
                requesting = false;
            }, error =>
            {
                IsReady = false;
                Status = "unavailable";
                Debug.LogWarning("[Presença] Não foi possível atualizar a arena: " + error, this);
                completed = true;
                requesting = false;
            });
            while (!completed) yield return null;
            yield return new WaitForSecondsRealtime(IsReady ? heartbeatSeconds : retrySeconds);
        }
        heartbeat = null;
    }

    [Serializable] private struct CapacityResponse { public bool ready; public string status; public int retryAfterSeconds; }
}
