using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Random = UnityEngine.Random;

public class PlayFabAuthManager : MonoBehaviour
{
    public static PlayFabAuthManager Instance { get; private set; }

    public event Action OnLoginSuccess;
    public event Action<string> OnLoginError;
    public event Action<string> OnNicknameChanged;

    public bool IsLoggedIn { get; private set; }
    public bool IsGuest { get; private set; }
    public bool IsOnline { get; private set; }

    public string PlayFabId { get; private set; }
    public string Nickname { get; private set; }

    private const string DEVICE_ID = "PLAYFAB_DEVICE_ID";
    private const string GUEST_NICKNAME = "GUEST_NICKNAME";

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

    private void Start()
    {
    }

    // =====================================================
    // LOGIN
    // =====================================================
    public void CompleteGoogleLogin(
        string playFabId,
        bool newlyCreated
    )
    {
        IsLoggedIn = true;
        IsGuest = false;
        IsOnline = true;

        PlayFabId = playFabId;

        Debug.Log(
            $"[PlayFab] Google conectado: {PlayFabId}"
        );

        if (newlyCreated)
            GenerateInitialNickname();
        else
            GetNickname();
    }
    public void LoginAnonymously()
    {
        string deviceId = GetDeviceId();

        var request = new LoginWithCustomIDRequest
        {
            CustomId = deviceId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(
            request,
            OnLogin,
            OnPlayFabLoginError
        );
    }

    private void OnLogin(LoginResult result)
    {
        IsLoggedIn = true;
        IsGuest = false;
        IsOnline = true;

        PlayFabId = result.PlayFabId;

        Debug.Log(
            $"PlayFab Login OK: {PlayFabId}"
        );

        Debug.Log(
            $"Conta criada agora? {result.NewlyCreated}"
        );

        if (result.NewlyCreated)
        {
            GenerateInitialNickname();
        }
        else
        {
            GetNickname();
        }
    }

    // =====================================================
    // GUEST / OFFLINE
    // =====================================================

    private void EnterGuestMode()
    {
        IsLoggedIn = false;
        IsGuest = true;
        IsOnline = false;

        PlayFabId = null;

        Nickname = GetOrCreateGuestNickname();

        Debug.Log(
            $"Modo visitante offline: {Nickname}"
        );

        OnLoginSuccess?.Invoke();
    }

    private string GetOrCreateGuestNickname()
    {
        if (PlayerPrefs.HasKey(GUEST_NICKNAME))
        {
            return PlayerPrefs.GetString(GUEST_NICKNAME);
        }

        string nickname =
            $"Visitante{Random.Range(1000, 10000)}";

        PlayerPrefs.SetString(
            GUEST_NICKNAME,
            nickname
        );

        PlayerPrefs.Save();

        return nickname;
    }

    public void LoginAsGuest()
    {
        EnterGuestMode();
    }

    // =====================================================
    // DEVICE
    // =====================================================

    private string GetDeviceId()
    {
        return SystemInfo.deviceUniqueIdentifier;
    }

    // =====================================================
    // NICKNAME
    // =====================================================

    private void GenerateInitialNickname()
    {
        string nickname =
            $"Capivara{Random.Range(1000, 10000)}";

        Debug.Log(
            $"Gerando nickname inicial: {nickname}"
        );

        var request =
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            };

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            request,
            result =>
            {
                Nickname = result.DisplayName;

                Debug.Log(
                    $"Nickname inicial criado: {Nickname}"
                );

                OnLoginSuccess?.Invoke();
            },
            OnError
        );
    }

    public void GetNickname()
    {
        var request =
            new GetAccountInfoRequest();

        PlayFabClientAPI.GetAccountInfo(
            request,
            result =>
            {
                if (result.AccountInfo != null &&
                    result.AccountInfo.TitleInfo != null)
                {
                    Nickname =
                        result.AccountInfo.TitleInfo.DisplayName;
                }

                Debug.Log(
                    $"Nickname carregado: {Nickname}"
                );

                OnLoginSuccess?.Invoke();
            },
            OnError
        );
    }

    public void ChangeNickname(string newNickname)
    {
        if (string.IsNullOrWhiteSpace(newNickname))
        {
            OnLoginError?.Invoke(
                "Digite um nome válido."
            );

            return;
        }

        newNickname = newNickname.Trim();

        // =================================================
        // VISITANTE
        // =================================================

        if (IsGuest)
        {
            Nickname = newNickname;

            PlayerPrefs.SetString(
                GUEST_NICKNAME,
                Nickname
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"Nickname do visitante alterado: {Nickname}"
            );

            OnNicknameChanged?.Invoke(
                Nickname
            );

            return;
        }

        // =================================================
        // PLAYFAB
        // =================================================

        if (!IsLoggedIn)
        {
            OnLoginError?.Invoke(
                "Você ainda não está conectado."
            );

            return;
        }

        var request =
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = newNickname
            };

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            request,
            result =>
            {
                Nickname = result.DisplayName;

                Debug.Log(
                    $"Nickname alterado para: {Nickname}"
                );

                OnNicknameChanged?.Invoke(
                    Nickname
                );
            },
            OnError
        );
    }

    // =====================================================
    // ERRORS
    // =====================================================

    private void OnPlayFabLoginError(PlayFabError error)
    {
        Debug.LogWarning(
            "[PlayFab] Não foi possível conectar. " +
            "Entrando como visitante."
        );

        Debug.LogWarning(
            error.ErrorMessage
        );

        EnterGuestMode();
    }

    private void OnError(PlayFabError error)
    {
        string message =
            error.GenerateErrorReport();

        Debug.LogError(message);

        OnLoginError?.Invoke(message);
    }
}