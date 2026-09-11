
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthUI : MonoBehaviour
{

    [Header("Login")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;

    [Header("Navegação")]
    [SerializeField] private Button guestButton;

    [Header("Mensagens")]
    [SerializeField] private TMP_Text messageText;

    [Header("Cena")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Start()
    {
        messageText?.SetText("");
        emailInput?.Select();
        
        if (loginButton != null)
            loginButton.onClick.AddListener(Login);

        if (registerButton != null)
            registerButton.onClick.AddListener(Register);

        if (guestButton != null)
            guestButton.onClick.AddListener(LoginAsGuest);

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess += OnAuthenticationSuccess;
            PlayFabAuthManager.Instance.OnLoginError += OnAuthenticationError;
        }
    }

    private void OnDestroy()
    {


        if (loginButton != null)
            loginButton.onClick.RemoveListener(Login);

        if (registerButton != null)
            registerButton.onClick.RemoveListener(Register);

        if (guestButton != null)
            guestButton.onClick.RemoveListener(LoginAsGuest);

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess -= OnAuthenticationSuccess;
            PlayFabAuthManager.Instance.OnLoginError -= OnAuthenticationError;
        }
    }


    // =====================================================
    // ENTRAR
    // =====================================================

    private void Login()
    {
        ClearMessage();

        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowMessage("Digite seu e-mail.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowMessage("Digite sua senha.");
            return;
        }

        SetButtonsInteractable(false);

        PlayFabAuthManager.Instance.LoginWithEmail(
            email,
            password
        );
    }

    // =====================================================
    // CRIAR CONTA
    // =====================================================

    private void Register()
    {
        ClearMessage();

        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowMessage("Digite seu e-mail.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowMessage("Digite uma senha.");
            return;
        }

        SetButtonsInteractable(false);

        PlayFabAuthManager.Instance.RegisterWithEmail(
            email,
            password
        );
    }

    // =====================================================
    // VISITANTE
    // =====================================================

    private void LoginAsGuest()
    {
        ClearMessage();

        SetButtonsInteractable(false);

        PlayFabAuthManager.Instance.LoginAsGuest();
    }

    // =====================================================
    // RESULTADO
    // =====================================================

    private void OnAuthenticationSuccess()
    {
        Debug.Log(
            "[LoginUI] Autenticação concluída."
        );

        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnAuthenticationError(string error)
    {
        SetButtonsInteractable(true);

        ShowMessage(error);
    }

    // =====================================================
    // UI
    // =====================================================

    private void SetButtonsInteractable(bool interactable)
    {

        if (loginButton != null)
            loginButton.interactable = interactable;

        if (registerButton != null)
            registerButton.interactable = interactable;

        if (guestButton != null)
            guestButton.interactable = interactable;
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.LogWarning(
            $"[LoginUI] {message}"
        );
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }
}
