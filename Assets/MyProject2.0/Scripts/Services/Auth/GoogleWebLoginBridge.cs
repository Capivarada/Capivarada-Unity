using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>WebGL bridge for Google OAuth authorization codes.</summary>
public sealed class GoogleWebLoginBridge : MonoBehaviour
{
    public static GoogleWebLoginBridge Instance { get; private set; }
    [SerializeField] private string googleWebClientId;
    private bool createAccount;

    public static GoogleWebLoginBridge GetOrCreate(string clientId)
    {
        if (Instance == null)
        {
            GameObject root = new GameObject("Google Web Login Bridge");
            Instance = root.AddComponent<GoogleWebLoginBridge>();
        }
        Instance.googleWebClientId = clientId;
        return Instance;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CapivaradaGoogleAuthorize(string clientId, string targetObject);
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Authorize(bool shouldCreateAccount)
    {
        createAccount = shouldCreateAccount;
        if (string.IsNullOrWhiteSpace(googleWebClientId))
        {
            PlayFabAuthManager.Instance?.ReportExternalAuthError("Configure o Google Web Client ID no GoogleWebLoginBridge.");
            return;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        CapivaradaGoogleAuthorize(googleWebClientId, gameObject.name);
#else
        PlayFabAuthManager.Instance?.ReportExternalAuthError("Login Google está disponível somente na build WebGL configurada.");
#endif
    }

    // Called by Plugins/WebGL/CapivaradaGoogleAuth.jslib.
    public void OnGoogleAuthorizationCode(string code)
    {
        PlayFabAuthManager.Instance?.LoginWithGoogleAuthorizationCode(code, createAccount);
    }

    public void OnGoogleAuthorizationError(string error)
    {
        PlayFabAuthManager.Instance?.ReportExternalAuthError(string.IsNullOrWhiteSpace(error) ? "Não foi possível autenticar com Google." : error);
    }
}
