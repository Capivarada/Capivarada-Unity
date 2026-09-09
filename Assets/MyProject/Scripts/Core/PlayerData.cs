using System;
using UnityEngine;

/// <summary>
/// In-memory player account data.
/// No local persistence (PlayerPrefs removed).
/// Prepared for future PlayFab integration.
/// </summary>
public class PlayerData
{
    #region Player Identity

    public string PlayerId { get; set; }
    public string Username { get; set; }
    public string PlayerName { get; set; }
    public bool IsGuest { get; set; }

    #endregion

    #region Progression

    public int Level { get; set; }
    public int CurrentXP { get; set; }
    public int TotalXP { get; set; }

    #endregion

    #region Statistics

    public int MatchesPlayed { get; set; }
    public int TotalKills { get; set; }
    public int TotalWins { get; set; }
    public int TotalRounds { get; set; }

    #endregion

    #region Constructor

    public PlayerData()
    {
        PlayerId = Guid.NewGuid().ToString();
        Username = "Guest";
        PlayerName = "Convidado";
        IsGuest = true;
        Level = 1;
        CurrentXP = 0;
        TotalXP = 0;
        MatchesPlayed = 0;
        TotalKills = 0;
        TotalWins = 0;
        TotalRounds = 0;
    }

    #endregion

    #region Progression Calculations

    /// <summary>
    /// Adds XP to current level. Handles level-up logic.
    /// Note: XP formula is now configurable via XPManager.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        CurrentXP += amount;
        TotalXP += amount;

        CheckLevelUp();
    }

    /// <summary>
    /// Check if player should level up based on current XP.
    /// Using the XP manager formula: 100 + ((Level - 1) * 50)
    /// </summary>
    private void CheckLevelUp()
    {
        int xpRequired = GetXPRequiredForNextLevel();

        while (CurrentXP >= xpRequired)
        {
            CurrentXP -= xpRequired;
            Level++;

            Debug.Log($"[PlayerData] Level Up! Now level {Level}");

            xpRequired = GetXPRequiredForNextLevel();
        }
    }

    /// <summary>
    /// Calculate XP required for next level.
    /// Formula: 100 + ((Level - 1) * 50)
    /// TODO: Make this configurable when adding XPConfig ScriptableObject
    /// </summary>
    public int GetXPRequiredForNextLevel()
    {
        return 100 + ((Level - 1) * 50);
    }

    #endregion

    #region Match Registration

    /// <summary>
    /// Register match completion. Updates all statistics.
    /// Called after match finishes with MatchResult data.
    /// No persistence - future PlayFab will handle this.
    /// </summary>
    public void RegisterMatch(int kills, int roundsSurvived, int placement, int xpEarned)
    {
        MatchesPlayed++;
        TotalKills += Mathf.Max(kills, 0);
        TotalRounds += Mathf.Max(roundsSurvived, 0);

        if (placement == 1)
        {
            TotalWins++;
        }

        AddXP(xpEarned);

        Debug.Log(
            $"[PlayerData] Match registered - Kills: {kills}, " +
            $"Rounds: {roundsSurvived}, Placement: {placement}, XP: {xpEarned}"
        );
    }

    #endregion

    #region Account Creation

    /// <summary>
    /// Create a local account with given username.
    /// Data exists only in memory during this session.
    /// </summary>
    public static PlayerData CreateLocalAccount(string username, string playerName)
    {
        var data = new PlayerData
        {
            PlayerId = Guid.NewGuid().ToString(),
            Username = username,
            PlayerName = playerName,
            IsGuest = false,
            Level = 1,
            CurrentXP = 0,
            TotalXP = 0,
            MatchesPlayed = 0,
            TotalKills = 0,
            TotalWins = 0,
            TotalRounds = 0
        };

        Debug.Log($"[PlayerData] Local account created: {username}");
        return data;
    }

    /// <summary>
    /// Create a guest account.
    /// Data exists only in memory during this session.
    /// </summary>
    public static PlayerData CreateGuestAccount()
    {
        var data = new PlayerData
        {
            PlayerId = Guid.NewGuid().ToString(),
            Username = "Guest",
            PlayerName = "Convidado",
            IsGuest = true,
            Level = 1,
            CurrentXP = 0,
            TotalXP = 0,
            MatchesPlayed = 0,
            TotalKills = 0,
            TotalWins = 0,
            TotalRounds = 0
        };

        Debug.Log($"[PlayerData] Guest account created: {data.PlayerId}");
        return data;
    }

    #endregion
}