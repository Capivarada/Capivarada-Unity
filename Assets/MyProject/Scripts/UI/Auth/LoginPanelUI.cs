using TMPro;
using UnityEngine;

public class LoginPanelUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Mensagem")]
    [SerializeField] private TMP_Text messageText;

    [Header("Recuperação")]
    [SerializeField] private AccountRecoveryPanelUI recoveryPanel;

    [Header("Navegação")]
    [SerializeField] private PanelManager panelManager;
    [SerializeField] private GameObject recoveryPanelObject;

    private void OnEnable()
    {
        ClearMessage();

        if (emailInput != null)
            emailInput.Select();

        if (PlayFabAuthManager.Instance != null)
            PlayFabAuthManager.Instance.OnLoginError += OnLoginError;
    }

    private void OnDisable()
    {
        if (PlayFabAuthManager.Instance != null)
            PlayFabAuthManager.Instance.OnLoginError -= OnLoginError;
    }

    public void Login()
    {
        ClearMessage();

        string email = emailInput.text.Trim();
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

        if (PlayFabAuthManager.Instance == null)
        {
            ShowMessage("Sistema de autenticação indisponível.");
            return;
        }

        PlayFabAuthManager.Instance.LoginWithEmail(email, password);
    }

    public void OpenRecovery()
    {
        ClearMessage();

        if (recoveryPanel == null)
        {
            Debug.LogWarning(
                "[LoginPanelUI] Recovery Panel não configurado."
            );
            return;
        }

        recoveryPanel.SetEmail(emailInput.text.Trim());

        if (panelManager != null && recoveryPanelObject != null)
        {
            panelManager.OpenPanel(recoveryPanelObject);
        }
    }

    private void OnLoginError(string message)
    {
        ShowMessage(message);
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.LogWarning($"[LoginPanelUI] {message}");
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }
}