using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[Serializable]
public class PlayerProfileData
{
    public string playerId, nickname, email, avatarId, equippedSkinId, createdAtUtc;
    public PlayerCosmeticLoadout cosmetics = new();
    public int level = 1, experience, experienceToNext = 1500, matches, eliminations, wins;
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
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "profile" } }, result => { if (result.Data != null && result.Data.TryGetValue("profile", out UserDataRecord data)) Current = JsonUtility.FromJson<PlayerProfileData>(data.Value) ?? Current; Current.playerId = authentication.PlayFabId; if (string.IsNullOrWhiteSpace(Current.nickname)) Current.nickname = authentication.Nickname; LoadAccountCreation(() => { SyncNicknameIndex(); LoadReadOnly(Current, () => LoadV2Progression(Publish)); }); }, error => Report(error.ErrorMessage));
    }
    public void Save(string nickname, string avatarId, string equippedSkinId, Action done = null)
    {
        if (authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { Report("Faça login para salvar o perfil."); return; }
        Current.playerId = authentication.PlayFabId; Current.nickname = string.IsNullOrWhiteSpace(nickname) ? Current.nickname : nickname.Trim(); Current.avatarId = avatarId ?? Current.avatarId; Current.equippedSkinId = equippedSkinId ?? Current.equippedSkinId; authentication.ChangeNickname(Current.nickname);
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = new Dictionary<string, string> { ["profile"] = JsonUtility.ToJson(Current) }, Permission = UserDataPermission.Public }, _ =>
        {
            SyncNicknameIndex();
            SyncLeaderboardPresentation(() => { Publish(); done?.Invoke(); });
        }, error => Report(error.ErrorMessage));
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
        if (string.IsNullOrWhiteSpace(playerId)) { Report("ID de jogador inválido."); return; }
        if (backend == null) { Report("CapivaradaBackendService não foi configurado em Services."); return; }
        backend.GetPublic<PublicProfileResponse>("profiles/" + Uri.EscapeDataString(playerId), result =>
        {
            if (result == null) { Report("Resposta pública de perfil inválida."); return; }
            done?.Invoke(new PlayerProfileData
            {
                playerId = result.playerId,
                nickname = result.nickname,
                avatarId = result.avatarId,
                equippedSkinId = result.equippedSkinId,
                level = Mathf.Max(1, result.level),
                experience = result.experience,
                experienceToNext = result.experienceToNext > 0 ? result.experienceToNext : 1500,
                matches = result.matches,
                eliminations = result.eliminations,
                wins = result.wins,
                playTimeSeconds = result.playTimeSeconds
            });
        }, error => Report("Perfil público: " + error));
    }
    private void LoadAccountCreation(Action done)
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            if (result.AccountInfo != null) Current.createdAtUtc = result.AccountInfo.Created.ToUniversalTime().ToString("O");
            done?.Invoke();
        }, _ => done?.Invoke());
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
    [Serializable]
    private class ProgressionResponse
    {
        public int matches, eliminations, wins, level;
        public int experience, experienceToNext, totalExperience, playTimeSeconds;
    }
    private void LoadV2Progression(Action done)
    {
        if (backend == null) { done?.Invoke(); return; }
        backend.Get<ProgressionResponse>("profiles/me/progression", result =>
        {
            if (result != null)
            {
                Current.matches = result.matches;
                Current.eliminations = result.eliminations;
                Current.wins = result.wins;
                Current.level = Mathf.Max(1, result.level);
                Current.experience = Mathf.Max(0, result.experience);
                Current.experienceToNext = result.experienceToNext > 0 ? result.experienceToNext : 1500;
                Current.playTimeSeconds = Mathf.Max(0, result.playTimeSeconds);
            }
            done?.Invoke();
        }, _ => done?.Invoke());
    }
    [Serializable] private class NicknameIndexInput { public string nickname; }
    private void SyncNicknameIndex()
    {
        if (backend == null || authentication == null || !authentication.IsLoggedIn || authentication.IsGuest || string.IsNullOrWhiteSpace(Current.nickname)) return;
        backend.Post<NicknameIndexInput, CapivaradaBackendService.EmptyResponse>("profiles/me/nickname-index", new NicknameIndexInput { nickname = Current.nickname.Trim() }, _ => { }, _ => { });
    }
    [Serializable] private class LeaderboardPresentationInput { public string nickname, avatarId; }
    private void SyncLeaderboardPresentation(Action done)
    {
        if (backend == null || authentication == null || !authentication.IsLoggedIn || authentication.IsGuest)
        {
            done?.Invoke();
            return;
        }

        backend.Post<LeaderboardPresentationInput, CapivaradaBackendService.EmptyResponse>(
            "profiles/me/presentation",
            new LeaderboardPresentationInput { nickname = Current.nickname, avatarId = Current.avatarId },
            _ => done?.Invoke(),
            _ => done?.Invoke());
    }
    private static int ReadInt(Dictionary<string, UserDataRecord> data, string key, int fallback) => data.TryGetValue(key, out UserDataRecord value) && int.TryParse(value.Value, out int parsed) ? parsed : fallback;
    [Serializable] private class PublicProfileResponse { public string playerId, nickname, avatarId, equippedSkinId; public int level, experience, experienceToNext, matches, eliminations, wins; public float playTimeSeconds; }
    private void Publish() => Changed?.Invoke(Current); private void Report(string message) { Failed?.Invoke(message); Debug.LogWarning(message, this); }
}
