using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Owns the lobby button while a party ticket is waiting or cancelled.</summary>
public sealed class MatchmakingQueueController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private FusionMatchmakingService matchmaking;
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private PartyService party;
    [Header("Button")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonText;
    [SerializeField] private string idleText = "JOGAR";
    [SerializeField] private string cancelText = "Cancelar pareamento?";
    [SerializeField, Min(0.25f)] private float pollIntervalSeconds = 0.75f;

    private Coroutine searchRoutine;
    private bool pointerOver;
    private bool connecting;

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

    public void Play()
    {
        if (connecting) return;
        if (IsSearching)
        {
            Cancel();
            return;
        }
        if (session == null || !session.IsOnline || matchmaking == null)
        {
            Debug.LogWarning("[Matchmaking] Serviços de sessão indisponíveis.", this);
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
            return;
        }
        ReportFailure("Não foi possível entrar na arena. Tente novamente.");
    }

    private void RefreshButton(int remainingSeconds)
    {
        if (playButtonText == null) return;
        if (connecting)
            playButtonText.text = "Entrando na arena...";
        else if (IsSearching)
            playButtonText.text = pointerOver ? cancelText : $"Procurando partida ({Mathf.Max(0, remainingSeconds)}s)";
        else
            playButtonText.text = idleText;
    }

    private void ReportFailure(string reason)
    {
        Debug.LogWarning("[Matchmaking] " + reason, this);
        connecting = false;
        RefreshButton(0);
    }
}
