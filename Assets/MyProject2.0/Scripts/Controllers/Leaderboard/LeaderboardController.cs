using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LeaderboardController : MonoBehaviour
{
    [SerializeField] private LeaderboardService leaderboardService;
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private Transform rowsContent;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private LeaderboardRowController rowPrefab;
    [SerializeField] private LeaderboardRowController selfRow;
    [SerializeField] private LeaderboardTopPlayerController topPlayer;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private int pageSize = 10;
    private readonly List<LeaderboardRowController> rows = new();
    private LeaderboardMetric metric = LeaderboardMetric.Matches; private int nextStart; private bool loading;
    private void Awake() { if (leaderboardService == null) leaderboardService = ServiceHub.Instance != null ? ServiceHub.Instance.Leaderboard : LeaderboardService.Instance; if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; }
    private void OnEnable() { ResetAndLoad(); if (scrollRect != null) scrollRect.onValueChanged.AddListener(OnScroll); }
    private void OnDisable() { if (scrollRect != null) scrollRect.onValueChanged.RemoveListener(OnScroll); }
    public void ShowMatches() => SetMetric(LeaderboardMetric.Matches); public void ShowEliminations() => SetMetric(LeaderboardMetric.Eliminations); public void ShowWins() => SetMetric(LeaderboardMetric.Wins); public void ShowLevel() => SetMetric(LeaderboardMetric.Level);
    private void SetMetric(LeaderboardMetric value) { if (metric == value) return; metric = value; ResetAndLoad(); }
    private void ResetAndLoad() { foreach (LeaderboardRowController row in rows) if (row != null) Destroy(row.gameObject); rows.Clear(); nextStart = 0; LoadNext(); LoadSelf(); }
    private void LoadNext()
    {
        if (loading || leaderboardService == null) return; loading = true; leaderboardService.Load(metric, nextStart, pageSize, list => { loading = false; foreach (LeaderboardPlayerData entry in list) Add(entry); nextStart += list.Count; if (rows.Count == list.Count && list.Count > 0) topPlayer?.Bind(list[0], metric, OpenOtherProfile); });
    }
    private void LoadSelf() { if (leaderboardService == null || selfRow == null) return; leaderboardService.LoadAroundMe(metric, list => { if (list.Count > 0) selfRow.Bind(list[0], metric, null); }); }
    private void Add(LeaderboardPlayerData entry) { if (rowPrefab == null || rowsContent == null) return; LeaderboardRowController row = Instantiate(rowPrefab, rowsContent); row.Bind(entry, metric, OpenOtherProfile); rows.Add(row); }
    private void OnScroll(Vector2 position) { if (position.y <= .05f) LoadNext(); }
    private void OpenOtherProfile(string playerId) { if (profileService != null && profileService.Current.playerId == playerId) return; otherProfile?.Open(playerId); }
}

public class LeaderboardRowController : MonoBehaviour
{
    [SerializeField] private TMP_Text positionText, nicknameText, winsText, matchesText, eliminationsText, levelText; [SerializeField] private Image avatarImage; [SerializeField] private AvatarCatalog avatarCatalog; [SerializeField] private Button profileButton; [SerializeField] private Color selectedColor = Color.white, defaultColor = Color.white;
    public void Bind(LeaderboardPlayerData entry, LeaderboardMetric metric, System.Action<string> openProfile)
    {
        if (positionText != null) positionText.text = $"{entry.position}º"; if (nicknameText != null) nicknameText.text = entry.nickname; if (winsText != null) winsText.text = entry.wins.ToString(); if (matchesText != null) matchesText.text = entry.matches.ToString(); if (eliminationsText != null) eliminationsText.text = entry.eliminations.ToString(); if (levelText != null) levelText.text = entry.level.ToString();
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(entry.avatarId) ?? avatarCatalog.Default : null; if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite; SetColor(winsText, metric == LeaderboardMetric.Wins); SetColor(matchesText, metric == LeaderboardMetric.Matches); SetColor(eliminationsText, metric == LeaderboardMetric.Eliminations); SetColor(levelText, metric == LeaderboardMetric.Level);
        if (profileButton != null) { profileButton.onClick.RemoveAllListeners(); profileButton.onClick.AddListener(() => openProfile?.Invoke(entry.playerId)); }
    }
    private void SetColor(TMP_Text text, bool selected) { if (text != null) text.color = selected ? selectedColor : defaultColor; }
}

