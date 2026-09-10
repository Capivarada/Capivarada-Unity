using System;
using System.Runtime.InteropServices;
using PlayFab;
using UnityEngine;

public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }

    [Header("Google OAuth")]
    [SerializeField]
    private string googleClientId =
        "544664617809-ugo3t9p7jtvtqv7dko20u10kef2mvim3.apps.googleusercontent.com";

    public bool IsAuthenticating { get; private set; }

    [Serializable]
    private class PlayFabLoginResult
    {
        public string PlayFabId;
        public bool NewlyCreated;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoginWithGoogle()
    {
    #if UNITY_WEBGL && !UNITY_EDITOR

        if (IsAuthenticating)
            return;

        IsAuthenticating = true;

        Debug.Log("[Google] Iniciando login WebGL...");


[DllImport("__Internal")]
static extern void GoogleLogin(
    string clientId,
    string titleId
);

#else
        Debug.LogWarning(
            "[Google] Este login está configurado para WebGL."
        );

#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern void GoogleLogin(
        string clientId,
        string titleId
    );

#endif

    public void OnPlayFabGoogleLoginSuccess(string json)
    {
        IsAuthenticating = false;

        Debug.Log("[PlayFab] Login Google realizado!");

        PlayFabLoginResult result =
            JsonUtility.FromJson<PlayFabLoginResult>(json);

        Debug.Log($"[PlayFab] ID: {result.PlayFabId}");
        Debug.Log($"[PlayFab] Nova conta: {result.NewlyCreated}");

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.CompleteGoogleLogin(
                result.PlayFabId,
                result.NewlyCreated
            );
        }
    }

    public void OnPlayFabGoogleLoginError(string error)
    {
        IsAuthenticating = false;

        Debug.LogError(
            "[PlayFab] Erro no login Google:"
        );

        Debug.LogError(error);
    }
}