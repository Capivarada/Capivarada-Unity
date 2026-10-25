using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Central UI state controller for the match, spectator flow and result return.</summary>
public class MatchUI : MonoBehaviour
{
    [Header("Stage Containers")]
    [SerializeField] private GameObject titleContainer;
    [SerializeField] private GameObject powerContainer;
    [SerializeField] private GameObject roundContainer;
    [SerializeField] private Animator titleContainerAnimator;

    [Header("Stage Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject countdownContainer;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text eliminatedPlayersText;
    [SerializeField] private TMP_Text killsText;

    [Header("Power")]
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Image powerImage;
    [SerializeField] private RectTransform powerSpeedPanel;

    [Header("End Of Match")]
    [SerializeField] private SpectatorUI spectatorUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private PlayerCameraController cameraController;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField, Min(1f)] private float lobbyCountdownDuration = 5f;

    [Header("Managers")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private MatchManager matchManager;
    
    private PlayerController humanPlayer;
    private PlayerMatchData playerData;
    private PlayerDeath humanDeath;
    private GameObject humanPlayerObject;
    private readonly List<GameObject> spectatablePlayers = new();
    private int spectatedIndex;
    private int totalPlayers;
    private int eliminatedCount;
    private bool humanEliminated;
    private bool isSpectating;
    private bool resultShown;
    private bool returningToLobby;
    private string displayedTitle;
    private Coroutine returnCoroutine;
    private float matchStartedAt;
    private int reportedKills;
    private int reportedRounds;
    private bool titleAnimationPlayedWhileVisible;
    private Coroutine powerShakeCoroutine;
    private Vector2 powerSpeedPanelBasePosition;

    private void Awake()
    {
        if (roundManager == null) roundManager = FindFirstObjectByType<RoundManager>();
        if (matchManager == null) matchManager = FindFirstObjectByType<MatchManager>();
        if (cameraController == null) cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (spectatorUI != null)
        {
            spectatorUI.PreviousRequested += SpectatePrevious;
            spectatorUI.NextRequested += SpectateNext;
            spectatorUI.ReturnToLobbyRequested += ShowResultAndReturn;
        }
        if (countdownContainer == null && countdownText != null && countdownText.transform.parent != null)
            countdownContainer = countdownText.transform.parent.gameObject;
        if (powerSpeedPanel != null) powerSpeedPanelBasePosition = powerSpeedPanel.anchoredPosition;
        HideSpectatorUI();
    }

    private void Start()
    {
        matchStartedAt = Time.realtimeSinceStartup;
        PlayFabMatchManager.Instance?.BeginMatch();
        if (roundManager != null)
        {
            roundManager.OnStateChanged += HandleRoundStateChanged;
            roundManager.OnRoundChanged += UpdateRound;
            roundManager.OnTimerChanged += UpdateCountdown;
            roundManager.OnTitleChanged += ShowTitle;
        }
        if (matchManager != null)
        {
            matchManager.OnPlayerEliminated += HandlePlayerEliminated;
            matchManager.OnMatchFinished += HandleMatchFinished;
            totalPlayers = matchManager.AllPlayers.Count;
        }
        HideTitleContainer();
        SetActive(powerContainer, false);
        SetActive(roundContainer, false);
        UpdateEliminatedPlayers();
    }

    public void SetHumanPlayer(GameObject player)
    {
        UnsubscribeHuman();
        humanPlayerObject = player;
        humanPlayer = player != null ? player.GetComponent<PlayerController>() : null;
        humanDeath = player != null ? player.GetComponent<PlayerDeath>() : null;
        if (humanPlayer != null) humanPlayer.OnChargeChanged += HandleChargeChanged;
        if (humanDeath != null) humanDeath.OnPlayerDeath += HandleHumanDeath;
        if (humanPlayer != null) HandleChargeChanged(humanPlayer.CurrentCharge);
    }

    public void SetPlayer(GameObject player)
    {
        if (playerData != null) { playerData.OnKillsChanged -= HandleKillsChanged; playerData.OnRoundsSurvivedChanged -= HandleRoundsSurvivedChanged; }
        playerData = player != null ? player.GetComponent<PlayerMatchData>() : null;
        reportedKills = playerData != null ? playerData.Kills : 0;
        reportedRounds = playerData != null ? playerData.RoundsSurvived : 0;
        if (playerData != null) { playerData.OnKillsChanged += HandleKillsChanged; playerData.OnRoundsSurvivedChanged += HandleRoundsSurvivedChanged; }
        UpdateKills();
    }

    public void SetTotalPlayers(int total) { totalPlayers = Mathf.Max(0, total); eliminatedCount = 0; UpdateEliminatedPlayers(); }

    public void ShowStartingUI()
    {
        SetActive(titleContainer, true); SetActive(powerContainer, false); SetActive(roundContainer, false);
        HideSpectatorUI();
    }
    public void ShowChoosingUI()
    {
        SetActive(titleContainer, true); SetActive(roundContainer, true); SetActive(powerContainer, !humanEliminated);
    }
    public void ShowRevealingUI()
    {
        SetActive(titleContainer, true); SetActive(roundContainer, true); SetActive(powerContainer, false);
    }
    public void ShowSpectatorUI()
    {
        SetActive(powerContainer, false);
        if (spectatorUI != null && spectatablePlayers.Count > 0) spectatorUI.Show(GetPlayerName(spectatablePlayers[spectatedIndex]));
    }
    public void HideSpectatorUI() { isSpectating = false; if (spectatorUI != null) spectatorUI.Hide(); }
    public void HideGameUI() {  if (gameUI != null) gameUI.SetActive(false); }

    private void HandleRoundStateChanged(RoundState state)
    {
        if (resultShown) return;
        switch (state)
        {
            case RoundState.Countdown: ShowStartingUI(); break;
            case RoundState.Choosing: ShowChoosingUI(); break;
            case RoundState.Revealing:
            case RoundState.Moving: ShowRevealingUI(); break;
            case RoundState.NextRound: SetActive(titleContainer, true); SetActive(roundContainer, true); SetActive(powerContainer, false); break;
        }
    }

    private void HandleHumanDeath(PlayerDeath _)
    {
        humanEliminated = true;
        SetActive(powerContainer, false);

        // A morte pode acontecer antes de haver alguém para observar. O cursor
        // precisa ser liberado independentemente de o painel ser exibido.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshSpectatablePlayers();
        if (spectatablePlayers.Count > 0) BeginSpectating(0);
    }

    private void Update()
    {
        if (!isSpectating || spectatablePlayers.Count == 0)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if ((keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame) ||
            (mouse != null && mouse.backButton.wasPressedThisFrame))
            SpectatePrevious();

        if ((keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame) ||
            (mouse != null && mouse.forwardButton.wasPressedThisFrame))
            SpectateNext();
    }

    private void HandlePlayerEliminated(GameObject _)
    {
        eliminatedCount++;
        totalPlayers = matchManager != null ? matchManager.AllPlayers.Count : totalPlayers;
        UpdateEliminatedPlayers();
        if (isSpectating) RefreshSpectatablePlayers();
    }

    private void HandleMatchFinished(IReadOnlyList<MatchResult> _)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowResultAndReturn();
        HideSpectatorUI();
        HideGameUI();
    }

    private void RefreshSpectatablePlayers()
    {
        GameObject previous = isSpectating && spectatedIndex >= 0 && spectatedIndex < spectatablePlayers.Count ? spectatablePlayers[spectatedIndex] : null;
        spectatablePlayers.Clear();
        if (matchManager != null)
            foreach (GameObject player in matchManager.GetAlivePlayers()) if (player != null && player != humanPlayerObject && player.activeInHierarchy) spectatablePlayers.Add(player);
        if (spectatablePlayers.Count == 0) { HideSpectatorUI(); return; }
        spectatedIndex = previous != null ? Mathf.Max(0, spectatablePlayers.IndexOf(previous)) : 0;
        if (isSpectating) ApplySpectatedPlayer();
    }

    private void BeginSpectating(int index)
    {
        if (spectatablePlayers.Count == 0) return;
        isSpectating = true;
        spectatedIndex = Mathf.Clamp(index, 0, spectatablePlayers.Count - 1);
        ApplySpectatedPlayer();
        ShowSpectatorUI();
    }
    private void SpectateNext() { if (!isSpectating || spectatablePlayers.Count == 0) return; spectatedIndex = (spectatedIndex + 1) % spectatablePlayers.Count; ApplySpectatedPlayer(); }
    private void SpectatePrevious() { if (!isSpectating || spectatablePlayers.Count == 0) return; spectatedIndex = (spectatedIndex - 1 + spectatablePlayers.Count) % spectatablePlayers.Count; ApplySpectatedPlayer(); }
    private void ApplySpectatedPlayer()
    {
        GameObject player = spectatablePlayers[spectatedIndex];
        if (player == null || !player.activeInHierarchy) { RefreshSpectatablePlayers(); return; }
        if (cameraController != null) cameraController.SetTarget(player.transform);
        if (spectatorUI != null) spectatorUI.SetPlayerName(GetPlayerName(player));
    }

    private void ShowResultAndReturn()
    {
        if (returningToLobby)
            return;

        SetActive(powerContainer, false);
        HideTitleContainer();
        SetActive(roundContainer, false);

        MatchResult result =
            matchManager != null
                ? matchManager.GetResultForPlayer(humanPlayerObject)
                : new MatchResult(0, 0, 0);

        resultShown = true;
        returningToLobby = true;

        returnCoroutine =
            StartCoroutine(ReturnToLobbyCoroutine(result));

        HideSpectatorUI();
        HideGameUI();
    }

    private IEnumerator ReturnToLobbyCoroutine(
        MatchResult matchResult)
    {
        for (
            int seconds = Mathf.CeilToInt(lobbyCountdownDuration);
            seconds > 0;
            seconds--)
        {
            yield return new WaitForSeconds(1f);
        }

        if (PlayFabMatchManager.Instance == null)
        {
            Debug.LogError(
                "PlayFabMatchManager não encontrado."
            );

            SceneManager.LoadScene(lobbySceneName);
            yield break;
        }

        PlayFabMatchManager.Instance.OnMatchProcessed +=
            OnMatchProcessed;

        PlayFabMatchManager.Instance.OnMatchProcessError +=
            OnMatchProcessError;

        PlayFabMatchManager.Instance.CompleteMatch(
            matchResult.Placement,
            matchResult.Won,
            Mathf.FloorToInt(Time.realtimeSinceStartup - matchStartedAt)
        );
    }
    
    private void OnMatchProcessed(
        MatchProcessResult result)
    {
        UnsubscribePlayFabEvents();

        Debug.Log(
            "Partida salva no PlayFab. " +
            "Voltando ao Lobby."
        );

        SceneManager.LoadScene(lobbySceneName);
    }
    
    private void OnMatchProcessError(string error)
    {
        UnsubscribePlayFabEvents();

        Debug.LogError(
            "Não foi possível salvar a partida: " +
            error
        );

        // Aqui podemos decidir depois se:
        // 1. tenta novamente
        // 2. mostra erro
        // 3. permite voltar mesmo assim
    }
    
    private void UnsubscribePlayFabEvents()
    {
        if (PlayFabMatchManager.Instance == null)
            return;

        PlayFabMatchManager.Instance.OnMatchProcessed -=
            OnMatchProcessed;

        PlayFabMatchManager.Instance.OnMatchProcessError -=
            OnMatchProcessError;
    }

    private void ShowTitle(string title)
    {
        if (titleText != null) titleText.text = title;
        SetActive(titleContainer, true);
        if (titleAnimationPlayedWhileVisible) return;
        titleAnimationPlayedWhileVisible = true;
        displayedTitle = title;
        if (titleContainerAnimator != null) { titleContainerAnimator.Rebind(); titleContainerAnimator.Play(0, 0, 0f); }
       
    }

    private void UpdateCountdown(float time)
    {
        bool visible = time > 0f;
        if (countdownContainer != null) countdownContainer.SetActive(visible);
        else if (countdownText != null) countdownText.gameObject.SetActive(visible);
        if (visible && countdownText != null) countdownText.text = Mathf.CeilToInt(time).ToString();
    }
    private void UpdateRound(int round) { if (roundText != null) roundText.text = $"Rodada {round}"; }
    private void HandleChargeChanged(float charge)
    {
        // Keep the original filled power display intact. The speed streaks are
        // a separate full-screen panel supplied by the scene UI.
        if (powerText != null) powerText.text = $"{charge:0.#}/5";
        if (powerImage != null) powerImage.fillAmount = charge / 5f;
        if (powerSpeedPanel == null) return;
        // This panel intentionally only changes at whole-force thresholds.
        // Half steps remain visible in the filled PowerImage, but do not make
        // the speed-streak presentation jump between its five authored states.
        int speedTier = Mathf.FloorToInt(charge);
        bool visible = speedTier > 0;
        powerSpeedPanel.gameObject.SetActive(visible);
        if (!visible) { StopPowerShake(); return; }
        float scale = speedTier switch { 1 => 1.5f, 2 => 1.4f, 3 => 1.3f, 4 => 1.2f, _ => 1.1f };
        powerSpeedPanel.localScale = Vector3.one * scale;
        if (speedTier >= 5) StartPowerShake(); else StopPowerShake();
    }

    private void StartPowerShake()
    {
        if (powerShakeCoroutine == null) powerShakeCoroutine = StartCoroutine(ShakePowerImage());
    }
    private void StopPowerShake()
    {
        if (powerShakeCoroutine != null) StopCoroutine(powerShakeCoroutine);
        powerShakeCoroutine = null;
        if (powerSpeedPanel != null) powerSpeedPanel.anchoredPosition = powerSpeedPanelBasePosition;
    }
    private IEnumerator ShakePowerImage()
    {
        while (true)
        {
            if (powerSpeedPanel != null) powerSpeedPanel.anchoredPosition = powerSpeedPanelBasePosition + UnityEngine.Random.insideUnitCircle * 7f;
            yield return null;
        }
    }
    private void HideTitleContainer()
    {
        titleAnimationPlayedWhileVisible = false;
        SetActive(titleContainer, false);
    }
    private void UpdateKills() { if (killsText != null && playerData != null) killsText.text = playerData.Kills.ToString(); }
    private void HandleKillsChanged(int kills)
    {
        UpdateKills();
        for (int value = reportedKills; value < kills; value++) PlayFabMatchManager.Instance?.RecordKill();
        reportedKills = Mathf.Max(reportedKills, kills);
    }
    private void HandleRoundsSurvivedChanged(int rounds)
    {
        for (int value = reportedRounds; value < rounds; value++) PlayFabMatchManager.Instance?.RecordRoundSurvived();
        reportedRounds = Mathf.Max(reportedRounds, rounds);
    }
    private void UpdateEliminatedPlayers() { if (eliminatedPlayersText != null) eliminatedPlayersText.text = $"{eliminatedCount}/{totalPlayers}"; }
    private static string GetPlayerName(GameObject player) => player != null ? player.name : string.Empty;
    private static void SetActive(GameObject target, bool active) { if (target != null) target.SetActive(active); }
    private void UnsubscribeHuman() { if (humanPlayer != null) humanPlayer.OnChargeChanged -= HandleChargeChanged; if (humanDeath != null) humanDeath.OnPlayerDeath -= HandleHumanDeath; }
    private void OnDestroy()
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        StopPowerShake();
        if (roundManager != null) { roundManager.OnStateChanged -= HandleRoundStateChanged; roundManager.OnRoundChanged -= UpdateRound; roundManager.OnTimerChanged -= UpdateCountdown; roundManager.OnTitleChanged -= ShowTitle; }
        if (matchManager != null) { matchManager.OnPlayerEliminated -= HandlePlayerEliminated; matchManager.OnMatchFinished -= HandleMatchFinished; }
        if (spectatorUI != null) { spectatorUI.PreviousRequested -= SpectatePrevious; spectatorUI.NextRequested -= SpectateNext; spectatorUI.ReturnToLobbyRequested -= ShowResultAndReturn; }
        if (playerData != null) { playerData.OnKillsChanged -= HandleKillsChanged; playerData.OnRoundsSurvivedChanged -= HandleRoundsSurvivedChanged; }
        UnsubscribeHuman();
    }
}
