using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Button target for NoConnection_Scene. Its visual layout remains scene-owned.</summary>
public sealed class NoConnectionController : MonoBehaviour
{
    [SerializeField] private string bootScene = "Boot_Scene";
    [SerializeField] private string loadingScene = "Loading_Scene";
    [SerializeField] private string lobbyScene = "Lobby_Scene";

    public void Retry() => SceneManager.LoadScene(bootScene);

    public void OpenOfflinePractice()
    {
        SessionAccessService session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        session?.SetOfflineGuestMode();
        SceneLoadRequest.Request(lobbyScene);
        SceneManager.LoadScene(loadingScene);
    }
}
