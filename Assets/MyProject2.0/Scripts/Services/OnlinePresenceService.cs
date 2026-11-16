using System;
using System.Collections;
using UnityEngine;

/// <summary>Authenticated-player heartbeat and MPS warmup gate. Visitors never use it.</summary>
public sealed class OnlinePresenceService : MonoBehaviour
{
    [SerializeField, Min(10f)] private float heartbeatSeconds = 30f;
    [SerializeField, Min(2f)] private float retrySeconds = 5f;
    [SerializeField, Min(60f)] private float estimatedWarmupSeconds = 420f;
    private PlayFabAuthManager authentication;
    private CapivaradaBackendService backend;
    private Coroutine heartbeat;
    private float warmingStartedAt = -1f;
    private bool firstArenaWarmup;
    private string lastDebugSnapshot;
    public bool IsReady { get; private set; }
    public string Status { get; private set; } = "idle";
    public float WarmupProgress => IsReady ? .75f : warmingStartedAt < 0f ? 0f : Mathf.Clamp01((Time.realtimeSinceStartup - warmingStartedAt) / estimatedWarmupSeconds) * .75f;
    public bool IsFirstArenaWarmup => !IsReady && firstArenaWarmup;

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
        warmingStartedAt = -1f;
        firstArenaWarmup = false;
        if (heartbeat != null) StopCoroutine(heartbeat);
        heartbeat = StartCoroutine(HeartbeatLoop());
    }

    public IEnumerator WaitUntilReady(Action<string, float> update, Action<string> failed = null)
    {
        ResolveServices();
        if (authentication == null || authentication.IsGuest) yield break;
        if (heartbeat == null) HandleLogin();
        while (!IsReady)
        {
            update?.Invoke(LoadingMessage(), WarmupProgress);
            if (backend == null) { failed?.Invoke("Serviço de arena indisponível."); yield break; }
            yield return new WaitForSecondsRealtime(Mathf.Max(2f, retrySeconds));
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (authentication != null && authentication.IsLoggedIn && !authentication.IsGuest)
        {
            bool completed = false;
            backend.Post<CapivaradaBackendService.EmptyResponse, CapacityResponse>("presence/heartbeat", new CapivaradaBackendService.EmptyResponse(), response =>
            {
                IsReady = response.ready;
                Status = string.IsNullOrWhiteSpace(response.status) ? (IsReady ? "ready" : "warming") : response.status;
                lastCapacity = response;
                if (!IsReady && Status == "warming" && response.onlinePlayers == 1 &&
                    response.standingByServers == 0 && response.proppingServers == 0 && response.activeServers == 0)
                    firstArenaWarmup = true;
                if (!IsReady && warmingStartedAt < 0f) warmingStartedAt = Time.realtimeSinceStartup;
                LogCapacity(response);
                completed = true;
            }, error =>
            {
                IsReady = false;
                Status = "unavailable";
                Debug.LogWarning("[Presença] Não foi possível atualizar a arena: " + error, this);
                completed = true;
            });
            while (!completed) yield return null;
            yield return new WaitForSecondsRealtime(IsReady ? heartbeatSeconds : retrySeconds);
        }
        heartbeat = null;
    }

    private string LoadingMessage()
    {
        if (IsReady) return "Arena pronta. Carregando lobby...";
        if (Status == "warming")
        {
            if (lastCapacity.proppingServers > 0) return "Preparando arena...";
            if (lastCapacity.activeServers > 0 && lastCapacity.standingByServers == 0) return "Partida em andamento. Preparando a próxima arena...";
            return "Preparando arena...";
        }
        return Status == "unconfigured" ? "Arena ainda não foi configurada." : "Verificando disponibilidade da arena...";
    }

    private CapacityResponse lastCapacity;
    private void LogCapacity(CapacityResponse response)
    {
        string snapshot = $"status={Status}; ready={response.ready}; online={response.onlinePlayers}; propping={response.proppingServers}; active={response.activeServers}; standby={response.standingByServers}";
        if (snapshot == lastDebugSnapshot) return;
        lastDebugSnapshot = snapshot;
        Debug.Log("[Presença] MPS " + snapshot, this);
    }

    [Serializable] private struct CapacityResponse
    {
        public bool ready;
        public string status;
        public int retryAfterSeconds;
        public int onlinePlayers;
        public int standingByServers;
        public int proppingServers;
        public int activeServers;
    }
}
