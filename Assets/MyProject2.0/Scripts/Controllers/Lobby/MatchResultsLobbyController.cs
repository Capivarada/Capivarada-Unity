using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Lobby presentation for the most recently completed online match.</summary>
public sealed class MatchResultsLobbyController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private TMP_Text experienceEarnedText;
    [SerializeField] private TMP_Text arenaPointsEarnedText;
    [SerializeField] private TMP_Text placementBadgeText;
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private Slider experienceBar;
    [SerializeField] private Transform rowsContent;
    [SerializeField] private MatchResultsRowController rowPrefab;
    [SerializeField, Min(0.1f)] private float animationSeconds = 1.2f;

    private MatchResultsContext.Snapshot snapshot;
    private Coroutine animation;

    private void Awake()
    {
        if (profile == null) profile = ServiceHub.Instance != null
            ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
        if (root == null) root = gameObject;
    }

    private void OnEnable()
    {
        snapshot = MatchResultsContext.Consume();
        if (snapshot == null)
        {
            root.SetActive(false);
            return;
        }

        BuildRows();
        MatchResultsContext.Entry localEntry = FindLocalEntry();
        if (placementBadgeText != null)
            placementBadgeText.text = localEntry != null
                ? $"{localEntry.Placement}º Lugar"
                : string.Empty;
        if (experienceEarnedText != null) experienceEarnedText.text = $"+{snapshot.ExperienceEarned} XP";
        if (arenaPointsEarnedText != null) arenaPointsEarnedText.text = $"+{snapshot.ArenaPointsEarned}";
        ApplyProgress(snapshot.PreviousLevel, snapshot.PreviousExperience, snapshot.ExperienceToNext);
        if (profile != null)
        {
            profile.Changed += OnProfileChanged;
            profile.Load();
        }
        else StartAnimation(snapshot.PreviousLevel, snapshot.PreviousExperience, snapshot.ExperienceToNext, snapshot.ExperienceEarned);
    }

    private void OnDisable()
    {
        if (profile != null) profile.Changed -= OnProfileChanged;
        if (animation != null) StopCoroutine(animation);
        animation = null;
    }

    public void Close() => root.SetActive(false);

    private void OnProfileChanged(PlayerProfileData current)
    {
        if (snapshot == null || current == null) return;
        profile.Changed -= OnProfileChanged;
        int earned = Mathf.Max(0, ToTotalExperience(current.level, current.experience, current.experienceToNext) -
            ToTotalExperience(snapshot.PreviousLevel, snapshot.PreviousExperience, snapshot.ExperienceToNext));
        if (earned <= 0) earned = snapshot.ExperienceEarned;
        StartAnimation(snapshot.PreviousLevel, snapshot.PreviousExperience, snapshot.ExperienceToNext, earned);
    }

    private void StartAnimation(int level, int experience, int toNext, int earned)
    {
        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(AnimateProgress(level, experience, toNext, earned));
    }

    private IEnumerator AnimateProgress(int level, int experience, int toNext, int earned)
    {
        int startTotal = ToTotalExperience(level, experience, toNext);
        int endTotal = startTotal + Mathf.Max(0, earned);
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, animationSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(startTotal, endTotal, Mathf.Clamp01(elapsed / duration)));
            ApplyTotalExperience(value, toNext);
            yield return null;
        }
        ApplyTotalExperience(endTotal, toNext);
        animation = null;
    }

    private void ApplyProgress(int level, int experience, int toNext) =>
        ApplyTotalExperience(ToTotalExperience(level, experience, toNext), toNext);

    private void ApplyTotalExperience(int total, int toNext)
    {
        int safeToNext = Mathf.Max(1, toNext);
        int level = Mathf.Max(1, total / safeToNext + 1);
        int experience = total % safeToNext;
        if (currentLevelText != null) currentLevelText.text = $"Nível {level}";
        if (nextLevelText != null) nextLevelText.text = $"Nível {level + 1}";
        if (experienceText != null) experienceText.text = $"{experience}/{safeToNext} XP";
        if (experienceBar != null)
        {
            experienceBar.minValue = 0f;
            experienceBar.maxValue = safeToNext;
            experienceBar.value = experience;
        }
    }

    private void BuildRows()
    {
        if (rowsContent == null || rowPrefab == null) return;
        for (int index = rowsContent.childCount - 1; index >= 0; index--)
            Destroy(rowsContent.GetChild(index).gameObject);
        foreach (MatchResultsContext.Entry entry in snapshot.Entries)
            Instantiate(rowPrefab, rowsContent).Bind(entry);
    }

    private MatchResultsContext.Entry FindLocalEntry()
    {
        if (snapshot?.Entries == null) return null;
        foreach (MatchResultsContext.Entry entry in snapshot.Entries)
            if (entry != null && entry.IsLocal) return entry;
        return null;
    }

    private static int ToTotalExperience(int level, int experience, int toNext) =>
        Mathf.Max(0, level - 1) * Mathf.Max(1, toNext) + Mathf.Max(0, experience);
}
