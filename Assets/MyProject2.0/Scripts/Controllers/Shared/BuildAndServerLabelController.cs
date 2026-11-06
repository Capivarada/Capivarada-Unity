using TMPro;
using UnityEngine;

/// <summary>Fills the repeated game-version and server labels from authoritative runtime sources.</summary>
public sealed class BuildAndServerLabelController : MonoBehaviour
{
    [SerializeField] private TMP_Text[] gameVersionLabels;
    [SerializeField] private TMP_Text[] serverLabels;
    [SerializeField] private string serverPrefix = "BR · São Paulo";
    [SerializeField] private BackendHealthService health;

    private void Awake()
    {
        if (health == null) health = ServiceHub.Instance != null ? ServiceHub.Instance.Health : BackendHealthService.Instance;
        RefreshGame();
    }

    private void OnEnable()
    {
        if (health != null) health.Changed += RefreshServer;
        RefreshServer(health != null ? health.Current : null);
    }

    private void OnDisable()
    {
        if (health != null) health.Changed -= RefreshServer;
    }

    public void RefreshGame()
    {
        string text = $"{Application.productName} · v{Application.version}";
        foreach (TMP_Text label in gameVersionLabels) if (label != null) label.text = text;
    }

    private void RefreshServer(BackendHealthService.HealthSnapshot snapshot)
    {
        string suffix = snapshot != null && !string.IsNullOrWhiteSpace(snapshot.serverVersion) && snapshot.serverVersion != "--"
            ? $" · {snapshot.serverVersion}"
            : " · indisponível";
        foreach (TMP_Text label in serverLabels) if (label != null) label.text = serverPrefix + suffix;
    }
}
