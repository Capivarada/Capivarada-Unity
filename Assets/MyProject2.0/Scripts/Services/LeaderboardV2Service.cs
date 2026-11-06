using System;
using UnityEngine;

public enum LeaderboardV2Metric { Matches, Eliminations, Wins, Level }

/// <summary>Reads the V2 leaderboard through the authenticated backend, which hydrates public player data consistently for every row.</summary>
public sealed class LeaderboardV2Service : MonoBehaviour
{
    [Serializable] public class PlayerEntry
    {
        public string playerId;
        public string nickname;
        public string avatarId;
        public int position;
        public int matches;
        public int eliminations;
        public int wins;
        public int level;
    }
    [Serializable] private class LeaderboardResponse { public PlayerEntry[] entries; public string continuationToken; }
    [Serializable] private class AroundResponse { public PlayerEntry entry; }

    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private string matchesLeaderboard = "RankingMatches";
    [SerializeField] private string eliminationsLeaderboard = "RankingEliminations";
    [SerializeField] private string winsLeaderboard = "RankingWins";
    [SerializeField] private string levelLeaderboard = "RankingLevel";
    public static LeaderboardV2Service Instance { get; private set; }
    public event Action<string> Failed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        DontDestroyOnLoad(gameObject);
    }

    public void Load(LeaderboardV2Metric metric, int pageSize, string continuationToken, Action<PlayerEntry[], string> succeeded)
    {
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        string route = "leaderboards/" + Uri.EscapeDataString(IdFor(metric)) + "?count=" + Mathf.Clamp(pageSize, 1, 100);
        if (!string.IsNullOrWhiteSpace(continuationToken)) route += "&continuationToken=" + Uri.EscapeDataString(continuationToken);
        backend.Get<LeaderboardResponse>(route, result => succeeded?.Invoke(result?.entries ?? Array.Empty<PlayerEntry>(), result?.continuationToken), Failed);
    }

    public void LoadSelf(LeaderboardV2Metric metric, Action<PlayerEntry> succeeded)
    {
        if (backend == null) { Failed?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        backend.Get<AroundResponse>("leaderboards/" + Uri.EscapeDataString(IdFor(metric)) + "/me", result => succeeded?.Invoke(result?.entry), Failed);
    }

    public string IdFor(LeaderboardV2Metric metric)
    {
        return metric switch
        {
            LeaderboardV2Metric.Eliminations => eliminationsLeaderboard,
            LeaderboardV2Metric.Wins => winsLeaderboard,
            LeaderboardV2Metric.Level => levelLeaderboard,
            _ => matchesLeaderboard
        };
    }
}
