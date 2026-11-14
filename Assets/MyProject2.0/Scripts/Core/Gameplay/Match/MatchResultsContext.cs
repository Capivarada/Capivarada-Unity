using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transient client snapshot passed from the finished Fusion arena to Lobby.
/// It is presentation data only; progression remains authoritative in Azure.
/// </summary>
public static class MatchResultsContext
{
    public sealed class Entry
    {
        public string PlayerId { get; init; } = string.Empty;
        public string Nickname { get; init; } = "Capivara";
        public string AvatarId { get; init; } = "avatar1";
        public bool IsBot { get; init; }
        public bool IsLocal { get; init; }
        public int Placement { get; init; }
        public int Eliminations { get; init; }
        public int RoundsSurvived { get; init; }
    }

    public sealed class Snapshot
    {
        public IReadOnlyList<Entry> Entries { get; init; } = Array.Empty<Entry>();
        public int PreviousLevel { get; init; } = 1;
        public int PreviousExperience { get; init; }
        public int ExperienceToNext { get; init; } = 1500;
        public int ExperienceEarned { get; init; }
        public int ArenaPointsEarned { get; init; }
    }

    public static Snapshot Pending { get; private set; }

    public static void CaptureFromNetwork()
    {
        PlayerProfileData profile = PlayerProfileService.Instance != null
            ? PlayerProfileService.Instance.Current
            : null;
        string localPlayerId = profile != null ? profile.playerId : string.Empty;
        List<Entry> entries = new();
        FusionRoundController rounds = UnityEngine.Object.FindFirstObjectByType<FusionRoundController>();
        IReadOnlyList<FusionPublishedResult> published = rounds != null
            ? rounds.PublishedResults
            : Array.Empty<FusionPublishedResult>();
        foreach (FusionPublishedResult result in published)
        {
            if (result == null || result.placement <= 0) continue;
            entries.Add(new Entry
            {
                PlayerId = result.playerId,
                Nickname = result.nickname,
                AvatarId = result.avatarId,
                Placement = result.placement,
                Eliminations = result.eliminations,
                RoundsSurvived = result.roundsSurvived,
                IsBot = result.isBot,
                IsLocal = !string.IsNullOrWhiteSpace(localPlayerId) &&
                    string.Equals(result.playerId, localPlayerId, StringComparison.Ordinal)
            });
        }

        entries.Sort((left, right) => left.Placement != right.Placement
            ? left.Placement.CompareTo(right.Placement)
            : string.Compare(left.Nickname, right.Nickname, StringComparison.OrdinalIgnoreCase));

        Entry localEntry = entries.Find(entry => entry.IsLocal);
        Pending = new Snapshot
        {
            Entries = entries,
            PreviousLevel = Mathf.Max(1, profile != null ? profile.level : 1),
            PreviousExperience = Mathf.Max(0, profile != null ? profile.experience : 0),
            ExperienceToNext = Mathf.Max(1, profile != null ? profile.experienceToNext : 1500),
            ExperienceEarned = localEntry != null ? ExperienceAward(localEntry) : 0,
            ArenaPointsEarned = localEntry != null ? ArenaPointsAward(localEntry) : 0
        };
    }

    public static Snapshot Consume()
    {
        Snapshot snapshot = Pending;
        Pending = null;
        return snapshot;
    }

    /// <summary>Creates a partial result for a dead spectator leaving early.</summary>
    public static void CaptureKnownFromNetwork()
    {
        PlayerProfileData profile = PlayerProfileService.Instance != null ? PlayerProfileService.Instance.Current : null;
        string localPlayerId = profile != null ? profile.playerId : string.Empty;
        List<Entry> entries = new();
        foreach (FusionNetworkPlayer player in UnityEngine.Object.FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player == null || player.Placement <= 0) continue;
            entries.Add(new Entry
            {
                PlayerId = player.NetworkPlayerId, Nickname = player.NetworkNickname, AvatarId = player.NetworkAvatarId,
                Placement = player.Placement, Eliminations = player.Eliminations, RoundsSurvived = player.RoundsSurvived,
                IsLocal = player.Object != null && player.Object.HasInputAuthority ||
                    (!string.IsNullOrWhiteSpace(localPlayerId) && player.NetworkPlayerId == localPlayerId)
            });
        }
        foreach (FusionNetworkBot bot in UnityEngine.Object.FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (bot != null && bot.Placement > 0)
                entries.Add(new Entry { Nickname = bot.NetworkNickname, IsBot = true, Placement = bot.Placement });
        entries.Sort((left, right) => left.Placement != right.Placement
            ? left.Placement.CompareTo(right.Placement)
            : string.Compare(left.Nickname, right.Nickname, StringComparison.OrdinalIgnoreCase));
        Entry localEntry = entries.Find(entry => entry.IsLocal);
        Pending = new Snapshot
        {
            Entries = entries,
            PreviousLevel = Mathf.Max(1, profile != null ? profile.level : 1),
            PreviousExperience = Mathf.Max(0, profile != null ? profile.experience : 0),
            ExperienceToNext = Mathf.Max(1, profile != null ? profile.experienceToNext : 1500),
            ExperienceEarned = localEntry != null ? ExperienceAward(localEntry) : 0,
            ArenaPointsEarned = localEntry != null ? ArenaPointsAward(localEntry) : 0
        };
    }

    private static int ExperienceAward(Entry entry)
    {
        int placement = Mathf.Max(1, entry.Placement);
        int placementAward = placement == 1 ? 400 : placement == 2 ? 300 : placement == 3 ? 200 : 100;
        return placementAward + Mathf.Max(0, entry.Eliminations) * 50 +
            Mathf.Max(0, entry.RoundsSurvived) * 20;
    }

    private static int ArenaPointsAward(Entry entry) => 20 +
        Mathf.Max(0, entry.Eliminations) * 10 + (entry.Placement == 1 ? 80 : 0);
}
