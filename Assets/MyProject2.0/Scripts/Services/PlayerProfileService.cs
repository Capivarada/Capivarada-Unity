using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[Serializable]
public class PlayerProfileData
{
    public string playerId, nickname, email, avatarId, equippedSkinId;
    public PlayerCosmeticLoadout cosmetics = new();
    public int level = 1, experience, experienceToNext = 1000, matches, eliminations, wins;
    public float playTimeSeconds;
    public List<MatchHistoryData> recentMatches = new();
}

[Serializable]
public class MatchHistoryData { public int placement, eliminations, experience; public string playedAt; }

public class PlayerProfileService : MonoBehaviour
{
    public static PlayerProfileService Instance { get; private set; }
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private CapivaradaBackendService backend;
    public PlayerProfileData Current { get; private set; } = new PlayerProfileData();
    public event Action<PlayerProfileData> Changed;
    public event Action<string> Failed;
    private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) { Destroy(this); } if (backend == null) backend = GetComponent<CapivaradaBackendService>(); }
    public void Load()
    {
        if (authentication == null || !authentication.IsLoggedIn) { Publish(); return; }
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "profile" } }, result => { if (result.Data != null && result.Data.TryGetValue("profile", out UserDataRecord data)) Current = JsonUtility.FromJson<PlayerProfileData>(data.Value) ?? Current; Current.playerId = authentication.PlayFabId; if (string.IsNullOrWhiteSpace(Current.nickname)) Current.nickname = authentication.Nickname; SyncNicknameIndex(); LoadReadOnly(Current, () => LoadV2Progression(Publish)); }, error => Report(error.ErrorMessage));
    }
    public void Save(string nickname, string avatarId, string equippedSkinId, Action done = null)
    {
        if (authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { Report("Faça login para salvar o perfil."); return; }
        Current.playerId = authentication.PlayFabId; Current.nickname = string.IsNullOrWhiteSpace(nickname) ? Current.nickname : nickname.Trim(); Current.avatarId = avatarId ?? Current.avatarId; Current.equippedSkinId = equippedSkinId ?? Current.equippedSkinId; authentication.ChangeNickname(Current.nickname);
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = new Dictionary<string, string> { ["profile"] = JsonUtility.ToJson(Current) }, Permission = UserDataPermission.Public }, _ => { SyncNicknameIndex(); Publish(); done?.Invoke(); }, error => Report(error.ErrorMessage));
    }
    public void SaveCosmetics(PlayerCosmeticLoadout cosmetics, Action done = null)
    {
        if (authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { Report("Faça login para salvar a personalização."); return; }
        Current.cosmetics = cosmetics ?? new PlayerCosmeticLoadout();
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { ["profile"] = JsonUtility.ToJson(Current) },
            Permission = UserDataPermission.Public
        }, _ => { Publish(); done?.Invoke(); }, error => Report(error.ErrorMessage));
    }
    public void LoadOther(string playerId, Action<PlayerProfileData> done)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "GetPublicPlayerProfile", FunctionParameter = new { playerId } }, result => { if (result.Error != null) { Report($"Perfil público: {result.Error.Error}: {result.Error.Message}"); return; } if (result.FunctionResult is not PlayFab.Json.JsonObject data) { Report("Resposta pública de perfil inválida: retorno sem objeto JSON."); return; } PlayerProfileData profile = new PlayerProfileData { playerId = playerId, nickname = Text(data, "nickname"), avatarId = Text(data, "avatarId"), equippedSkinId = Text(data, "equippedSkinId"), level = Number(data, "level", 1), experience = Number(data, "experience", 0), experienceToNext = Number(data, "experienceToNext", 1500), matches = Number(data, "matches", 0), eliminations = Number(data, "eliminations", 0), wins = Number(data, "wins", 0), playTimeSeconds = Number(data, "playTimeSeconds", 0) }; done?.Invoke(profile); }, error => Report(error.ErrorMessage));
    }
    private void LoadReadOnly(PlayerProfileData profile, Action done, string playerId = null)
    {
        PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest { PlayFabId = playerId }, result => { if (result.Data != null) { profile.level = ReadInt(result.Data, "Level", profile.level); profile.experience = ReadInt(result.Data, "CurrentXP", profile.experience); profile.experienceToNext = 1500; profile.playTimeSeconds = ReadInt(result.Data, "PlayTimeSeconds", (int)profile.playTimeSeconds); } LoadStatistics(profile, done, playerId); }, _ => LoadStatistics(profile, done, playerId));
    }
    private void LoadStatistics(PlayerProfileData profile, Action done, string playerId)
    {
        if (!string.IsNullOrEmpty(playerId) && (authentication == null || playerId != authentication.PlayFabId)) { done?.Invoke(); return; }
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), result => { foreach (StatisticValue stat in result.Statistics) { if (stat.StatisticName == "Matches") profile.matches = stat.Value; else if (stat.StatisticName == "Eliminations") profile.eliminations = stat.Value; else if (stat.StatisticName == "Wins") profile.wins = stat.Value; else if (stat.StatisticName == "Level") profile.level = stat.Value; else if (stat.StatisticName == "PlayTimeSeconds") profile.playTimeSeconds = stat.Value; } done?.Invoke(); }, _ => done?.Invoke());
    }
    [Serializable] private class ProgressionResponse { public int matches, eliminations, wins, level; }
    private void LoadV2Progression(Action done)
    {
        if (backend == null) { done?.Invoke(); return; }
        backend.Get<ProgressionResponse>("profiles/me/progression", result => { if (result != null) { Current.matches = result.matches; Current.eliminations = result.eliminations; Current.wins = result.wins; Current.level = Mathf.Max(1, result.level); } done?.Invoke(); }, _ => done?.Invoke());
    }
    [Serializable] private class NicknameIndexInput { public string nickname; }
    private void SyncNicknameIndex()
    {
        if (backend == null || authentication == null || !authentication.IsLoggedIn || authentication.IsGuest || string.IsNullOrWhiteSpace(Current.nickname)) return;
        backend.Post<NicknameIndexInput, CapivaradaBackendService.EmptyResponse>("profiles/me/nickname-index", new NicknameIndexInput { nickname = Current.nickname.Trim() }, _ => { }, _ => { });
    }
    private static int ReadInt(Dictionary<string, UserDataRecord> data, string key, int fallback) => data.TryGetValue(key, out UserDataRecord value) && int.TryParse(value.Value, out int parsed) ? parsed : fallback;
    private static string Text(PlayFab.Json.JsonObject data, string key) => data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
    private static int Number(PlayFab.Json.JsonObject data, string key, int fallback) { try { return data.TryGetValue(key, out object value) ? Convert.ToInt32(value) : fallback; } catch { return fallback; } }
    private void Publish() => Changed?.Invoke(Current); private void Report(string message) { Failed?.Invoke(message); Debug.LogWarning(message, this); }
}
