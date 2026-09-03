using System;
using UnityEngine;

public static class PlayerData
{
    #region PLAYER

    public static string PlayerId;
    public static string Username;
    public static string PlayerName;

    public static bool IsGuest;

    #endregion


    #region PROGRESSION

    public static int Level;
    public static int CurrentXP;
    public static int TotalXP;

    #endregion


    #region STATISTICS

    public static int MatchesPlayed;
    public static int TotalKills;
    public static int TotalWins;
    public static int TotalRounds;

    #endregion


    #region PLAYER PREFS KEYS

    private const string PlayerIdKey =
        "PLAYER_ID";

    private const string UsernameKey =
        "PLAYER_USERNAME";

    private const string PlayerNameKey =
        "PLAYER_NAME";

    private const string IsGuestKey =
        "PLAYER_IS_GUEST";


    private const string LevelKey =
        "PLAYER_LEVEL";

    private const string CurrentXPKey =
        "PLAYER_CURRENT_XP";

    private const string TotalXPKey =
        "PLAYER_TOTAL_XP";


    private const string MatchesPlayedKey =
        "PLAYER_MATCHES_PLAYED";

    private const string TotalKillsKey =
        "PLAYER_TOTAL_KILLS";

    private const string TotalWinsKey =
        "PLAYER_TOTAL_WINS";

    private const string TotalRoundsKey =
        "PLAYER_TOTAL_ROUNDS";

    #endregion


    #region LOAD

    public static void Load()
    {
        PlayerId =
            PlayerPrefs.GetString(
                PlayerIdKey,
                null
            );

        Username =
            PlayerPrefs.GetString(
                UsernameKey,
                null
            );

        PlayerName =
            PlayerPrefs.GetString(
                PlayerNameKey,
                null
            );

        IsGuest =
            PlayerPrefs.GetInt(
                IsGuestKey,
                0
            ) == 1;


        Level =
            PlayerPrefs.GetInt(
                LevelKey,
                1
            );

        CurrentXP =
            PlayerPrefs.GetInt(
                CurrentXPKey,
                0
            );

        TotalXP =
            PlayerPrefs.GetInt(
                TotalXPKey,
                0
            );


        MatchesPlayed =
            PlayerPrefs.GetInt(
                MatchesPlayedKey,
                0
            );

        TotalKills =
            PlayerPrefs.GetInt(
                TotalKillsKey,
                0
            );

        TotalWins =
            PlayerPrefs.GetInt(
                TotalWinsKey,
                0
            );

        TotalRounds =
            PlayerPrefs.GetInt(
                TotalRoundsKey,
                0
            );

        Debug.Log(
            "[PlayerData] Dados carregados."
        );
    }

    #endregion


    #region SAVE

    public static void Save()
    {
        PlayerPrefs.SetString(
            PlayerIdKey,
            PlayerId ?? string.Empty
        );

        PlayerPrefs.SetString(
            UsernameKey,
            Username ?? string.Empty
        );

        PlayerPrefs.SetString(
            PlayerNameKey,
            PlayerName ?? string.Empty
        );

        PlayerPrefs.SetInt(
            IsGuestKey,
            IsGuest ? 1 : 0
        );


        PlayerPrefs.SetInt(
            LevelKey,
            Level
        );

        PlayerPrefs.SetInt(
            CurrentXPKey,
            CurrentXP
        );

        PlayerPrefs.SetInt(
            TotalXPKey,
            TotalXP
        );


        PlayerPrefs.SetInt(
            MatchesPlayedKey,
            MatchesPlayed
        );

        PlayerPrefs.SetInt(
            TotalKillsKey,
            TotalKills
        );

        PlayerPrefs.SetInt(
            TotalWinsKey,
            TotalWins
        );

        PlayerPrefs.SetInt(
            TotalRoundsKey,
            TotalRounds
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[PlayerData] Dados salvos."
        );
    }

    #endregion


    #region PROGRESSION

    public static void AddXP(
        int amount
    )
    {
        if (amount <= 0)
            return;

        CurrentXP += amount;
        TotalXP += amount;

        CheckLevelUp();

        Save();
    }


    private static void CheckLevelUp()
    {
        int xpRequired =
            GetXPRequiredForNextLevel();

        while (
            CurrentXP >=
            xpRequired
        )
        {
            CurrentXP -=
                xpRequired;

            Level++;

            Debug.Log(
                $"[PLAYER] Level Up! " +
                $"Agora é nível {Level}"
            );

            xpRequired =
                GetXPRequiredForNextLevel();
        }
    }


    public static int GetXPRequiredForNextLevel()
    {
        return
            100 +
            ((Level - 1) * 50);
    }

    #endregion


    #region STATISTICS

    public static void RegisterMatch(
        int kills,
        int roundsSurvived,
        int placement,
        int xpEarned
    )
    {
        MatchesPlayed++;

        TotalKills +=
            Mathf.Max(
                kills,
                0
            );

        TotalRounds +=
            Mathf.Max(
                roundsSurvived,
                0
            );


        if (placement == 1)
        {
            TotalWins++;
        }
        
        AddXP(
            xpEarned
        );

        Save();
    }

    #endregion


    #region ACCOUNT

    public static void CreateLocalAccount(
        string username,
        string playerName
    )
    {
        PlayerId =
            Guid.NewGuid()
                .ToString();

        Username =
            username;

        PlayerName =
            playerName;

        IsGuest =
            false;

        Level = 1;

        CurrentXP = 0;
        TotalXP = 0;

        MatchesPlayed = 0;
        TotalKills = 0;
        TotalWins = 0;
        TotalRounds = 0;

        Save();

        Debug.Log(
            $"[PlayerData] Conta criada: " +
            $"{Username}"
        );
    }


    public static void CreateGuestAccount()
    {
        PlayerId =
            Guid.NewGuid()
                .ToString();

        Username =
            "Guest";

        PlayerName =
            "Convidado";

        IsGuest =
            true;

        Level = 1;

        CurrentXP = 0;
        TotalXP = 0;

        MatchesPlayed = 0;
        TotalKills = 0;
        TotalWins = 0;
        TotalRounds = 0;
        
        Save();
    }

    #endregion


    #region CLEAR

    public static void Clear()
    {
        PlayerPrefs.DeleteAll();

        PlayerId = null;
        Username = null;
        PlayerName = null;

        IsGuest = false;

        Level = 1;

        CurrentXP = 0;
        TotalXP = 0;

        MatchesPlayed = 0;
        TotalKills = 0;
        TotalWins = 0;
        TotalRounds = 0;

        Debug.Log(
            "[PlayerData] Todos os dados foram apagados."
        );
    }

    #endregion
}