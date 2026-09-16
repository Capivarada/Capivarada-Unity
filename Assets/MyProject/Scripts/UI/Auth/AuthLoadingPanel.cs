using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthLoadingPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private Slider loadingSlider;

    [Header("Cena")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private bool finished;

    private void Start()
    {
        ShowLoading();

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        if (PlayFabAuthManager.Instance == null)
        {
            Debug.LogError(
                "[AuthLoading] PlayFabAuthManager não encontrado."
            );

            ShowLogin();
            return;
        }

        PlayFabAuthManager.Instance.OnAuthProgress += OnAuthProgress;
        PlayFabAuthManager.Instance.OnLoginSuccess += OnLoginSuccess;
        PlayFabAuthManager.Instance.OnLoginError += OnLoginError;

        StartAutoLogin();
    }

    private void StartAutoLogin()
    {
        var auth =
            PlayFabAuthManager.Instance;

        SetProgress(0f);

        if (!auth.HasSavedLogin())
        {
            Debug.Log(
                "[AuthLoading] Nenhum login salvo."
            );

            SetProgress(1f);

            ShowLogin();

            return;
        }

        Debug.Log(
            "[AuthLoading] Login salvo encontrado. " +
            "Tentando login automático..."
        );

        SetProgress(0.15f);

        auth.AutoLogin();
    }

    private void OnAuthProgress(float progress)
    {
        SetProgress(progress);
    }

    private void SetProgress(float progress)
    {
        if (loadingSlider == null)
            return;

        loadingSlider.value =
            Mathf.Clamp01(progress);
    }

    private void OnLoginSuccess()
    {
        if (finished)
            return;

        finished = true;

        SetProgress(1f);

        Debug.Log(
            "[AuthLoading] Autenticação concluída."
        );

        ShowLoading();

        SceneManager.LoadScene(
            lobbySceneName
        );
    }

    private void OnLoginError(string message)
    {
        if (finished)
            return;

        Debug.LogWarning(
            $"[AuthLoading] Autenticação falhou: {message}"
        );

        SetProgress(1f);

        ShowLogin();
    }

    private void ShowLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (loginPanel != null)
            loginPanel.SetActive(false);
    }

    private void ShowLogin()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (loginPanel != null)
            loginPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance == null)
            return;

        PlayFabAuthManager.Instance.OnAuthProgress -= OnAuthProgress;
        PlayFabAuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
        PlayFabAuthManager.Instance.OnLoginError -= OnLoginError;
    }
}