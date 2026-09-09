using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class LobbyPlayerProgress : MonoBehaviour
{
    public event Action<int, int> OnProgressLoaded;

    public int CurrentXP { get; private set; }
    public int Level { get; private set; }

    public void LoadProgress()
{
    if (PlayFabAuthManager.Instance == null)
    {
        Debug.LogWarning(
            "[LobbyPlayerProgress] AuthManager não encontrado."
        );

        return;
    }

    if (PlayFabAuthManager.Instance.IsGuest)
    {
        Debug.Log(
            "[LobbyPlayerProgress] Visitante: " +
            "sem progresso."
        );

        CurrentXP = 0;
        Level = 1;

        OnProgressLoaded?.Invoke(
            CurrentXP,
            Level
        );

        return;
    }

    if (!PlayFabAuthManager.Instance.IsLoggedIn)
    {
        Debug.LogWarning(
            "[LobbyPlayerProgress] Jogador não conectado."
        );

        return;
    }

    var request = new GetUserDataRequest
    {
        Keys = new List<string>
        {
            "CurrentXP",
            "Level"
        }
    };

    PlayFabClientAPI.GetUserReadOnlyData(
        request,
        OnProgressLoadedFromPlayFab,
        OnProgressError
    );
}
    private void OnProgressLoadedFromPlayFab(
        GetUserDataResult result)
    {
        CurrentXP = 0;
        Level = 1;

        if (result.Data != null)
        {
            if (result.Data.TryGetValue(
                    "CurrentXP",
                    out UserDataRecord xpData))
            {
                if (int.TryParse(
                        xpData.Value,
                        out int parsedXP))
                {
                    CurrentXP = parsedXP;
                }
            }

            if (result.Data.TryGetValue(
                    "Level",
                    out UserDataRecord levelData))
            {
                if (int.TryParse(
                        levelData.Value,
                        out int parsedLevel))
                {
                    Level = parsedLevel;
                }
            }
        }

        Debug.Log(
            $"[LobbyPlayerProgress] XP: {CurrentXP} | Level: {Level}"
        );

        OnProgressLoaded?.Invoke(
            CurrentXP,
            Level
        );
    }

    private void OnProgressError(
        PlayFabError error)
    {
        Debug.LogError(
            "[LobbyPlayerProgress] " +
            error.GenerateErrorReport()
        );
    }
}