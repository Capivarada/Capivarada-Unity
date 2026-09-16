using TMPro;
using UnityEngine;

public class RegisterPanelUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("Mensagem")]
    [SerializeField] private TMP_Text messageText;

    private void OnEnable()
    {
        ClearMessage();

        if (emailInput != null)
            emailInput.Select();

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginError += OnRegisterError;
        }
    }

    private void OnDisable()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginError -= OnRegisterError;
        }
    }

    // =====================================================
    // CRIAR CONTA
    // =====================================================

    public void Register()
    {
        ClearMessage();

        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

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

        if (password.Length < 6)
        {
            ShowMessage(
                "A senha deve ter pelo menos 6 caracteres."
            );

            return;
        }

        if (password != confirmPassword)
        {
            ShowMessage(
                "As senhas não coincidem."
            );

            return;
        }

        if (PlayFabAuthManager.Instance == null)
        {
            ShowMessage(
                "Sistema de autenticação indisponível."
            );

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

        if (PlayFabAuthManager.Instance == null)
        {
            ShowMessage(
                "Sistema de autenticação indisponível."
            );

            return;
        }

        PlayFabAuthManager.Instance.LoginAsGuest();
    }

    // =====================================================
    // MENSAGEM
    // =====================================================

    private void OnRegisterError(string message)
    {
        ShowMessage(message);
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.LogWarning(
            $"[RegisterPanelUI] {message}"
        );
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }
}