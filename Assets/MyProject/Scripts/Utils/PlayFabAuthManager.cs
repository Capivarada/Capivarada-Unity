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

    public string PlayFabId { get; private set; }
    public string Nickname { get; private set; }

    private const string DEVICE_ID = "PLAYFAB_DEVICE_ID";

    #region UNITY

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
        LoginAnonymously();
    }

    #endregion

    #region LOGIN

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
            OnError
        );
    }

    private void OnLogin(LoginResult result)
    {
        IsLoggedIn = true;
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

    #endregion

    #region DEVICE

    private string GetDeviceId()
    {
        return SystemInfo.deviceUniqueIdentifier;
    }

    #endregion

    #region NICKNAME

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
        if (!IsLoggedIn)
        {
            OnLoginError?.Invoke(
                "Você ainda não está conectado."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(newNickname))
        {
            OnLoginError?.Invoke(
                "Digite um nome válido."
            );

            return;
        }

        newNickname = newNickname.Trim();

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

    #endregion

    #region ERRORS

    private void OnError(PlayFabError error)
    {
        string message =
            error.GenerateErrorReport();

        Debug.LogError(message);

        OnLoginError?.Invoke(message);
    }

    #endregion
}