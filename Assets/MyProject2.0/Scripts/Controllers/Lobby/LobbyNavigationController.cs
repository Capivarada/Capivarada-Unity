using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyNavigationController : MonoBehaviour
{
    [SerializeField] private PanelNavigatorController panels;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private string menuPanel = "Menu";
    [SerializeField] private string gameScene = "Game_Scene";
    [SerializeField] private string trainingScene = "Pratice_Scene";
    [SerializeField] private string loadingScene = "Loading_Scene";
    private bool joiningMatch;

    private void Awake()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (fusion == null) fusion = ServiceHub.Instance != null ? ServiceHub.Instance.Fusion : FusionSessionService.Instance;
    }

    public void Open(string panelId) => panels?.Show(panelId);
    public void BackToMenu() => panels?.Show(menuPanel);

    public void Play()
    {
        if (session != null && !session.IsOnline)
        {
            Debug.LogWarning("[Lobby] Online match requested without an online session.", this);
            return;
        }
        if (joiningMatch) return;
        StartCoroutine(JoinOnlineMatch());
    }

    public void Training()
    {
        MatchParticipationContext.StartPractice();
        // The project originally saved "Training_Scene" in this serialized
        // field, while the actual scene is named "Pratice_Scene". Preserve
        // editable configuration but always fall back to the real build scene.
        string resolvedScene = Application.CanStreamedLevelBeLoaded(trainingScene)
            ? trainingScene
            : "Pratice_Scene";
        Load(resolvedScene);
    }

    public void Quit() => Application.Quit();

    private IEnumerator JoinOnlineMatch()
    {
        joiningMatch = true;
        if (fusion == null)
        {
            Debug.LogError("[Lobby] FusionSessionService não está configurado no Services.", this);
            joiningMatch = false;
            yield break;
        }

        Task<bool> join = fusion.JoinRandomMatchAndLoadAsync();
        while (!join.IsCompleted) yield return null;
        joiningMatch = false;
        if (join.Status != TaskStatus.RanToCompletion || !join.Result)
        {
            Debug.LogWarning("[Lobby] Não foi possível encontrar uma partida Fusion.", this);
            yield break;
        }

        MatchParticipationContext.StartOnline(session);
        // Fusion owns the online scene transition. Loading it locally here would
        // produce a disconnected copy of Game_Scene on this client.
    }

    private void Load(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene)) return;
        SceneLoadRequest.Request(scene);
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(loadingScene) ? scene : loadingScene);
    }
}
