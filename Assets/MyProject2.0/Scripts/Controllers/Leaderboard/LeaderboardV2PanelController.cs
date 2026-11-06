using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>V2-only leaderboard panel. The row prefab owns all visual references and exposes LeaderboardRowController.</summary>
public sealed class LeaderboardV2PanelController : MonoBehaviour
{
    [SerializeField] private LeaderboardV2Service service;
    [SerializeField] private Transform rowsContent;
    [SerializeField] private LeaderboardRowController rowPrefab;
    [SerializeField] private LeaderboardRowController selfRow;
    [SerializeField] private LeaderboardTopPlayerController topPlayer;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private int pageSize = 10;
    private readonly List<GameObject> rows = new();
    private LeaderboardV2Metric metric = LeaderboardV2Metric.Matches;
    private bool loading;

    private void Awake() { if (service == null) service = ServiceHub.Instance != null ? ServiceHub.Instance.LeaderboardV2 : LeaderboardV2Service.Instance; if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; }
    private void OnEnable() { Reload(); }
    public void ShowMatches() => Select(LeaderboardV2Metric.Matches);
    public void ShowEliminations() => Select(LeaderboardV2Metric.Eliminations);
    public void ShowWins() => Select(LeaderboardV2Metric.Wins);
    public void ShowLevel() => Select(LeaderboardV2Metric.Level);
    private void Select(LeaderboardV2Metric value) { if (metric == value) return; metric = value; Reload(); }
    private void Reload()
    {
        foreach (GameObject row in rows) if (row != null) Destroy(row); rows.Clear();
        if (topPlayer != null) topPlayer.gameObject.SetActive(false);
        if (selfRow != null) selfRow.gameObject.SetActive(false);
        Load();
        LoadSelf();
    }
    private void Load()
    {
        if (loading || service == null || rowPrefab == null || rowsContent == null) return;
        loading = true;
        service.Load(metric, pageSize, null, (entries, _) =>
        {
            loading = false;
            foreach (LeaderboardV2Service.PlayerEntry entry in entries)
            {
                LeaderboardRowController row = Instantiate(rowPrefab, rowsContent);
                rows.Add(row.gameObject);
                row.Bind(ToLegacy(entry, metric), ToLegacy(metric), OpenOtherProfile);
            }
            if (entries.Length > 0 && topPlayer != null)
            {
                topPlayer.gameObject.SetActive(true);
                topPlayer.Bind(ToLegacy(entries[0], metric), ToLegacy(metric), OpenOtherProfile);
            }
        });
    }
    private void LoadSelf()
    {
        if (service == null || selfRow == null) return;
        service.LoadSelf(metric, entry =>
        {
            if (entry == null) return;
            selfRow.gameObject.SetActive(true);
            selfRow.Bind(ToLegacy(entry, metric), ToLegacy(metric), null);
        });
    }
    private void OpenOtherProfile(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || (profileService != null && profileService.Current.playerId == playerId)) return;
        otherProfile?.Open(playerId);
    }
    private static LeaderboardPlayerData ToLegacy(LeaderboardV2Service.PlayerEntry data, LeaderboardV2Metric metric) => new() { playerId = data.playerId, nickname = data.nickname, avatarId = data.avatarId, position = data.position, matches = data.matches, eliminations = data.eliminations, wins = data.wins, level = data.level, value = Value(data, metric) };
    private static int Value(LeaderboardV2Service.PlayerEntry data, LeaderboardV2Metric metric) => metric switch { LeaderboardV2Metric.Eliminations => data.eliminations, LeaderboardV2Metric.Wins => data.wins, LeaderboardV2Metric.Level => data.level, _ => data.matches };
    private static LeaderboardMetric ToLegacy(LeaderboardV2Metric value) => value == LeaderboardV2Metric.Eliminations ? LeaderboardMetric.Eliminations : value == LeaderboardV2Metric.Wins ? LeaderboardMetric.Wins : value == LeaderboardV2Metric.Level ? LeaderboardMetric.Level : LeaderboardMetric.Matches;
}
