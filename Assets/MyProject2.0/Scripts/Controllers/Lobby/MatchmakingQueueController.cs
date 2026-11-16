using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Owns the lobby button while a party ticket is waiting or cancelled.</summary>
public sealed class MatchmakingQueueController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private FusionMatchmakingService matchmaking;
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private PartyService party;
    [Header("Play buttons")]
    [Tooltip("Botão Jogar do menu de conta autenticada.")]
    [SerializeField] private Button authenticatedOnlinePlayButton;
    [SerializeField] private TMP_Text authenticatedOnlinePlayButtonText;
    [Tooltip("Botão Jogar da tela de grupo.")]
    [FormerlySerializedAs("playButton")]
    [SerializeField] private Button groupPlayButton;
    [FormerlySerializedAs("playButtonText")]
    [SerializeField] private TMP_Text groupPlayButtonText;
    [SerializeField] private string idleText = "JOGAR";
    [SerializeField] private string cancelText = "Cancelar pareamento?";
    [SerializeField, Min(0.25f)] private float pollIntervalSeconds = 0.75f;
    [SerializeField, Min(1f)] private float passiveObserveIntervalSeconds = 2f;

    private Coroutine searchRoutine;
    private Coroutine observeRoutine;
    private bool pointerOver;
    private bool connecting;
    private float nextPassiveObserveAt;
    private float suppressObserveUntil;

    public bool IsSearching => searchRoutine != null;

    private void Awake()
    {
        ServiceHub hub = ServiceHub.Instance;
        if (matchmaking == null) matchmaking = hub != null ? hub.GetComponent<FusionMatchmakingService>() : FindFirstObjectByType<FusionMatchmakingService>();
        if (fusion == null) fusion = hub != null ? hub.Fusion : FusionSessionService.Instance;
        if (session == null) session = hub != null ? hub.Session : SessionAccessService.Instance;
        if (party == null) party = hub != null ? hub.Party : PartyService.Instance;
    }

    private void OnDisable()
    {
        // Do not cancel a server-side ticket merely because a UI panel was hidden.
        // The persistent Services object continues polling only while this controller is active.
        pointerOver = false;
        RefreshButton(0);
    }

    private void Update()
    {
        // Non-leaders never create or cancel a ticket. They only discover the
        // leader's party ticket and receive their own signed connection proof.
        if (IsSearching || observeRoutine != null || connecting || matchmaking == null ||
            session == null || !session.IsAuthenticated ||
            party == null || !party.HasOtherMember || Time.unscaledTime < nextPassiveObserveAt ||
            Time.unscaledTime < suppressObserveUntil)
            return;

        nextPassiveObserveAt = Time.unscaledTime + passiveObserveIntervalSeconds;
        observeRoutine = StartCoroutine(ObserveExistingTicket());
    }

    /// <summary>Backward-compatible group button entry point.</summary>
    public void Play() => PlayFromGroup();

    public void PlayFromGuestOnline()
    {
        // Kept as a harmless Inspector-compatible entry point for any old button.
        // Visitors can browse the public ranking, but never create a ticket.
        ReportFailure("Partidas online são exclusivas para contas autenticadas.");
    }

    public void PlayFromAuthenticatedOnline() => StartMatchmaking();

    public void PlayFromGroup() => StartMatchmaking();

    private void StartMatchmaking()
    {
        if (connecting) return;
        if (IsSearching)
        {
            Cancel();
            return;
        }
        if (session == null || !session.IsAuthenticated || matchmaking == null)
        {
            ReportFailure("Partidas online são exclusivas para contas autenticadas.");
            return;
        }
        if (party == null || !party.CanStart)
        {
            Debug.LogWarning("[Matchmaking] O líder só inicia quando todos estiverem prontos.", this);
            return;
        }
        searchRoutine = StartCoroutine(Search());
    }

    public void Cancel()
    {
        if (!IsSearching || matchmaking == null) return;
        StopCoroutine(searchRoutine);
        searchRoutine = null;
        connecting = false;
        suppressObserveUntil = Time.unscaledTime + passiveObserveIntervalSeconds;
        matchmaking.Cancel(() => RefreshButton(0), ReportFailure);
        RefreshButton(0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        RefreshButton(0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        RefreshButton(0);
    }

    // Assign these simple methods to EventTrigger entries on each button when
    // this component lives on a persistent Lobby object rather than the button.
    public void OnPlayButtonPointerEnter() { pointerOver = true; RefreshButton(0); }
    public void OnPlayButtonPointerExit() { pointerOver = false; RefreshButton(0); }

    private IEnumerator Search()
    {
        bool received = false;
        FusionMatchmakingService.Status status = null;
        string error = null;
        matchmaking.StartSearch(value => { status = value; received = true; }, reason => { error = reason; received = true; });
        while (!received) yield return null;
        if (!string.IsNullOrWhiteSpace(error))
        {
            ReportFailure(error);
            searchRoutine = null;
            yield break;
        }

        yield return FollowTicket(status);
    }

    private IEnumerator ObserveExistingTicket()
    {
        bool received = false;
        FusionMatchmakingService.Status status = null;
        matchmaking.GetStatus(value => { status = value; received = true; }, reason =>
        {
            Debug.LogWarning("[Matchmaking] Consulta passiva falhou: " + reason, this);
            received = true;
        });
        while (!received) yield return null;
        observeRoutine = null;

        if (status == null || string.Equals(status.state, "idle", StringComparison.OrdinalIgnoreCase))
            yield break;

        if (searchRoutine == null)
            searchRoutine = StartCoroutine(FollowTicket(status));
    }

    private IEnumerator FollowTicket(FusionMatchmakingService.Status status)
    {
        bool received;
        string error;
        while (true)
        {
            if (status != null && matchmaking.TryCreateDedicatedIdentity(status, out FusionDedicatedIdentity identity))
            {
                connecting = true;
                RefreshButton(0);
                yield return JoinDedicated(identity);
                yield break;
            }

            RefreshButton(status != null ? status.remainingSeconds : 0);
            yield return new WaitForSecondsRealtime(pollIntervalSeconds);

            received = false;
            error = null;
            matchmaking.GetStatus(value => { status = value; received = true; }, reason => { error = reason; received = true; });
            while (!received) yield return null;
            if (!string.IsNullOrWhiteSpace(error))
            {
                ReportFailure(error);
                searchRoutine = null;
                yield break;
            }

            if (status != null && string.Equals(status.state, "idle", StringComparison.OrdinalIgnoreCase))
            {
                ReportFailure(string.IsNullOrWhiteSpace(status.error) ? "O pareamento foi encerrado." : status.error);
                searchRoutine = null;
                yield break;
            }
        }
    }

    private IEnumerator JoinDedicated(FusionDedicatedIdentity identity)
    {
        Task<bool> join = fusion != null ? fusion.JoinDedicatedMatchAsync(identity) : null;
        if (join == null)
        {
            ReportFailure("FusionSessionService não está configurado.");
            searchRoutine = null;
            connecting = false;
            yield break;
        }
        while (!join.IsCompleted) yield return null;
        connecting = false;
        searchRoutine = null;
        if (join.Status == TaskStatus.RanToCompletion && join.Result)
        {
            MatchParticipationContext.StartOnline(session);
            yield break;
        }
        ReportFailure("Não foi possível entrar na arena. Tente novamente.");
    }

    private void RefreshButton(int remainingSeconds)
    {
        string text;
        if (connecting)
            text = "Entrando na arena...";
        else if (IsSearching)
            text = pointerOver ? cancelText : $"Procurando partida ({Mathf.Max(0, remainingSeconds)}s)";
        else
            text = idleText;

        SetText(authenticatedOnlinePlayButtonText, text);
        SetText(groupPlayButtonText, text);
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null) label.text = value;
    }

    private void ReportFailure(string reason)
    {
        Debug.LogWarning("[Matchmaking] " + reason, this);
        connecting = false;
        RefreshButton(0);
    }
}
