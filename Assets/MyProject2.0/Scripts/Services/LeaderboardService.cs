using System;
using System.Collections.Generic;
using UnityEngine;

public enum LeaderboardMetric { Eliminations, Matches, Wins, Level }

[Serializable]
public class LeaderboardPlayerData
{
    public string playerId, nickname, avatarId;
    public int position, value, eliminations, matches, wins, level;
}

/// <summary>Leaderboard V2 facade. Data is supplied by the Azure API, never
/// by legacy PlayerStatistics or CloudScript.</summary>
public class LeaderboardService : MonoBehaviour
{
    [Serializable] private class LeaderboardResponse { public LeaderboardPlayerData[] entries; }
    [Serializable] private class SelfResponse { public LeaderboardPlayerData entry; }

    [SerializeField] private CapivaradaBackendService backend;
    public static LeaderboardService Instance { get; private set; }
    public event Action<string> Failed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
    }

    public void Load(LeaderboardMetric metric, int start, int amount, Action<IReadOnlyList<LeaderboardPlayerData>> done)
    {
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); done?.Invoke(Array.Empty<LeaderboardPlayerData>()); return; }
        string route = "leaderboards/" + LeaderboardName(metric) + "?count=" + Mathf.Clamp(amount, 1, 100) + "&start=" + Mathf.Max(0, start);
        backend.GetPublic<LeaderboardResponse>(route, response =>
        {
            LeaderboardPlayerData[] entries = response?.entries ?? Array.Empty<LeaderboardPlayerData>();
            foreach (LeaderboardPlayerData entry in entries) ApplyMetricValue(entry, metric);
            done?.Invoke(entries);
        }, error => { Failed?.Invoke(error); done?.Invoke(Array.Empty<LeaderboardPlayerData>()); });
    }

    public void LoadAroundMe(LeaderboardMetric metric, Action<IReadOnlyList<LeaderboardPlayerData>> done)
    {
        if (backend == null) { done?.Invoke(Array.Empty<LeaderboardPlayerData>()); return; }
        backend.Get<SelfResponse>("leaderboards/" + LeaderboardName(metric) + "/me", response =>
        {
            if (response?.entry == null) { done?.Invoke(Array.Empty<LeaderboardPlayerData>()); return; }
            ApplyMetricValue(response.entry, metric);
            done?.Invoke(new[] { response.entry });
        }, _ => done?.Invoke(Array.Empty<LeaderboardPlayerData>()));
    }

    public static string Statistic(LeaderboardMetric metric) => LeaderboardName(metric);
    private static string LeaderboardName(LeaderboardMetric metric) => metric == LeaderboardMetric.Matches ? "RankingMatches" : metric == LeaderboardMetric.Wins ? "RankingWins" : metric == LeaderboardMetric.Level ? "RankingLevel" : "RankingEliminations";
    private static void ApplyMetricValue(LeaderboardPlayerData player, LeaderboardMetric metric)
    {
        if (player == null) return;
        player.value = metric == LeaderboardMetric.Matches ? player.matches : metric == LeaderboardMetric.Wins ? player.wins : metric == LeaderboardMetric.Level ? player.level : player.eliminations;
    }
}
