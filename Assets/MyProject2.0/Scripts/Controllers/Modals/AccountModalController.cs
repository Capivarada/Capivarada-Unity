using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AccountModalController : MonoBehaviour
{
    [SerializeField] private GameObject leaveGameModal, leaveAccountModal, deleteAccountModal; [SerializeField] private TMP_InputField deleteConfirmation; [SerializeField] private Button confirmDeleteButton; [SerializeField] private TMP_Text accountBadge; [SerializeField] private PlayerProfileService profileService; [SerializeField] private string authScene = "Auth_Scene";
    public void OpenLeaveGame() => Open(leaveGameModal); public void OpenLeaveAccount() => Open(leaveAccountModal); public void OpenDeleteAccount() => Open(deleteAccountModal);
    public void CloseAll() { Close(leaveGameModal); Close(leaveAccountModal); Close(deleteAccountModal); }
    public void UpdateDeleteAvailability() { if (confirmDeleteButton != null) confirmDeleteButton.interactable = deleteConfirmation != null && deleteConfirmation.text.Trim().ToUpperInvariant() == "EXCLUIR"; }
    public void LeaveAccount() { PlayFab.PlayFabClientAPI.ForgetAllCredentials(); SceneManager.LoadScene(authScene); }
    public void QuitGame() => Application.Quit();
    private void Open(GameObject modal) { CloseAll(); if (modal != null) modal.SetActive(true); if (accountBadge != null && profileService != null) accountBadge.text = $"{profileService.Current.nickname}\n{profileService.Current.email}"; }
    private static void Close(GameObject modal) { if (modal != null) modal.SetActive(false); }
}
