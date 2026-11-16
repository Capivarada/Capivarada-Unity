using System;
using UnityEngine;

public enum SessionAccessMode
{
    AuthenticatedOnline,
    GuestOnline,
    GuestOffline
}

/// <summary>
/// Owns the access policy for the current game session. Controllers use this
/// service to guard actions; lock visuals alone are never considered security.
/// </summary>
public sealed class SessionAccessService : MonoBehaviour
{
    public static SessionAccessService Instance { get; private set; }

    [SerializeField] private PlayFabAuthManager authentication;
    public SessionAccessMode Mode { get; private set; } = SessionAccessMode.GuestOffline;
    public bool IsAuthenticated => Mode == SessionAccessMode.AuthenticatedOnline;
    public bool IsGuest => !IsAuthenticated;
    public bool IsOnline => Mode != SessionAccessMode.GuestOffline;
    public bool CanPersist => IsAuthenticated;
    public bool CanUseSocial => IsAuthenticated;
    public bool CanUseEconomy => IsAuthenticated;
    // A aparência de visitante é mantida localmente, tanto com conexão quanto offline.
    public bool CanUsePersonalization => true;
    public bool CanUseSelfLeaderboardRow => IsAuthenticated;
    public event Action<SessionAccessMode> Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (authentication == null) authentication = GetComponent<PlayFabAuthManager>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (authentication != null)
        {
            authentication.OnLoginSuccess += RefreshFromAuthentication;
            authentication.OnLoginError += HandleAuthenticationError;
        }
        RefreshFromAuthentication();
    }

    private void OnDisable()
    {
        if (authentication != null)
        {
            authentication.OnLoginSuccess -= RefreshFromAuthentication;
            authentication.OnLoginError -= HandleAuthenticationError;
        }
    }

    public void SetOfflineGuestMode()
    {
        SetMode(SessionAccessMode.GuestOffline);
    }

    public bool RequireAuthenticated(Action<string> denied)
    {
        if (IsAuthenticated) return true;
        denied?.Invoke("Faça login para usar este recurso.");
        return false;
    }

    private void RefreshFromAuthentication()
    {
        if (authentication != null && authentication.IsLoggedIn && !authentication.IsGuest)
            SetMode(SessionAccessMode.AuthenticatedOnline);
        else if (authentication != null && authentication.IsGuest)
            SetMode(Application.internetReachability == NetworkReachability.NotReachable ? SessionAccessMode.GuestOffline : SessionAccessMode.GuestOnline);
        else
            SetMode(Application.internetReachability == NetworkReachability.NotReachable ? SessionAccessMode.GuestOffline : SessionAccessMode.GuestOnline);
    }

    private void HandleAuthenticationError(string _) => RefreshFromAuthentication();

    private void SetMode(SessionAccessMode value)
    {
        if (Mode == value) return;
        Mode = value;
        Debug.Log($"[Session] Access mode: {Mode}", this);
        Changed?.Invoke(Mode);
    }
}
