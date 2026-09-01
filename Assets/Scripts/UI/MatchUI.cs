using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchUI : MonoBehaviour
{
    [Header("Round")]
    [SerializeField]
    private TMP_Text roundText;

    [Header("Countdown")]
    [SerializeField]
    private TMP_Text countdownText;

    [Header("Power")]
    [SerializeField]
    private GameObject powerContainer;

    [SerializeField]
    private TMP_Text powerText;

    [SerializeField]
    private Image powerImage;

    [Header("Managers")]
    [SerializeField]
    private RoundManager roundManager;

    [SerializeField]
    private PlayerController humanPlayer;

    private void Start()
    {
        if (roundManager == null)
        {
            roundManager =
                FindObjectOfType<
                    RoundManager
                >();
        }

        if (roundManager != null)
        {
            roundManager.OnStateChanged +=
                HandleRoundStateChanged;

            roundManager.OnRoundChanged +=
                UpdateRound;

            roundManager.OnTimerChanged +=
                UpdateCountdown;
        }

        HidePower();
    }

    private void Update()
    {
        UpdatePower();
    }

    public void SetHumanPlayer(
        GameObject player
    )
    {
        if (player == null)
            return;

        humanPlayer =
            player.GetComponent<
                PlayerController
            >();
    }

    private void HandleRoundStateChanged(
        RoundState state
    )
    {
        switch (state)
        {
            case RoundState.Countdown:

                ShowCountdown();

                break;

            case RoundState.Choosing:

                ShowCountdown();

                ShowPower();

                break;
            
            case RoundState.Revealing:

                HideCountdown();

                HidePower();

                break;

            case RoundState.Launching:

                HideCountdown();

                HidePower();

                break;

            case RoundState.Moving:

                HideCountdown();

                HidePower();

                break;

            case RoundState.Finished:

                HideCountdown();

                HidePower();

                break;
        }
    }

    private void UpdateRound(
        int round
    )
    {
        if (roundText != null)
        {
            roundText.text =
                $"Rodada {round}";
        }
    }

    private void UpdateCountdown(
        float time
    )
    {
        if (countdownText == null)
            return;

        int seconds =
            Mathf.CeilToInt(time);

        if (
            roundManager.CurrentState ==
            RoundState.Countdown
        )
        {
            countdownText.text =
                $"Partida começa em {seconds}...";
        }

        if (
            roundManager.CurrentState ==
            RoundState.Choosing
        )
        {
            countdownText.text =
                $"Direções serão exibidas em {seconds}...";
        }
    }

    private void UpdatePower()
    {
        if (humanPlayer == null)
            return;

        if (
            roundManager == null ||
            roundManager.CurrentState !=
            RoundState.Choosing
        )
        {
            return;
        }

        int power =
            humanPlayer.CurrentCharge;

        if (powerText != null)
        {
            powerText.text =
                $"Poder {power}";
        }

        if (powerImage != null)
        {
            powerImage.fillAmount =
                power / 10f;
        }
    }

    private void ShowPower()
    {
        if (powerContainer != null)
        {
            powerContainer.SetActive(
                true
            );
        }
    }

    private void HidePower()
    {
        if (powerContainer != null)
        {
            powerContainer.SetActive(
                false
            );
        }
    }

    private void ShowCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject
                .SetActive(true);
        }
    }

    private void HideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject
                .SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnStateChanged -=
                HandleRoundStateChanged;

            roundManager.OnRoundChanged -=
                UpdateRound;

            roundManager.OnTimerChanged -=
                UpdateCountdown;
        }
    }
}