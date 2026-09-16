using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class AccountRecoveryPanelUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField emailInput;

    [Header("Botão")]
    [SerializeField] private Button sendButton;
    [SerializeField] private TMP_Text sendButtonText;

    [Header("Mensagem")]
    [SerializeField] private TMP_Text messageText;

    [Header("PlayFab")]
    [SerializeField] private string titleId;
    [SerializeField] private string emailTemplateId;

    [Header("Cooldown")]
    [SerializeField] private int cooldownSeconds = 60;

    private string recoveryEmail;

    private bool recoveryCooldown;
    private Coroutine cooldownCoroutine;

    private void Awake()
    {
        UpdateButtonText();
    }

    /// <summary>
    /// Define o e-mail que será usado na recuperação.
    /// </summary>
    public void SetEmail(string email)
    {
        recoveryEmail = email?.Trim();

        if (emailInput != null)
            emailInput.text = recoveryEmail;

        ClearMessage();
    }

    /// <summary>
    /// Envia o e-mail de recuperação pelo PlayFab.
    /// </summary>
    public void SendRecoveryEmail()
    {
        if (recoveryCooldown)
        {
            ShowMessage(
                "Aguarde antes de solicitar outro e-mail."
            );

            return;
        }

        ClearMessage();

        string email =
            emailInput != null
                ? emailInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowMessage(
                "Digite seu e-mail."
            );

            return;
        }

        recoveryEmail = email;

        recoveryCooldown = true;

        if (sendButton != null)
            sendButton.interactable = false;

        var request =
            new SendAccountRecoveryEmailRequest
            {
                Email = recoveryEmail,
                TitleId = titleId,
                EmailTemplateId = emailTemplateId
            };

        Debug.Log(
            $"[Recovery] Enviando recuperação...\n" +
            $"Email: {recoveryEmail}\n" +
            $"TitleId: {titleId}\n" +
            $"Template: {emailTemplateId}"
        );

        PlayFabClientAPI.SendAccountRecoveryEmail(
            request,
            OnRecoverySuccess,
            OnRecoveryError
        );

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }

        cooldownCoroutine =
            StartCoroutine(
                CooldownCoroutine()
            );
    }

    private IEnumerator CooldownCoroutine()
    {
        for (
            int remaining = cooldownSeconds;
            remaining > 0;
            remaining--)
        {
            UpdateButtonText(remaining);

            yield return new WaitForSeconds(1f);
        }

        recoveryCooldown = false;

        if (sendButton != null)
            sendButton.interactable = true;

        UpdateButtonText();

        cooldownCoroutine = null;
    }

    private void UpdateButtonText(
        int remaining = 0)
    {
        if (sendButtonText == null)
            return;

        if (remaining > 0)
        {
            sendButtonText.text =
                $"REENVIAR E-MAIL ({remaining}s)";
        }
        else
        {
            sendButtonText.text =
                "ENVIAR E-MAIL";
        }
    }

    private void OnRecoverySuccess(
        SendAccountRecoveryEmailResult result)
    {
        Debug.Log(
            "[Recovery] E-mail de recuperação enviado."
        );

        ShowMessage(
            "E-mail enviado! " +
            "Verifique sua caixa de entrada."
        );
    }

    private void OnRecoveryError(
        PlayFabError error)
    {
        Debug.LogError(
            "[Recovery] ERRO DO PLAYFAB\n" +
            error.GenerateErrorReport()
        );

        ShowMessage(
            "Não foi possível enviar o e-mail.\n" +
            error.ErrorMessage
        );
    }

    public string GetRecoveryEmail()
    {
        return recoveryEmail;
    }

    private void ShowMessage(
        string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log(
            $"[RecoveryPanelUI] {message}"
        );
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }

    private void OnDisable()
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
    }
}