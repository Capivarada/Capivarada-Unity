using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthSceneController : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Start()
    {
        if (PlayFabAuthManager.Instance == null)
        {
            Debug.LogError(
                "[Auth] PlayFabAuthManager não encontrado!"
            );

            return;
        }

        PlayFabAuthManager.Instance.OnLoginSuccess += OnLoginSuccess;

        Debug.Log("[Auth] Inscrito no evento OnLoginSuccess.");
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
        }
    }

    private void OnLoginSuccess()
    {
        Debug.Log(
            "[Auth] OnLoginSuccess recebido! Indo para o Lobby."
        );

        SceneManager.LoadScene(lobbySceneName);
    }
}