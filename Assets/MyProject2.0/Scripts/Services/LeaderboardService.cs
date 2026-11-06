using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

public enum LeaderboardMetric { Eliminations, Matches, Wins, Level }
[Serializable] public class LeaderboardPlayerData { public string playerId, nickname, avatarId; public int position, value, eliminations, matches, wins, level; }
public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }
    public event Action<string> Failed;
    private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) Destroy(this); }
    public void Load(LeaderboardMetric metric, int start, int amount, Action<IReadOnlyList<LeaderboardPlayerData>> done)
    {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest { StatisticName = Statistic(metric), StartPosition = Mathf.Max(0, start), MaxResultsCount = Mathf.Clamp(amount, 1, 100), ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true } }, result => { List<LeaderboardPlayerData> list = new(); foreach (PlayerLeaderboardEntry player in result.Leaderboard) list.Add(new LeaderboardPlayerData { playerId = player.PlayFabId, nickname = string.IsNullOrWhiteSpace(player.DisplayName) ? "Capivara" : player.DisplayName, position = player.Position + 1, value = player.StatValue }); HydrateReadOnly(list, metric, done); }, error => Failed?.Invoke(error.ErrorMessage));
    }
    public void LoadAroundMe(LeaderboardMetric metric, Action<IReadOnlyList<LeaderboardPlayerData>> done) { PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest { StatisticName = Statistic(metric), MaxResultsCount = 1, ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true } }, result => { List<LeaderboardPlayerData> list = new(); foreach (PlayerLeaderboardEntry player in result.Leaderboard) list.Add(new LeaderboardPlayerData { playerId = player.PlayFabId, nickname = player.DisplayName, position = player.Position + 1, value = player.StatValue }); done?.Invoke(list); }, error => Failed?.Invoke(error.ErrorMessage)); }
    public static string Statistic(LeaderboardMetric metric) => metric == LeaderboardMetric.Matches ? "Matches" : metric == LeaderboardMetric.Wins ? "Wins" : metric == LeaderboardMetric.Level ? "Level" : "Eliminations";
    private void HydrateReadOnly(List<LeaderboardPlayerData> list, LeaderboardMetric metric, Action<IReadOnlyList<LeaderboardPlayerData>> done)
    {
        if (list.Count == 0) { done?.Invoke(list); return; }
        int pending = list.Count;
        foreach (LeaderboardPlayerData player in list)
        {
            if (metric == LeaderboardMetric.Matches) player.matches = player.value; else if (metric == LeaderboardMetric.Eliminations) player.eliminations = player.value; else if (metric == LeaderboardMetric.Wins) player.wins = player.value; else player.level = player.value;
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "GetPublicPlayerProfile", FunctionParameter = new { playerId = player.playerId } }, result => { ApplyPublic(player, result.FunctionResult as JsonObject); if (--pending == 0) done?.Invoke(list); }, _ => { if (--pending == 0) done?.Invoke(list); });
        }
    }
    private static int Read(Dictionary<string, UserDataRecord> data, string key) => data.TryGetValue(key, out UserDataRecord v) && int.TryParse(v.Value, out int value) ? value : 0;
    private static void ApplyPublic(LeaderboardPlayerData player, JsonObject data)
    {
        if (data == null) return; player.nickname = Text(data, "nickname", player.nickname); player.avatarId = Text(data, "avatarId", "avatar1"); player.matches = Number(data, "matches", player.matches); player.eliminations = Number(data, "eliminations", player.eliminations); player.wins = Number(data, "wins", player.wins); player.level = Number(data, "level", player.level);
    }
    private static string Text(JsonObject data, string key, string fallback) => data.TryGetValue(key, out object value) && value != null ? value.ToString() : fallback;
    private static int Number(JsonObject data, string key, int fallback) { try { return data.TryGetValue(key, out object value) ? System.Convert.ToInt32(value) : fallback; } catch { return fallback; } }
}
