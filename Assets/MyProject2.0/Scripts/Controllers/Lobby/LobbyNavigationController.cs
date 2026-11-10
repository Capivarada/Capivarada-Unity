using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyNavigationController : MonoBehaviour
{
    [SerializeField] private PanelNavigatorController panels;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private FusionMatchJoinTokenService dedicatedMatch;
    [Header("Dedicated matches")]
    [Tooltip("Enable only after CAPIVARADA_MPS_BUILD_ID points to a deployed PlayFab MPS build.")]
    [SerializeField] private bool useDedicatedServers;
    [Header("Party invitations")]
    [SerializeField] private PartyService partyService;
    [SerializeField] private GroupInviteModalController groupInviteModal;
    [SerializeField, Min(1f)] private float partyRefreshIntervalSeconds = 3f;
    [SerializeField] private string menuPanel = "Menu";
    [SerializeField] private string gameScene = "Game_Scene";
    [SerializeField] private string trainingScene = "Pratice_Scene";
    [SerializeField] private string loadingScene = "Loading_Scene";
    private bool joiningMatch;
    private float nextPartyRefreshAt;
    private string lastPresentedInviteId;

    private void Awake()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (fusion == null) fusion = ServiceHub.Instance != null ? ServiceHub.Instance.Fusion : FusionSessionService.Instance;
        if (dedicatedMatch == null) dedicatedMatch = ServiceHub.Instance != null ? ServiceHub.Instance.GetComponent<FusionMatchJoinTokenService>() : FindFirstObjectByType<FusionMatchJoinTokenService>();
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
        if (groupInviteModal == null) groupInviteModal = UnityEngine.Object.FindFirstObjectByType<GroupInviteModalController>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (partyService == null) return;
        partyService.Changed += OnPartyChanged;
        partyService.Refresh();
    }

    private void OnDisable()
    {
        if (partyService != null) partyService.Changed -= OnPartyChanged;
    }

    private void Update()
    {
        if (partyService == null || Time.unscaledTime < nextPartyRefreshAt) return;
        nextPartyRefreshAt = Time.unscaledTime + Mathf.Max(1f, partyRefreshIntervalSeconds);
        partyService.Refresh();
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

    private void OnPartyChanged()
    {
        if (partyService == null || partyService.Incoming.Count == 0) return;
        PartyService.Invitation invitation = partyService.Incoming[0];
        if (invitation == null || string.IsNullOrWhiteSpace(invitation.inviteId) || invitation.inviteId == lastPresentedInviteId) return;

        if (groupInviteModal == null) groupInviteModal = UnityEngine.Object.FindFirstObjectByType<GroupInviteModalController>(FindObjectsInactive.Include);
        if (groupInviteModal == null)
        {
            Debug.LogWarning("[Lobby] GroupInviteModalController não foi encontrado na Lobby_Scene.", this);
            return;
        }

        lastPresentedInviteId = invitation.inviteId;
        groupInviteModal.PresentLatest();
    }

    private IEnumerator JoinOnlineMatch()
    {
        joiningMatch = true;
        if (fusion == null)
        {
            Debug.LogError("[Lobby] FusionSessionService não está configurado no Services.", this);
            joiningMatch = false;
            yield break;
        }

        if (useDedicatedServers && session != null && session.IsAuthenticated && dedicatedMatch != null)
        {
            bool completed = false;
            bool succeeded = false;
            string error = string.Empty;
            dedicatedMatch.AllocateAndRequest(identity => StartCoroutine(JoinDedicated(identity, value => { succeeded = value; completed = true; })), reason => { error = reason; completed = true; }, "BrazilSouth", "EastUs");
            while (!completed) yield return null;
            joiningMatch = false;
            if (!succeeded) Debug.LogWarning("[Lobby] Não foi possível entrar no servidor dedicado: " + error, this);
            else MatchParticipationContext.StartOnline(session);
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

    private IEnumerator JoinDedicated(FusionDedicatedIdentity identity, System.Action<bool> completed)
    {
        Task<bool> join = fusion.JoinDedicatedMatchAsync(identity);
        while (!join.IsCompleted) yield return null;
        completed?.Invoke(join.Status == TaskStatus.RanToCompletion && join.Result);
    }

    private void Load(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene)) return;
        SceneLoadRequest.Request(scene);
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(loadingScene) ? scene : loadingScene);
    }
}
