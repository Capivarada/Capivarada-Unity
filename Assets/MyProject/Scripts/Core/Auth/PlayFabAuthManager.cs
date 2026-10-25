using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
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
    
    private string registeredEmail;
    
    // Older releases persisted e-mail and password in PlayerPrefs. PlayerPrefs
    // is not encrypted, so credentials must never be kept there.
    private const string LegacySavedEmailKey = "Capivarada_SavedEmail";
    private const string LegacySavedPasswordKey = "Capivarada_SavedPassword";
    public event Action<float> OnAuthProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RemoveLegacyCredentials();
    }
    
    private void SetAuthProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        OnAuthProgress?.Invoke(progress);

        Debug.Log(
            $"[PlayFab] Progresso da autenticação: {progress * 100f:0}%"
        );
    }

    public bool HasSavedLogin()
    {
        // Device recognition is intentionally not implemented with an IP,
        // PlayerPrefs value or client-side CustomId. Those are copyable and
        // would grant account access without proving ownership.
        return false;
    }
    
    public void AutoLogin()
    {
        if (!HasSavedLogin())
        {
            OnLoginError?.Invoke(
                "Nenhum login salvo."
            );

            return;
        }

        OnLoginError?.Invoke("Entre com e-mail e senha para autorizar este dispositivo.");
    }
    
    // =====================================================
    // VISITANTE
    // =====================================================

    public void LoginAsGuest()
    {
        IsLoggedIn = false;
        IsGuest = true;
        IsOnline = false;

        PlayFabId = null;

        Nickname = GenerateGuestNickname();

        Debug.Log(
            $"[PlayFab] Entrando como visitante: {Nickname}"
        );

        OnLoginSuccess?.Invoke();
    }

    private string GenerateGuestNickname()
    {
        return $"Visitante {Random.Range(1000, 10000)}";
    }

    // =====================================================
    // CRIAR CONTA
    // =====================================================

    public void RegisterWithEmail(
        string email,
        string password
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            OnLoginError?.Invoke(
                "Digite seu e-mail."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            OnLoginError?.Invoke(
                "Digite sua senha."
            );

            return;
        }

        email = email.Trim();
        
        registeredEmail = email;

        if (password.Length < 6)
        {
            OnLoginError?.Invoke(
                "A senha deve ter pelo menos 6 caracteres."
            );

            return;
        }

        Debug.Log(
            $"[PlayFab] Criando conta: {email}"
        );

        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,

            // PlayFab exige Username nesse método.
            Username = GenerateUsername(),

            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            OnRegisterSuccess,
            OnPlayFabError
        );
    }

    private void OnRegisterSuccess(
        RegisterPlayFabUserResult result
    )
    {
        Debug.Log(
            "[PlayFab] Conta criada com sucesso!"
        );

        IsLoggedIn = true;
        IsGuest = false;
        IsOnline = true;

        PlayFabId = result.PlayFabId;

        CompleteProfileInfos();
    }
    
    private void AddContactEmail(string email)
    {
        var request = new AddOrUpdateContactEmailRequest
        {
            EmailAddress = email
        };

        PlayFabClientAPI.AddOrUpdateContactEmail(
            request,
            result =>
            {
                Debug.Log(
                    "[PlayFab] Contact Email configurado com sucesso."
                );

                OnLoginSuccess?.Invoke();
            },
            error =>
            {
                Debug.LogError(
                    "[PlayFab] Erro ao configurar Contact Email:\n" +
                    error.GenerateErrorReport()
                );

                OnLoginError?.Invoke(
                    "Não foi possível configurar o e-mail da conta."
                );
            }
        );
    }

    // =====================================================
    // ENTRAR COM E-MAIL
    // =====================================================

    public void LoginWithEmail(
        string email,
        string password
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            OnLoginError?.Invoke(
                "Digite seu e-mail."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            OnLoginError?.Invoke(
                "Digite sua senha."
            );

            return;
        }

        email = email.Trim();
        
        registeredEmail = email;
        
        SetAuthProgress(0.25f);

        Debug.Log(
            $"[PlayFab] Entrando com: {email}"
        );

        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };

        PlayFabClientAPI.LoginWithEmailAddress(
            request,
            OnLoginSuccessInternal,
            OnPlayFabError
        );
    }

    private void OnLoginSuccessInternal(
        LoginResult result
    )
    {
        IsLoggedIn = true;
        IsGuest = false;
        IsOnline = true;

        PlayFabId = result.PlayFabId;

        SetAuthProgress(0.60f);

        Debug.Log(
            $"[PlayFab] Login realizado: {PlayFabId}"
        );

        GetNickname();
    }
    
    private static void RemoveLegacyCredentials()
    {
        PlayerPrefs.DeleteKey(LegacySavedEmailKey);
        PlayerPrefs.DeleteKey(LegacySavedPasswordKey);
        PlayerPrefs.Save();
    }

    // =====================================================
    // USERNAME
    // =====================================================

    private string GenerateUsername()
    {
        return $"Capivara{Random.Range(100000, 1000000)}";
    }

    // =====================================================
    // NICKNAME
    // =====================================================

    private void CompleteProfileInfos()
    {
        string nickname =
            $"Capivara{Random.Range(1000, 10000)}";

        Debug.Log(
            $"[PlayFab] Gerando nickname inicial: {nickname}"
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
                    $"[PlayFab] Nickname criado: {Nickname}"
                );

                AddContactEmail(registeredEmail);
            },
            OnError
        );
    }

    public void GetNickname()
    {
        SetAuthProgress(0.80f);
        
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
                    $"[PlayFab] Nickname carregado: {Nickname}"
                );

                OnLoginSuccess?.Invoke();
            },
            OnError
        );
    }

    // =====================================================
    // ALTERAR NICKNAME
    // =====================================================

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

            // Apenas durante essa sessão.
            // Não salva no PlayerPrefs.

            Debug.Log(
                $"[PlayFab] Nickname visitante: {Nickname}"
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
                    $"[PlayFab] Nickname alterado: {Nickname}"
                );

                OnNicknameChanged?.Invoke(
                    Nickname
                );
            },
            OnError
        );
    }

    // =====================================================
    // ERROS
    // =====================================================

    private void OnPlayFabError(
        PlayFabError error
    )
    {
        string message =
            error.GenerateErrorReport();

        Debug.LogError(
            $"[PlayFab] {message}"
        );

        OnLoginError?.Invoke(
            error.ErrorMessage
        );
    }

    private void OnError(
        PlayFabError error
    )
    {
        string message =
            error.GenerateErrorReport();

        Debug.LogError(
            message
        );

        OnLoginError?.Invoke(
            message
        );
    }
}
