using System;
using System.Collections;
using UnityEngine;

public enum RoundState { Waiting, Countdown, Choosing, Revealing, Moving, NextRound, Finished }

/// <summary>Owns the timed round flow. Match completion remains in MatchManager.</summary>
public class RoundManager : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private GameManager gameManager;
    private MatchConfig matchConfig;
    private Coroutine flow;
    private RoundState currentState = RoundState.Waiting;
    private int currentRound;
    public event Action<RoundState> OnStateChanged;
    public event Action<int> OnRoundChanged;
    public event Action<float> OnTimerChanged;
    public event Action<string> OnTitleChanged;
    public RoundState CurrentState => currentState;
    public int CurrentRound => currentRound;
    public float RemainingTime { get; private set; }

    private void Awake() { if (matchManager == null) matchManager = GetComponent<MatchManager>(); if (gameManager == null) gameManager = GetComponent<GameManager>(); matchConfig = gameManager.MatchConfig; }
    private void OnEnable() { if (matchManager != null) matchManager.OnStateChanged += HandleMatchState; }
    private void OnDisable() { if (matchManager != null) matchManager.OnStateChanged -= HandleMatchState; }
    private void HandleMatchState(MatchState state) { if (state == MatchState.Playing) StartMatchRounds(); else if (state == MatchState.Finished) StopRounds(); }
     public void StartMatchRounds() { if (matchConfig == null || matchManager == null) { Debug.LogError("[RoundManager] MatchConfig or MatchManager missing."); return; } if (flow != null) StopCoroutine(flow); currentRound = 0; flow = StartCoroutine(RoundFlow()); }
    public void StopRounds() { if (flow != null) StopCoroutine(flow); flow = null; SetState(RoundState.Finished); }

    private IEnumerator RoundFlow()
    {
        SetState(RoundState.Countdown); OnTitleChanged?.Invoke("A partida vai começar em..."); yield return Countdown(matchConfig.InitialCountdownDuration);
        while (matchManager.CurrentState == MatchState.Playing && matchManager.AlivePlayers > 1 && currentRound < matchConfig.MaxRounds)
        {
            currentRound++; OnRoundChanged?.Invoke(currentRound); ResetLivingPlayers(); HideAllIndicators(); ShowHumanIndicator();
            SetState(RoundState.Choosing); OnTitleChanged?.Invoke("Escolha a direção e a força"); yield return Countdown(matchConfig.ChoosingDuration);
            SetState(RoundState.Revealing);
            OnTitleChanged?.Invoke("Direções Reveladas!");
            HideTimer();
            ShowAllIndicators();
            yield return new WaitForSeconds(matchConfig.RevealDuration);
            HideAllIndicators();
            LaunchLivingPlayers();
            SetState(RoundState.Moving);
            yield return WaitForPlayersToStop();


// =====================================================
// TODOS OU QUASE TODOS MORRERAM
// =====================================================

            if (matchManager.AlivePlayers <= 1)
            {
                matchManager.FinishMatch(false);
                yield break;
            }


// =====================================================
// FECHA AS ELIMINAÇÕES DESTE ROUND
// =====================================================

            matchManager.FinishRound();

            RegisterRoundSurvival();


// =====================================================
// ÚLTIMO ROUND
// =====================================================

            if (currentRound >= matchConfig.MaxRounds)
            {
                matchManager.FinishMatch(true);
                yield break;
            }
            SetState(RoundState.NextRound); OnTitleChanged?.Invoke("Próxima rodada em..."); yield return Countdown(matchConfig.NextRoundCountdown);
        }
        if (matchManager.CurrentState == MatchState.Playing) matchManager.FinishMatch(currentRound >= matchConfig.MaxRounds && matchManager.AlivePlayers > 1);
    }

    private IEnumerator Countdown(float duration) { float remaining = duration; while (remaining > 0f) { RemainingTime = remaining; OnTimerChanged?.Invoke(remaining); yield return new WaitForSeconds(1f); remaining -= 1f; } HideTimer(); }
    private IEnumerator WaitForPlayersToStop()
    {
        yield return new WaitForSeconds(
            matchConfig.StopCheckDelay
        );

        while (matchManager.CurrentState == MatchState.Playing)
        {
            // Se sobrou 1 ou nenhum,
            // não precisamos esperar física.
            if (matchManager.AlivePlayers <= 1)
                yield break;


            bool stopped = true;

            foreach (GameObject player in matchManager.GetAlivePlayers())
            {
                Rigidbody body =
                    player.GetComponent<Rigidbody>();

                if (body == null)
                    continue;

                Vector3 velocity =
                    body.linearVelocity;

                velocity.y = 0f;

                if (velocity.magnitude >
                    matchConfig.StopVelocityThreshold)
                {
                    stopped = false;
                    break;
                }
            }

            if (stopped)
                yield break;

            yield return null;
        }
    }
    private void RegisterRoundSurvival() { foreach (GameObject player in matchManager.GetAlivePlayers()) player.GetComponent<PlayerMatchData>()?.AddRoundSurvived(); }
    private void ResetLivingPlayers() { foreach (GameObject p in matchManager.GetAlivePlayers()) p.GetComponent<PlayerController>()?.ResetRound(); }

    private void LaunchLivingPlayers()
    {
        foreach (GameObject p in matchManager.GetAlivePlayers())
        {
            p.GetComponent<PlayerController>()?.Launch();

            PlayerController animator = p.GetComponent<PlayerController>();
            //animator.animator.SetBool("Run", true);
        }
    }

    private void ShowAllIndicators() { foreach (GameObject p in matchManager.GetAlivePlayers()) p.GetComponent<PlayerController>()?.ShowIndicator(); }
    private void HideAllIndicators() { foreach (GameObject p in matchManager.AllPlayers) if (p != null) p.GetComponent<PlayerController>()?.HideIndicator(); }
    private void ShowHumanIndicator() { foreach (GameObject p in matchManager.GetAlivePlayers()) if (p.GetComponent<HumanPlayerController>() != null) { p.GetComponent<PlayerController>()?.ShowIndicator(); return; } }
    private void HideTimer() { RemainingTime = 0f; OnTimerChanged?.Invoke(0f); }
    private void SetState(RoundState state) { if (currentState == state) return; currentState = state; OnStateChanged?.Invoke(state); }
}
