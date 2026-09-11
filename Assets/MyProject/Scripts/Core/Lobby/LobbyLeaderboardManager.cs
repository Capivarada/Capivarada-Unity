using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class LobbyLeaderboardManager : MonoBehaviour
{
    [System.Serializable]
    public class LeaderboardUI
    {
        public Transform content;
    }

    [Header("Prefabs")]

    [SerializeField]
    private GameObject playerEntryPrefab;

    [Header("Leaderboards")]

    [SerializeField]
    private LeaderboardUI matchesLeaderboard;

    [SerializeField]
    private LeaderboardUI eliminationsLeaderboard;

    [SerializeField]
    private LeaderboardUI levelLeaderboard;

    [SerializeField]
    private LeaderboardUI winsLeaderboard;

    [Header("Config")]

    [SerializeField]
    private int maxResults = 10;

    #region UNITY

    private void Start()
    {
        if (PlayFabAuthManager.Instance == null)
        {
            Debug.LogError(
                "[Leaderboard] PlayFabAuthManager não encontrado."
            );

            return;
        }

        PlayFabAuthManager.Instance.OnLoginSuccess += OnLoginSuccess;

        // Caso já esteja logado.
        if (PlayFabAuthManager.Instance.IsLoggedIn)
        {
            OnLoginSuccess();
        }
    }
    
    private void OnLoginSuccess()
    {
        LoadAllLeaderboards();
    }
    
    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
        }
    }

    #endregion

    #region LOAD ALL

    public void LoadAllLeaderboards()
    {
        GetLeaderboard(
            "Matches",
            matchesLeaderboard
        );

        GetLeaderboard(
            "Eliminations",
            eliminationsLeaderboard
        );

        GetLeaderboard(
            "Level",
            levelLeaderboard
        );

        GetLeaderboard(
            "Wins",
            winsLeaderboard
        );
    }

    #endregion

    #region GET LEADERBOARD

    private void GetLeaderboard(
        string statisticName,
        LeaderboardUI leaderboardUI)
    {
        if (leaderboardUI == null ||
            leaderboardUI.content == null)
        {
            Debug.LogError(
                $"[Leaderboard] Content não configurado: {statisticName}"
            );

            return;
        }

        ClearLeaderboard(leaderboardUI.content);

        var request = new GetLeaderboardRequest
        {
            StatisticName = statisticName,
            StartPosition = 0,
            MaxResultsCount = maxResults
        };

        PlayFabClientAPI.GetLeaderboard(
            request,
            result =>
            {
                PopulateLeaderboard(
                    result.Leaderboard,
                    leaderboardUI.content
                );
            },
            error =>
            {
                Debug.LogError(
                    $"Erro no ranking {statisticName}: " +
                    error.GenerateErrorReport()
                );
            }
        );
    }

    #endregion

    #region POPULATE

    private void PopulateLeaderboard(
        List<PlayerLeaderboardEntry> players,
        Transform content)
    {
        foreach (PlayerLeaderboardEntry player in players)
        {
            GameObject entry =
                Instantiate(
                    playerEntryPrefab,
                    content
                );

            LeaderboardPlayerUI ui =
                entry.GetComponent<LeaderboardPlayerUI>();

            if (ui == null)
            {
                Debug.LogError(
                    "[Leaderboard] O prefab não possui " +
                    "LeaderboardPlayerUI."
                );

                continue;
            }

            string playerName =
                string.IsNullOrWhiteSpace(player.DisplayName)
                    ? "Jogador"
                    : player.DisplayName;

            ui.Setup(
                player.Position,
                playerName,
                player.StatValue
            );
        }
    }

    #endregion

    #region CLEAR

    private void ClearLeaderboard(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    #endregion
}