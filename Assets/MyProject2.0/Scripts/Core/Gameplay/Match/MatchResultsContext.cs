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
        public string PlayerId { get; set; } = string.Empty;
        public string Nickname { get; set; } = "Capivara";
        public string AvatarId { get; set; } = "avatar1";
        public bool IsBot { get; set; }
        public bool IsLocal { get; set; }
        public int Placement { get; set; }
        public int Eliminations { get; set; }
        public int RoundsSurvived { get; set; }
    }

    public sealed class Snapshot
    {
        public IReadOnlyList<Entry> Entries { get; set; } = Array.Empty<Entry>();
        public int PreviousLevel { get; set; } = 1;
        public int PreviousExperience { get; set; }
        public int ExperienceToNext { get; set; } = 1500;
        public int PreviousArenaPoints { get; set; }
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
            PreviousArenaPoints = EconomyV2Service.Instance != null ? EconomyV2Service.Instance.Points : 0
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
            PreviousArenaPoints = EconomyV2Service.Instance != null ? EconomyV2Service.Instance.Points : 0
        };
    }

}
