using System;
using UnityEngine;

/// <summary>Checks the public Function App health endpoint and exposes build metadata to UI.</summary>
public sealed class BackendHealthService : MonoBehaviour
{
    [Serializable] public sealed class HealthSnapshot { public string status; public string serverVersion; public string environment; }

    public static BackendHealthService Instance { get; private set; }
    [SerializeField] private CapivaradaBackendService backend;
    public HealthSnapshot Current { get; private set; } = new HealthSnapshot { status = "offline", serverVersion = "--", environment = "--" };
    public bool IsHealthy { get; private set; }
    public event Action<HealthSnapshot> Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        DontDestroyOnLoad(gameObject);
    }

    public void Check(Action<bool> completed = null)
    {
        if (backend == null)
        {
            SetOffline();
            completed?.Invoke(false);
            return;
        }

        backend.GetPublic<HealthSnapshot>("health", snapshot =>
        {
            Current = snapshot ?? new HealthSnapshot();
            IsHealthy = string.Equals(Current.status, "ok", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Current.status, "online", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(Current.serverVersion)) Current.serverVersion = "--";
            if (string.IsNullOrWhiteSpace(Current.environment)) Current.environment = "--";
            Changed?.Invoke(Current);
            completed?.Invoke(IsHealthy);
        }, _ =>
        {
            SetOffline();
            completed?.Invoke(false);
        });
    }

    private void SetOffline()
    {
        IsHealthy = false;
        Current = new HealthSnapshot { status = "offline", serverVersion = "--", environment = "offline" };
        Changed?.Invoke(Current);
    }
}
