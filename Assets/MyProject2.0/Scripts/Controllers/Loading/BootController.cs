using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Entry point for the game. It keeps Services alive, verifies a usable
/// backend connection and only then sends the player to authentication.
/// </summary>
public sealed class BootController : MonoBehaviour
{
    [SerializeField] private ServiceHub services;
    [SerializeField] private BackendHealthService health;
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private VideoPlayer introVideo;
    [SerializeField] private string loadingScene = "Loading_Scene";
    [SerializeField] private string authenticationScene = "Auth_Scene";
    [SerializeField] private string noConnectionScene = "NoConnection_Scene";
    [SerializeField, Min(0f)] private float minimumIntroSeconds = 1f;
    [SerializeField] private bool startOnAwake = true;
    private bool starting;

    private void Awake()
    {
        if (services == null) services = ServiceHub.Instance;
        if (health == null) health = services != null ? services.Health : BackendHealthService.Instance;
        if (fusion == null) fusion = services != null ? services.Fusion : FusionSessionService.Instance;
    }

    private void Start()
    {
        if (startOnAwake) Begin();
    }

    public void Begin()
    {
        if (starting) return;
        StartCoroutine(Boot());
    }

    public void Retry() => Begin();

    public void StartOfflinePractice()
    {
        if (services != null && services.Session != null) services.Session.SetOfflineGuestMode();
        SceneLoadRequest.Request("Lobby_Scene");
        SceneManager.LoadScene(loadingScene);
    }

    private IEnumerator Boot()
    {
        starting = true;
        Debug.Log("[Boot] Starting services and connectivity validation.", this);
        float startedAt = Time.realtimeSinceStartup;
        if (introVideo != null) introVideo.Play();

        bool reachable = Application.internetReachability != NetworkReachability.NotReachable;
        bool backendReady = false;
        if (reachable && health != null)
        {
            bool finished = false;
            health.Check(success => { backendReady = success; finished = true; });
            while (!finished) yield return null;
        }

        while (Time.realtimeSinceStartup - startedAt < minimumIntroSeconds) yield return null;

        if (!reachable || !backendReady)
        {
            Debug.LogWarning("[Boot] No usable online connection. Opening offline choice.", this);
            SceneManager.LoadScene(noConnectionScene);
            starting = false;
            yield break;
        }

        if (fusion != null)
        {
            Task<bool> fusionTask = fusion.ConnectLobbyAsync();
            while (!fusionTask.IsCompleted) yield return null;
            bool fusionReady = fusionTask.Status == TaskStatus.RanToCompletion && fusionTask.Result;
            if (!fusionReady) Debug.LogWarning("[Boot] Fusion indisponível; login continua disponível e o botão Jogar exibirá o erro de conexão.", this);
        }

        Debug.Log("[Boot] Services ready. Opening authentication through Loading_Scene.", this);
        SceneLoadRequest.Request(authenticationScene);
        SceneManager.LoadScene(loadingScene);
    }
}
