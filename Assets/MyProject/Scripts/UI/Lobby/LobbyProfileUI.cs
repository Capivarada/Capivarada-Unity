using TMPro;
using UnityEngine;

public class LobbyProfileUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_Text errorText;

    [Header("Botão")]
    [SerializeField] private TMP_Text buttonText;

    private bool editing;

    #region UNITY

    private void Start()
    {
        if (PlayFabAuthManager.Instance == null)
        {
            Debug.LogError(
                "[LobbyProfileUI] PlayFabAuthManager não encontrado."
            );

            return;
        }

        PlayFabAuthManager.Instance.OnLoginSuccess += LoadProfile;
        PlayFabAuthManager.Instance.OnNicknameChanged += OnNicknameChanged;
        PlayFabAuthManager.Instance.OnLoginError += OnError;

        nicknameInput.interactable = false;

        if (PlayFabAuthManager.Instance.IsLoggedIn)
            LoadProfile();
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance == null)
            return;

        PlayFabAuthManager.Instance.OnLoginSuccess -= LoadProfile;
        PlayFabAuthManager.Instance.OnNicknameChanged -= OnNicknameChanged;
        PlayFabAuthManager.Instance.OnLoginError -= OnError;
    }

    #endregion

    #region PROFILE

    private void LoadProfile()
    {
        nicknameInput.text =
            PlayFabAuthManager.Instance.Nickname;

        nicknameInput.interactable = false;

        editing = false;

        UpdateButtonText();
    }

    #endregion

    #region BUTTON

    public void OnClickEditNickname()
    {
        if (!editing)
        {
            editing = true;

            nicknameInput.interactable = true;
            nicknameInput.Select();
            nicknameInput.ActivateInputField();

            UpdateButtonText();

            return;
        }

        SaveNickname();
    }

    private void SaveNickname()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            errorText.text = "Digite um nome válido.";
            return;
        }

        PlayFabAuthManager.Instance.ChangeNickname(nickname);
    }

    #endregion

    #region EVENTS

    private void OnNicknameChanged(string nickname)
    {
        nicknameInput.text = nickname;
        nicknameInput.interactable = false;

        editing = false;

        errorText.text = "";

        UpdateButtonText();
    }

    private void OnError(string error)
    {
        if (errorText != null)
            errorText.text = error;
    }

    #endregion

    #region UI

    private void UpdateButtonText()
    {
        if (buttonText == null)
            return;

        buttonText.text = editing
            ? "Salvar"
            : "Alterar";
    }

    #endregion
}