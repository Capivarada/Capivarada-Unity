using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

/// <summary>Runtime projection assembled from the four independent Player Data records.</summary>
[Serializable]
public class PlayerProfileData
{
    public string playerId, nickname, email, avatarId, createdAtUtc;
    public PlayerCosmeticLoadout cosmetics = new();
    public int level = 1, experience, experienceToNext = 1500, matches, eliminations, wins;
    public long AccountAgeSeconds
    {
        get
        {
            return DateTime.TryParse(createdAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime created)
                ? Math.Max(0, (long)(DateTime.UtcNow - created.ToUniversalTime()).TotalSeconds) : 0;
        }
    }
}

[Serializable] public class MatchHistoryData { public int placement, eliminations, experience; public string playedAt; }
[Serializable] public class PlayerIdentityRecord { public string playerId, nickname, email, avatarId, createdAtUtc; }
[Serializable] public class PlayerProgressionRecord { public int level = 1, experience; }
[Serializable] public class PlayerStatisticsRecord { public int matches, wins, eliminations; }

public class PlayerProfileService : MonoBehaviour
{
    private const string ProfileKey = "profile";
    private const string CosmeticsKey = "cosmetics";
    private const string ProgressionKey = "progression";
    private const string StatisticsKey = "statistics";

    public static PlayerProfileService Instance { get; private set; }
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private CapivaradaBackendService backend;
    public PlayerProfileData Current { get; private set; } = new();
    public event Action<PlayerProfileData> Changed;
    public event Action<string> Failed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
    }

    public void Load()
    {
        if (authentication == null || !authentication.IsLoggedIn) { Publish(); return; }
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            Keys = new List<string> { ProfileKey, CosmeticsKey, ProgressionKey, StatisticsKey }
        }, result =>
        {
            MergeRecords(result.Data, Current);
            Current.playerId = authentication.PlayFabId;
            if (string.IsNullOrWhiteSpace(Current.nickname)) Current.nickname = authentication.Nickname;
            LoadAccountCreation(() => MigrateLegacyIfNeeded(result.Data, () => { SyncNicknameIndex(); SyncLeaderboardPresentation(Publish); }));
        }, error => Report(error.ErrorMessage));
    }

    public void Save(string nickname, string avatarId, string _, Action done = null)
    {
        if (!CanSave()) return;
        Current.playerId = authentication.PlayFabId;
        Current.nickname = string.IsNullOrWhiteSpace(nickname) ? Current.nickname : nickname.Trim();
        Current.avatarId = avatarId ?? Current.avatarId;
        authentication.ChangeNickname(Current.nickname);
        PlayerIdentityRecord identity = new()
        {
            playerId = Current.playerId, nickname = Current.nickname, email = Current.email,
            avatarId = Current.avatarId, createdAtUtc = Current.createdAtUtc
        };
        UpdatePublic(ProfileKey, JsonUtility.ToJson(identity), () =>
        {
            SyncNicknameIndex();
            SyncLeaderboardPresentation(() => { Publish(); done?.Invoke(); });
        });
    }

    public void SaveCosmetics(PlayerCosmeticLoadout cosmetics, Action done = null)
    {
        if (!CanSave()) return;
        Current.cosmetics = cosmetics ?? new PlayerCosmeticLoadout();
        UpdatePublic(CosmeticsKey, JsonUtility.ToJson(Current.cosmetics), () => { Publish(); done?.Invoke(); });
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
                playerId = result.playerId, nickname = result.nickname, email = result.email,
                avatarId = result.avatarId, createdAtUtc = result.createdAtUtc,
                cosmetics = result.cosmetics ?? new PlayerCosmeticLoadout(), level = Mathf.Max(1, result.progression.level),
                experience = Mathf.Max(0, result.progression.experience), experienceToNext = 1500,
                matches = result.statistics.matches, wins = result.statistics.wins,
                eliminations = result.statistics.eliminations
            });
        }, error => Report("Perfil público: " + error));
    }

    private bool CanSave()
    {
        if (authentication != null && authentication.IsLoggedIn && !authentication.IsGuest) return true;
        Report("Faça login para salvar o perfil.");
        return false;
    }

    private void UpdatePublic(string key, string value, Action done)
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { [key] = value }, Permission = UserDataPermission.Public
        }, _ => done?.Invoke(), error => Report(error.ErrorMessage));
    }

    private void MigrateLegacyIfNeeded(Dictionary<string, UserDataRecord> data, Action done)
    {
        if (data != null && data.ContainsKey(CosmeticsKey) && data.ContainsKey(ProgressionKey) && data.ContainsKey(StatisticsKey)) { done?.Invoke(); return; }
        PlayerIdentityRecord identity = new()
        {
            playerId = Current.playerId, nickname = Current.nickname, email = Current.email,
            avatarId = Current.avatarId, createdAtUtc = Current.createdAtUtc
        };
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Permission = UserDataPermission.Public,
            Data = new Dictionary<string, string>
            {
                [ProfileKey] = JsonUtility.ToJson(identity),
                [CosmeticsKey] = JsonUtility.ToJson(Current.cosmetics ?? new PlayerCosmeticLoadout()),
                [ProgressionKey] = JsonUtility.ToJson(new PlayerProgressionRecord { level = Current.level, experience = Current.experience }),
                [StatisticsKey] = JsonUtility.ToJson(new PlayerStatisticsRecord { matches = Current.matches, wins = Current.wins, eliminations = Current.eliminations })
            }
        }, _ => done?.Invoke(), error => { Report("Migração do perfil: " + error.ErrorMessage); done?.Invoke(); });
    }

    private static void MergeRecords(Dictionary<string, UserDataRecord> data, PlayerProfileData target)
    {
        if (data == null) return;
        if (TryRead(data, ProfileKey, out PlayerIdentityRecord identity))
        {
            target.playerId = identity.playerId; target.nickname = identity.nickname; target.email = identity.email;
            target.avatarId = identity.avatarId; target.createdAtUtc = identity.createdAtUtc;
        }
        if (TryRead(data, CosmeticsKey, out PlayerCosmeticLoadout cosmetics)) target.cosmetics = cosmetics ?? new PlayerCosmeticLoadout();
        if (TryRead(data, ProgressionKey, out PlayerProgressionRecord progression))
        {
            target.level = Mathf.Max(1, progression.level); target.experience = Mathf.Max(0, progression.experience);
        }
        if (TryRead(data, StatisticsKey, out PlayerStatisticsRecord statistics))
        {
            target.matches = Mathf.Max(0, statistics.matches); target.wins = Mathf.Max(0, statistics.wins);
            target.eliminations = Mathf.Max(0, statistics.eliminations);
        }

        // One-time read compatibility: old accounts are not discarded before
        // their identity/cosmetics are saved back under the split schema.
        if ((!data.ContainsKey(CosmeticsKey) || !data.ContainsKey(ProgressionKey) || !data.ContainsKey(StatisticsKey)) &&
            data.TryGetValue(ProfileKey, out UserDataRecord legacyRecord))
        {
            PlayerProfileData legacy = JsonUtility.FromJson<PlayerProfileData>(legacyRecord.Value);
            if (legacy != null)
            {
                if (!data.ContainsKey(CosmeticsKey)) target.cosmetics = legacy.cosmetics ?? target.cosmetics;
                if (!data.ContainsKey(ProgressionKey)) { target.level = Mathf.Max(1, legacy.level); target.experience = Mathf.Max(0, legacy.experience % 1500); }
                if (!data.ContainsKey(StatisticsKey)) { target.matches = legacy.matches; target.wins = legacy.wins; target.eliminations = legacy.eliminations; }
            }
        }
        target.experienceToNext = 1500;
    }

    private static bool TryRead<T>(Dictionary<string, UserDataRecord> data, string key, out T value) where T : class
    {
        value = null;
        if (!data.TryGetValue(key, out UserDataRecord record) || string.IsNullOrWhiteSpace(record.Value)) return false;
        value = JsonUtility.FromJson<T>(record.Value);
        return value != null;
    }

    private void LoadAccountCreation(Action done)
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            if (result.AccountInfo != null) Current.createdAtUtc = result.AccountInfo.Created.ToUniversalTime().ToString("O");
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
        if (backend == null || authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { done?.Invoke(); return; }
        backend.Post<LeaderboardPresentationInput, CapivaradaBackendService.EmptyResponse>("profiles/me/presentation", new LeaderboardPresentationInput { nickname = Current.nickname, avatarId = Current.avatarId }, _ => done?.Invoke(), _ => done?.Invoke());
    }
    [Serializable] private class PublicProfileResponse
    {
        public string playerId, nickname, email, avatarId, createdAtUtc;
        public PlayerCosmeticLoadout cosmetics;
        public PlayerProgressionRecord progression = new();
        public PlayerStatisticsRecord statistics = new();
    }
    private void Publish() => Changed?.Invoke(Current);
    private void Report(string message) { Failed?.Invoke(message); Debug.LogWarning(message, this); }
}
