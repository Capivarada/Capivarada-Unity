using System;
using System.Collections;
using UnityEngine;

public enum RoundState
{
    Waiting,
    Countdown,
    Choosing,
    Revealing,
    Launching,
    Moving,
    Finished
}
public class RoundManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchManager matchManager;

    [Header("Timing")]
    [SerializeField] private float initialCountdownDuration = 5f;
    [SerializeField] private float choosingDuration = 10f;
    [SerializeField] private float stopVelocityThreshold = 0.15f;
    [SerializeField] private float stopCheckDelay = 0.75f;
    [Header("Reveal")]
    [SerializeField] private float revealDuration = 1.5f;
    [Header("Rounds")]
    [SerializeField] private int maxRounds = 10;

    private RoundState currentState =
        RoundState.Waiting;

    private int currentRound = 0;

    public RoundState CurrentState =>
        currentState;

    public int CurrentRound =>
        currentRound;
    
    public int MaxRounds => maxRounds;

    public float RemainingTime { get; private set; }

    public event Action<RoundState> OnStateChanged;
    public event Action<int> OnRoundChanged;
    public event Action<float> OnTimerChanged;

    private Coroutine roundCoroutine;

    private void Awake()
    {
        if (matchManager == null)
        {
            matchManager =
                GetComponent<MatchManager>();
        }
    }

    private void Start()
    {
        if (matchManager != null)
        {
            matchManager.OnStateChanged +=
                HandleMatchStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (matchManager != null)
        {
            matchManager.OnStateChanged -=
                HandleMatchStateChanged;
        }
    }
    
    private void RegisterRoundSurvival()
    {
        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (
                death != null &&
                death.IsDead()
            )
            {
                continue;
            }

            PlayerMatchData data =
                player.GetComponent<
                    PlayerMatchData
                >();

            if (data != null)
            {
                data.AddRoundSurvived();
            }
        }
    }

    private void HandleMatchStateChanged(
        MatchState state
    )
    {
        if (state == MatchState.Playing)
        {
            StartMatchRounds();
        }

        if (state == MatchState.Finished)
        {
            StopRounds();
        }
    }

    public void StartMatchRounds()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
        }

        roundCoroutine =
            StartCoroutine(RoundFlow());
    }

    public void StopRounds()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        SetState(RoundState.Finished);
    }

    private IEnumerator RoundFlow()
    {
        SetState(RoundState.Countdown);

        yield return StartCoroutine(
            Countdown(
                initialCountdownDuration,
                "Partida começa em"
            )
        );

        while (
            matchManager != null &&
            matchManager.AlivePlayers > 1 &&
            currentRound < maxRounds
        )
        {
            currentRound++;

            OnRoundChanged?.Invoke(
                currentRound
            );

            StartRound();

            yield return StartCoroutine(
                ChoosingPhase()
            );

            yield return StartCoroutine(
                RevealPhase()
            );

            LaunchPlayers();

            SetState(
                RoundState.Moving
            );

            yield return StartCoroutine(
                WaitForPlayersToStop()
            );
            
            RegisterRoundSurvival();
            
            if (
                matchManager != null &&
                matchManager.AlivePlayers > 1
            )
            {
                Debug.Log(
                    "[ROUND] Limite máximo de rounds atingido!"
                );

                matchManager.FinishWithRemainingPlayers();
            }
        }

        FinishRounds();
    }
    
    private void FinishRounds()
    {
        if (matchManager == null)
            return;

        Debug.Log(
            $"[ROUND] Rodadas encerradas. " +
            $"Rodada atual: {currentRound}/{maxRounds}"
        );

        SetState(
            RoundState.Finished
        );

        matchManager.FinishMatch();
    }
    
    private IEnumerator RevealPhase()
    {
        SetState(
            RoundState.Revealing
        );

        Debug.Log(
            "[ROUND] Revelando direções..."
        );

        ShowAllIndicators();

        yield return new WaitForSeconds(
            revealDuration
        );

        HideAllIndicators();
    }
    
    private void ShowAllIndicators()
    {
        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (
                death != null &&
                death.IsDead()
            )
            {
                continue;
            }

            PlayerController controller =
                player.GetComponent<PlayerController>();

            if (controller != null)
            {
                controller.ShowIndicator();
            }
        }
    }

    private void HideAllIndicators()
    {
        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerController controller =
                player.GetComponent<PlayerController>();

            if (controller != null)
            {
                controller.HideIndicator();
            }
        }
    }

    private void StartRound()
    {
        Debug.Log(
            $"[ROUND] Rodada {currentRound}"
        );

        ResetPlayers();

        HideAllIndicators();

        ShowHumanIndicator();

        SetState(
            RoundState.Choosing
        );
    }
    
    private void ShowHumanIndicator()
    {
        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            HumanPlayerController humanController =
                player.GetComponent<
                    HumanPlayerController
                >();

            if (humanController == null)
                continue;

            PlayerController controller =
                player.GetComponent<
                    PlayerController
                >();

            if (controller != null)
            {
                controller.SetIndicatorVisible(
                    true
                );
            }

            return;
        }
    }

    private IEnumerator ChoosingPhase()
    {
        yield return StartCoroutine(
            Countdown(
                choosingDuration,
                "Direções serão exibidas em"
            )
        );
    }

    private void LaunchPlayers()
    {
        SetState(
            RoundState.Launching
        );

        Debug.Log(
            "[ROUND] TODOS OS PLAYERS LANÇANDO!"
        );

        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (
                death != null &&
                death.IsDead()
            )
            {
                continue;
            }

            PlayerController controller =
                player.GetComponent<PlayerController>();

            if (controller == null)
                continue;

            controller.Launch();
        }
    }

    private void ResetPlayers()
    {
        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (
                death != null &&
                death.IsDead()
            )
            {
                continue;
            }

            PlayerController controller =
                player.GetComponent<PlayerController>();

            if (controller == null)
                continue;

            controller.ResetRound();
        }
    }

    private IEnumerator WaitForPlayersToStop()
    {
        yield return new WaitForSeconds(
            0.5f
        );

        while (true)
        {
            bool everyoneStopped = true;

            foreach (
                GameObject player
                in matchManager.AllPlayers
            )
            {
                if (
                    player == null ||
                    !player.activeInHierarchy
                )
                {
                    continue;
                }

                PlayerDeath death =
                    player.GetComponent<PlayerDeath>();

                if (
                    death != null &&
                    death.IsDead()
                )
                {
                    continue;
                }

                Rigidbody rb =
                    player.GetComponent<Rigidbody>();

                if (rb == null)
                    continue;

                Vector3 velocity =
                    rb.linearVelocity;

                velocity.y = 0f;

                if (
                    velocity.magnitude >
                    stopVelocityThreshold
                )
                {
                    everyoneStopped = false;
                    break;
                }
            }

            if (everyoneStopped)
            {
                Debug.Log(
                    "[ROUND] Todos pararam."
                );

                yield return new WaitForSeconds(
                    stopCheckDelay
                );

                break;
            }

            yield return null;
        }
    }

    private IEnumerator Countdown(
        float duration,
        string message
    )
    {
        float timer = duration;

        while (timer > 0f)
        {
            RemainingTime = timer;

            OnTimerChanged?.Invoke(
                timer
            );

            Debug.Log(
                $"{message} {Mathf.CeilToInt(timer)}..."
            );

            yield return new WaitForSeconds(
                1f
            );

            timer -= 1f;
        }

        RemainingTime = 0f;

        OnTimerChanged?.Invoke(0f);
    }

    private void SetState(
        RoundState newState
    )
    {
        if (currentState == newState)
            return;

        currentState = newState;

        OnStateChanged?.Invoke(
            currentState
        );

        Debug.Log(
            $"[ROUND] Estado: {currentState}"
        );
    }
}