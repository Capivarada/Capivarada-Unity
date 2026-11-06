using TMPro;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AuthenticationController : MonoBehaviour
{
    private const string DefaultRecoveryTemplateId = "45EF793ABCC355CF";
    [Header("Services")] [SerializeField] private PlayFabAuthManager authentication;
    [Header("Panels")] [SerializeField] private PanelNavigatorController panels;
    [SerializeField] private string loginPanel = "Login";
    [SerializeField] private string registerPanel = "Register";
    [SerializeField] private string recoveryPanel = "Recovery";
    [Header("Login")] [SerializeField] private TMP_InputField loginEmail; [SerializeField] private TMP_InputField loginPassword;
    [Header("Registration")] [SerializeField] private TMP_InputField registerNickname; [SerializeField] private TMP_InputField registerEmail; [SerializeField] private TMP_InputField registerPassword; [SerializeField] private TMP_InputField registerConfirmation; [SerializeField] private Toggle acceptPrivacy;
    [Header("Recovery")] [SerializeField] private TMP_InputField recoveryEmail;
    [SerializeField] private string recoveryEmailTemplateId = "45EF793ABCC355CF";
    [Header("Feedback")] [SerializeField] private TMP_Text feedbackText; [SerializeField] private string lobbyScene = "Lobby_Scene";
    private void Awake() { if (authentication == null) authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : PlayFabAuthManager.Instance; }
    private void OnEnable() { if (authentication == null) return; authentication.OnLoginSuccess += OpenLobby; authentication.OnLoginError += ShowFeedback; }
    private void OnDisable() { if (authentication == null) return; authentication.OnLoginSuccess -= OpenLobby; authentication.OnLoginError -= ShowFeedback; }
    public void OpenLogin() => panels?.Show(loginPanel);
    public void OpenRegister() => panels?.Show(registerPanel);
    public void OpenRecovery() => panels?.Show(recoveryPanel);
    public void Login() { ClearFeedback(); authentication?.LoginWithEmail(loginEmail.text, loginPassword.text); }
    public void Register()
    {
        if (acceptPrivacy == null || !acceptPrivacy.isOn) { ShowFeedback("Aceite os termos de privacidade para continuar."); return; }
        if (registerPassword.text != registerConfirmation.text) { ShowFeedback("As senhas não coincidem."); return; }
        if (string.IsNullOrWhiteSpace(registerNickname.text)) { ShowFeedback("Digite um apelido."); return; }
        authentication?.RegisterWithEmail(registerEmail.text, registerPassword.text);
    }
    public void LoginAsGuest() { ClearFeedback(); authentication?.LoginAsGuest(); }
    public void SendRecoveryInstructions()
    {
        string email = recoveryEmail != null ? recoveryEmail.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(email)) { ShowFeedback("Digite o e-mail da conta."); return; }
        if (!IsEmail(email)) { ShowFeedback("Digite um e-mail válido."); return; }

        string titleId = PlayFab.PlayFabSettings.staticSettings?.TitleId?.Trim();
        if (string.IsNullOrWhiteSpace(titleId)) { ShowFeedback("Title ID do PlayFab não foi configurado."); return; }

        string templateId = string.IsNullOrWhiteSpace(recoveryEmailTemplateId)
            ? DefaultRecoveryTemplateId
            : recoveryEmailTemplateId.Trim();
        PlayFab.ClientModels.SendAccountRecoveryEmailRequest request = new()
        {
            Email = email,
            TitleId = titleId,
            EmailTemplateId = templateId
        };
        PlayFab.PlayFabClientAPI.SendAccountRecoveryEmail(request,
            _ => ShowFeedback("Instruções enviadas para o e-mail informado."),
            error =>
            {
                Debug.LogError("[Recuperação de senha] " + error.GenerateErrorReport(), this);
                ShowFeedback("Não foi possível enviar as instruções: " + error.ErrorMessage);
            });
    }
    public void TogglePasswordVisibility(TMP_InputField input) { if (input != null) input.contentType = input.contentType == TMP_InputField.ContentType.Password ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password; if (input != null) input.ForceLabelUpdate(); }
    private void OpenLobby() { if (!string.IsNullOrWhiteSpace(lobbyScene)) SceneManager.LoadScene(lobbyScene); }
    private void ClearFeedback() => ShowFeedback(string.Empty);
    private void ShowFeedback(string message) { if (feedbackText != null) feedbackText.text = message; }
    private static bool IsEmail(string value) => Regex.IsMatch(value, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
}
