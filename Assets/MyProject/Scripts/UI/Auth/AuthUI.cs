
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthUI : MonoBehaviour
{

    [Header("Login")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Mensagens")]
    [SerializeField] private TMP_Text messageText;

    [Header("Cena")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Start()
    {
        messageText?.SetText("");
        emailInput?.Select();

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess += OnAuthenticationSuccess;
            PlayFabAuthManager.Instance.OnLoginError += OnAuthenticationError;
        }
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess -= OnAuthenticationSuccess;
            PlayFabAuthManager.Instance.OnLoginError -= OnAuthenticationError;
        }
    }


    // =====================================================
    // ENTRAR
    // =====================================================

    public void Login()
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

        PlayFabAuthManager.Instance.LoginWithEmail(
            email,
            password
        );
    }

    // =====================================================
    // CRIAR CONTA
    // =====================================================

    public void Register()
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

        PlayFabAuthManager.Instance.RegisterWithEmail(
            email,
            password
        );
    }

    // =====================================================
    // VISITANTE
    // =====================================================

    public void LoginAsGuest()
    {
        ClearMessage();

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
        ShowMessage(error);
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
