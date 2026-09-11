using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMatchResultUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject lobbyContainer;

    [Header("Result")]
    [SerializeField] private GameObject victoryCrown;

    [Header("Level")]
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text nextLevelText;

    [Header("XP Progress")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TMP_Text animatedXPText;

    [Header("XP Summary")]
    [SerializeField] private TMP_Text totalAccumulatedXPText;

    [Header("Rounds")]
    [SerializeField] private TMP_Text roundsTitleText;
    [SerializeField] private TMP_Text roundsXPText;

    [Header("Placement")]
    [SerializeField] private TMP_Text placementTitleText;
    [SerializeField] private TMP_Text placementXPText;

    [Header("Eliminations")]
    [SerializeField] private TMP_Text eliminationsTitleText;
    [SerializeField] private TMP_Text eliminationsXPText;

    [Header("Victory Bonus")]
    [SerializeField] private TMP_Text victoryBonusTitleText;
    [SerializeField] private TMP_Text victoryBonusText;

    [Header("Coins")]
    [SerializeField] private TMP_Text totalCoinsText;

    [Header("Animation")]
    [SerializeField] private float xpAnimationDuration = 1.5f;
    [SerializeField] private float levelUpPause = 0.3f;

    private Coroutine animationCoroutine;


    private void Start()
    {
        Hide();

        if (PlayFabMatchManager.Instance == null)
        {
            Debug.LogWarning(
                "[LobbyMatchResultUI] " +
                "PlayFabMatchManager não encontrado."
            );

            return;
        }

        if (
            PlayFabAuthManager.Instance != null &&
            PlayFabAuthManager.Instance.IsGuest
        )
        {
            return;
        }

        MatchProcessResult result =
            PlayFabMatchManager.Instance.LastMatchResult;

        if (result != null)
        {
            Show(result);
        }
    }


    public void Show(MatchProcessResult result)
    {
        if (result == null)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (container != null)
            container.SetActive(true);
        if(lobbyContainer != null)
            lobbyContainer.SetActive(false);

        if (victoryCrown != null)
            victoryCrown.SetActive(result.Won);


        // ==========================================
        // RESULTADO
        // ==========================================

        Set(
            placementTitleText,
            $"Colocação ({FormatPlacement(result.Placement)} Lugar)"
        );

        Set(
            placementXPText,
            $"+{result.PlacementXP} XP"
        );


        // ==========================================
        // ELIMINAÇÕES
        // ==========================================

        Set(
            eliminationsTitleText,
            $"Eliminações ({result.Kills} Capivaras)"
        );

        Set(
            eliminationsXPText,
            $"+{result.KillsXP} XP"
        );


        // ==========================================
        // ROUNDS
        // ==========================================

        Set(
            roundsTitleText,
            $"Rounds Sobrevividos ({result.RoundsSurvived}/10)"
        );

        Set(
            roundsXPText,
            $"+{result.RoundsXP} XP"
        );


        // ==========================================
        // XP TOTAL
        // ==========================================

        Set(
            totalAccumulatedXPText,
            $"{result.TotalXP} XP"
        );


        // ==========================================
        // BÔNUS DE VITÓRIA
        // ==========================================

        Set(
            victoryBonusTitleText,
            "Bônus de Vitória"
        );

        Set(
            victoryBonusText,
            $"+{result.VictoryBonusCoins} Cascos"
        );


        // ==========================================
        // MOEDAS
        // ==========================================

        Set(
            totalCoinsText,
            $"{result.TotalCoinsEarned}"
        );


        // ==========================================
        // ANIMAÇÃO
        // ==========================================

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(AnimateXP(result));
    }


    private IEnumerator AnimateXP(MatchProcessResult result)
    {
        int xpRequired =
            result.XPRequired > 0
                ? result.XPRequired
                : 1500;

        int totalXP = result.TotalXP;

        int previousXP =
            Mathf.Max(0, result.PreviousXP);

        int finalXP =
            Mathf.Max(previousXP, result.TotalXP);

        int currentLevel =
            Mathf.Max(1, result.PreviousLevel);

        int finalLevel =
            Mathf.Max(currentLevel, result.CurrentLevel);


        // ==========================================
        // ESTADO INICIAL
        // ==========================================

        int startingLevelXP =
            previousXP % xpRequired;

        SetLevel(currentLevel);
        SetNextLevel(currentLevel + 1);

        SetXP(
            startingLevelXP,
            xpRequired,
            0
        );

        yield return null;


        // ==========================================
        // ANIMAÇÃO
        // ==========================================

        float elapsed = 0f;

        while (elapsed < xpAnimationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / xpAnimationDuration
                );

            int animatedXP =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        previousXP,
                        finalXP,
                        t
                    )
                );

            int calculatedLevel =
                Mathf.FloorToInt(
                    animatedXP / xpRequired
                ) + 1;

            calculatedLevel =
                Mathf.Clamp(
                    calculatedLevel,
                    1,
                    finalLevel
                );

            int levelXP =
                animatedXP % xpRequired;


            if (calculatedLevel != currentLevel)
            {
                currentLevel = calculatedLevel;

                SetLevel(currentLevel);
                SetNextLevel(currentLevel + 1);

                SetXP(
                    0,
                    xpRequired,
                    totalXP
                );

                yield return new WaitForSeconds(
                    levelUpPause
                );
            }

            SetXP(
                levelXP,
                xpRequired,
                totalXP
            );

            yield return null;
        }


        // ==========================================
        // ESTADO FINAL
        // ==========================================

        SetLevel(finalLevel);
        SetNextLevel(finalLevel + 1);

        SetXP(
            finalXP % xpRequired,
            xpRequired,
            totalXP
        );

        animationCoroutine = null;
    }


    private void SetLevel(int level)
    {
        Set(
            currentLevelText,
            $"Nível {level}"
        );
    }


    private void SetNextLevel(int level)
    {
        Set(
            nextLevelText,
            $"Nível {level}"
        );
    }


    private void SetXP(
        int currentXP,
        int requiredXP,
        int totalXP)
    {
        if (xpSlider != null)
        {
            xpSlider.minValue = 0;
            xpSlider.maxValue = requiredXP;
            xpSlider.value =
                Mathf.Clamp(
                    currentXP,
                    0,
                    requiredXP   
                );
        }

        Set(
            animatedXPText,
            $"{totalXP} XP OBTIDOS"
        );
    }


    public void Hide()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (container != null)
            container.SetActive(false);
if(lobbyContainer != null)
            lobbyContainer.SetActive(true);
        if (victoryCrown != null)
            victoryCrown.SetActive(false);
    }


    private static void Set(
        TMP_Text text,
        string value)
    {
        if (text != null)
            text.text = value;
    }


    private static string FormatPlacement(
        int placement)
    {
        if (placement <= 0)
            return "-";

        return $"{placement}º";
    }


    public void OnClickClose()
    {
        Hide();
    }
}