
using System;
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
    [SerializeField] private bool useDedicatedServers = true;
    [SerializeField, Min(10f)]
    private float allocationTimeoutSeconds = 120f;

    [Header("Party invitations")]
    [SerializeField] private PartyService partyService;
    [SerializeField] private GroupInviteModalController groupInviteModal;
    [SerializeField, Min(1f)]
    private float partyRefreshIntervalSeconds = 3f;

    [Header("Scenes")]
    [SerializeField] private string menuPanel = "Menu";
    [SerializeField] private string trainingScene = "Pratice_Scene";
    [SerializeField] private string loadingScene = "Loading_Scene";

    private bool joiningMatch;
    private float nextPartyRefreshAt;
    private string lastPresentedInviteId;

    private void Awake()
    {
        if (session == null)
            session = ServiceHub.Instance != null
                ? ServiceHub.Instance.Session
                : SessionAccessService.Instance;

        if (fusion == null)
            fusion = ServiceHub.Instance != null
                ? ServiceHub.Instance.Fusion
                : FusionSessionService.Instance;

        if (dedicatedMatch == null)
            dedicatedMatch = ServiceHub.Instance != null
                ? ServiceHub.Instance.GetComponent<FusionMatchJoinTokenService>()
                : FindFirstObjectByType<FusionMatchJoinTokenService>();

        if (partyService == null)
            partyService = ServiceHub.Instance != null
                ? ServiceHub.Instance.Party
                : PartyService.Instance;

        if (groupInviteModal == null)
            groupInviteModal =
                FindFirstObjectByType<GroupInviteModalController>(
                    FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (partyService == null) return;

        partyService.Changed += OnPartyChanged;
        partyService.Refresh();
    }

    private void OnDisable()
    {
        if (partyService != null)
            partyService.Changed -= OnPartyChanged;
    }

    private void Update()
    {
        if (partyService == null ||
            Time.unscaledTime < nextPartyRefreshAt)
            return;

        nextPartyRefreshAt =
            Time.unscaledTime +
            Mathf.Max(1f, partyRefreshIntervalSeconds);

        partyService.Refresh();
    }

    public void Open(string panelId)
    {
        panels?.Show(panelId);
    }

    public void BackToMenu()
    {
        panels?.Show(menuPanel);
    }

    public void Play()
    {
        if (joiningMatch) return;

        if (session == null || !session.IsOnline)
        {
            Debug.LogWarning(
                "[Lobby] Sessão online indisponível.", this);
            return;
        }

        StartCoroutine(JoinOnlineMatch());
    }

    public void Training()
    {
        MatchParticipationContext.StartPractice();

        string resolvedScene =
            Application.CanStreamedLevelBeLoaded(trainingScene)
                ? trainingScene
                : "Pratice_Scene";

        Load(resolvedScene);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void OnPartyChanged()
    {
        if (partyService == null ||
            partyService.Incoming.Count == 0)
            return;

        PartyService.Invitation invitation =
            partyService.Incoming[0];

        if (invitation == null ||
            string.IsNullOrWhiteSpace(invitation.inviteId) ||
            invitation.inviteId == lastPresentedInviteId)
            return;

        if (groupInviteModal == null)
            groupInviteModal =
                FindFirstObjectByType<GroupInviteModalController>(
                    FindObjectsInactive.Include);

        if (groupInviteModal == null)
        {
            Debug.LogWarning(
                "[Lobby] GroupInviteModalController não encontrado.");
            return;
        }

        lastPresentedInviteId = invitation.inviteId;
        groupInviteModal.PresentLatest();
    }

    private IEnumerator JoinOnlineMatch()
    {
        joiningMatch = true;

        try
        {
            if (fusion == null)
            {
                Debug.LogError(
                    "[Lobby] FusionSessionService não configurado.");
                yield break;
            }

            if (useDedicatedServers)
            {
                if (session == null ||
                    !session.IsAuthenticated ||
                    dedicatedMatch == null)
                {
                    Debug.LogError(
                        "[Lobby] Serviços necessários para MPS " +
                        "não estão disponíveis.");
                    yield break;
                }

                bool completed = false;
                bool succeeded = false;
                string error = string.Empty;

                Debug.Log(
                    "[Lobby] Solicitando servidor dedicado PlayFab.");

                try
                {
                    dedicatedMatch.AllocateAndRequest(
                        identity =>
                        {
                            StartCoroutine(
                                JoinDedicated(
                                    identity,
                                    value =>
                                    {
                                        succeeded = value;
                                        completed = true;
                                    }));
                        },
                        reason =>
                        {
                            error = reason;
                            completed = true;
                        },
                        "EastUs"
                    );
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                    completed = true;
                }

                float deadline =
                    Time.realtimeSinceStartup +
                    allocationTimeoutSeconds;

                while (!completed &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                if (!completed)
                {
                    Debug.LogError(
                        "[Lobby] Tempo limite de alocação/conexão " +
                        "excedido.");
                    // The server was already allocated by PlayFab. Explicitly
                    // cancel the client runner; the dedicated process has its
                    // own no-player timeout and will return capacity to MPS.
                    fusion.CancelDedicatedJoin();
                    yield break;
                }

                if (!succeeded)
                {
                    Debug.LogWarning(
                        "[Lobby] Falha no servidor dedicado: " +
                        (string.IsNullOrWhiteSpace(error)
                            ? "Falha na conexão Fusion."
                            : error));
                    yield break;
                }

                MatchParticipationContext.StartOnline(session);

                Debug.Log(
                    "[Lobby] Conexão dedicada estabelecida.");

                yield break;
            }

            Task<bool> join =
                fusion.JoinRandomMatchAndLoadAsync();

            while (!join.IsCompleted)
                yield return null;

            if (join.Status != TaskStatus.RanToCompletion ||
                !join.Result)
            {
                Debug.LogWarning(
                    "[Lobby] Falha ao encontrar partida Fusion.");
                yield break;
            }

            MatchParticipationContext.StartOnline(session);
        }
        finally
        {
            joiningMatch = false;
        }
    }

    private IEnumerator JoinDedicated(
        FusionDedicatedIdentity identity,
        Action<bool> completed)
    {
        Task<bool> join = null;

        try
        {
            join = fusion.JoinDedicatedMatchAsync(identity);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Lobby] Exceção ao conectar ao Fusion: " + ex);

            completed?.Invoke(false);
            yield break;
        }

        if (join == null)
        {
            Debug.LogError(
                "[Lobby] JoinDedicatedMatchAsync retornou null.");

            completed?.Invoke(false);
            yield break;
        }

        while (!join.IsCompleted)
            yield return null;

        if (join.IsFaulted)
        {
            Debug.LogError(
                "[Lobby] Erro Fusion: " + join.Exception);
        }
        else if (join.IsCanceled)
        {
            Debug.LogWarning(
                "[Lobby] Conexão Fusion cancelada.");
        }

        completed?.Invoke(
            join.Status == TaskStatus.RanToCompletion &&
            join.Result);
    }

    private void Load(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene))
            return;

        SceneLoadRequest.Request(scene);

        SceneManager.LoadScene(
            string.IsNullOrWhiteSpace(loadingScene)
                ? scene
                : loadingScene);
    }
}
