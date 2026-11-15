using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AccountModalController : MonoBehaviour
{
    [SerializeField] private GameObject leaveGameModal, leaveAccountModal, deleteAccountModal;
    [SerializeField] private TMP_InputField deleteConfirmation;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private TMP_Text accountBadge, deleteFeedback;
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private string authScene = "Auth_Scene";

    private void Awake()
    {
        if (deleteConfirmation == null) deleteConfirmation = GetComponentInChildren<TMP_InputField>(true);
        if (confirmDeleteButton == null)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text.IndexOf("excluir", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    confirmDeleteButton = button;
                    break;
                }
            }
        }
        if (backend == null) backend = ServiceHub.Instance != null ? ServiceHub.Instance.Backend : CapivaradaBackendService.Instance;
        if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
        if (deleteConfirmation != null) deleteConfirmation.onValueChanged.AddListener(_ => UpdateDeleteAvailability());
        if (confirmDeleteButton != null) confirmDeleteButton.onClick.AddListener(RequestAccountDeletion);
        UpdateDeleteAvailability();
    }
    public void OpenLeaveGame() => Open(leaveGameModal); public void OpenLeaveAccount() => Open(leaveAccountModal); public void OpenDeleteAccount() => Open(deleteAccountModal);
    public void CloseAll() { Close(leaveGameModal); Close(leaveAccountModal); Close(deleteAccountModal); }
    public void UpdateDeleteAvailability() { if (confirmDeleteButton != null) confirmDeleteButton.interactable = deleteConfirmation != null && deleteConfirmation.text.Trim().ToUpperInvariant() == "EXCLUIR"; }
    public void RequestAccountDeletion()
    {
        if (deleteConfirmation == null || !string.Equals(deleteConfirmation.text.Trim(), "EXCLUIR", System.StringComparison.OrdinalIgnoreCase)) { SetDeleteFeedback("Digite EXCLUIR para continuar."); return; }
        if (backend == null) { SetDeleteFeedback("O serviço de conta não está disponível."); return; }
        if (confirmDeleteButton != null) confirmDeleteButton.interactable = false;
        backend.Post<CapivaradaBackendService.EmptyResponse, CapivaradaBackendService.EmptyResponse>("account/deletion/request", new CapivaradaBackendService.EmptyResponse(), _ => { if (deleteConfirmation != null) deleteConfirmation.SetTextWithoutNotify(string.Empty); SetDeleteFeedback("Enviamos um e-mail. Confirme o link para excluir sua conta."); }, message => { SetDeleteFeedback("Não foi possível enviar o e-mail de confirmação. " + message); UpdateDeleteAvailability(); });
    }
    public void LeaveAccount() { PlayFab.PlayFabClientAPI.ForgetAllCredentials(); SceneManager.LoadScene(authScene); }
    public void QuitGame() => Application.Quit();
    private void SetDeleteFeedback(string message) { if (deleteFeedback != null) deleteFeedback.text = message; else Debug.LogWarning(message, this); }
    private void Open(GameObject modal) { CloseAll(); if (modal != null) modal.SetActive(true); if (accountBadge != null && profileService != null) accountBadge.text = $"{profileService.Current.nickname}\n{profileService.Current.email}"; }
    private static void Close(GameObject modal) { if (modal != null) modal.SetActive(false); }
}
