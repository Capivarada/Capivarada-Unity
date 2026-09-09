using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMatchResultUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private GameObject container;

    [Header("Result")]
    [SerializeField] private GameObject victoryCrown;
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text killsText;

    [Header("XP")]
    [SerializeField] private TMP_Text placementXPText;
    [SerializeField] private TMP_Text killsXPText;
    [SerializeField] private TMP_Text roundsXPText;
    [SerializeField] private TMP_Text totalXPText;

    [Header("Animated Progress")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text animatedXPText;

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

        MatchProcessResult result =
            PlayFabMatchManager.Instance.LastMatchResult;

        if (result != null)
        {
            Show(result);
        }
    }

    public void Show(MatchProcessResult result)
    {
        Cursor.lockState = CursorLockMode.None;
        if (result == null)
            return;

        if (container != null)
            container.SetActive(true);

        if (victoryCrown != null)
            victoryCrown.SetActive(result.Won);

        Set(
            placementText,
            FormatPlacement(result)
        );

        Set(
            killsText,
            result.KillsXP > 0
                ? result.KillsXP.ToString()
                : "0"
        );

        Set(
            placementXPText,
            $"Colocação: +{result.PlacementXP} XP"
        );

        Set(
            killsXPText,
            $"Eliminações: +{result.KillsXP} XP"
        );

        Set(
            roundsXPText,
            $"Rodadas Sobrevividas: +{result.RoundsXP} XP"
        );

        Set(
            totalXPText,
            $"Total: +{result.XPEarned} XP"
        );

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(AnimateXP(result));
    }

    private IEnumerator AnimateXP(
        MatchProcessResult result)
    {
        int xpPerLevel = 1500;

        int startTotalXP =
            result.TotalXP - result.XPEarned;

        int endTotalXP =
            result.TotalXP;

        int currentLevel =
            result.PreviousLevel;

        int startLevelXP =
            startTotalXP % xpPerLevel;

        int endLevelXP =
            endTotalXP % xpPerLevel;

        SetLevel(currentLevel);
        SetXP(startLevelXP);

        yield return null;

        int totalXPToAnimate =
            endTotalXP - startTotalXP;

        float elapsed = 0f;

        while (elapsed < xpAnimationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / xpAnimationDuration
                );

            int animatedTotalXP =
                startTotalXP +
                Mathf.RoundToInt(
                    totalXPToAnimate * t
                );

            int animatedLevel =
                Mathf.FloorToInt(
                    animatedTotalXP / xpPerLevel
                ) + 1;

            int animatedLevelXP =
                animatedTotalXP % xpPerLevel;

            if (animatedLevel != currentLevel)
            {
                currentLevel =
                    animatedLevel;

                SetLevel(currentLevel);

                SetXP(0);

                yield return new WaitForSeconds(
                    levelUpPause
                );
            }

            SetXP(animatedLevelXP);

            yield return null;
        }

        SetLevel(result.CurrentLevel);
        SetXP(endLevelXP);

        animationCoroutine = null;
    }

    private void SetLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text =
                $"Nível {level}";
        }
    }

    private void SetXP(int xp)
    {
        if (xpSlider != null)
        {
            xpSlider.minValue = 0;
            xpSlider.maxValue = 1500;
            xpSlider.value = xp;
        }

        if (animatedXPText != null)
        {
            animatedXPText.text =
                $"{xp} / 1500 XP";
        }
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
        MatchProcessResult result)
    {
        int placement = result.Placement;

        if (placement <= 0)
            return "-";

        if (placement == 1)
            return "1º";

        if (placement == 2)
            return "2º";

        if (placement == 3)
            return "3º";

        return $"{placement}º";
    }
    
    public void OnClickClose()
    {
        Hide();
    }
}