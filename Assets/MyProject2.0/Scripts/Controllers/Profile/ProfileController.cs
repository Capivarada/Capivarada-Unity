using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProfileController : MonoBehaviour
{
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nicknameText, idText, levelText, experienceText, matchesText, eliminationsText, winsText, playTimeText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Transform historyContent;
    [SerializeField] private MatchHistoryCardController historyPrefab;
    [SerializeField] private EditProfileController editProfile; [SerializeField] private GameObject profileView;
    private readonly List<MatchHistoryCardController> history = new();
    private void Awake() { if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; }
    private void OnEnable() { if (profileService != null) { profileService.Changed += Bind; profileService.Load(); Bind(profileService.Current); } }
    private void OnDisable() { if (profileService != null) profileService.Changed -= Bind; }
    public void OpenEdit() { if (profileView != null) profileView.SetActive(false); editProfile?.Open(profileService != null ? profileService.Current : null, () => { if (profileView != null) profileView.SetActive(true); }); }
    public void ReturnFromEdit() { editProfile?.Close(); if (profileView != null) profileView.SetActive(true); }
    private void Bind(PlayerProfileData data)
    {
        if (data == null) return; int required = data.experienceToNext > 0 ? data.experienceToNext : 1500; int localXp = Mathf.Max(0, data.experience % required); if (nicknameText != null) nicknameText.text = data.nickname; if (idText != null) idText.text = $"ID #{data.playerId}"; if (levelText != null) levelText.text = $"Nível {data.level:00}"; if (experienceText != null) experienceText.text = $"{localXp}/{required} XP"; if (experienceSlider != null) experienceSlider.value = (float)localXp / required; if (matchesText != null) matchesText.text = data.matches.ToString(); if (eliminationsText != null) eliminationsText.text = data.eliminations.ToString(); if (winsText != null) winsText.text = data.wins.ToString(); if (playTimeText != null) playTimeText.text = AccountAge(data.createdAtUtc); AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(data.avatarId) ?? avatarCatalog.Default : null; if (avatar != null && avatarImage != null) avatarImage.sprite = avatar.Sprite; BuildHistory(data.recentMatches);
    }
    private static string AccountAge(string createdAtUtc)
    {
        if (!System.DateTime.TryParse(createdAtUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out System.DateTime created)) return "—";
        System.DateTime start = created.ToUniversalTime().Date;
        System.DateTime today = System.DateTime.UtcNow.Date;
        if (start > today) return "0 dias";
        int years = today.Year - start.Year;
        if (start.AddYears(years) > today) years--;
        System.DateTime cursor = start.AddYears(years);
        int months = (today.Year - cursor.Year) * 12 + today.Month - cursor.Month;
        if (cursor.AddMonths(months) > today) months--;
        cursor = cursor.AddMonths(months);
        int days = (today - cursor).Days;
        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} {(years == 1 ? "ano" : "anos")}");
        if (months > 0) parts.Add($"{months} {(months == 1 ? "mês" : "meses")}");
        if (days > 0 || parts.Count == 0) parts.Add($"{days} {(days == 1 ? "dia" : "dias")}");
        return string.Join(", ", parts);
    }
    private void BuildHistory(List<MatchHistoryData> matches) { foreach (MatchHistoryCardController card in history) if (card != null) Destroy(card.gameObject); history.Clear(); if (matches == null || historyContent == null || historyPrefab == null) return; for (int i = 0; i < Mathf.Min(3, matches.Count); i++) { MatchHistoryCardController card = Instantiate(historyPrefab, historyContent); card.Bind(matches[i]); history.Add(card); } }
}

public class EditProfileController : MonoBehaviour
{
    [SerializeField] private GameObject root; [SerializeField] private PlayerProfileService profileService; [SerializeField] private AvatarCatalog avatarCatalog; [SerializeField] private Transform avatarsContent; [SerializeField] private AvatarOptionController avatarPrefab; [SerializeField] private Image selectedAvatarImage; [SerializeField] private TMP_InputField nicknameInput;
    private readonly List<AvatarOptionController> options = new(); private string selectedAvatarId; private System.Action closed;
    private void Awake() { if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; }
    public void Open(PlayerProfileData current) => Open(current, null);
    public void Open(PlayerProfileData current, System.Action onClosed) { closed = onClosed; if (root != null) root.SetActive(true); selectedAvatarId = string.IsNullOrWhiteSpace(current?.avatarId) ? avatarCatalog?.Default?.Id : current.avatarId; if (nicknameInput != null) nicknameInput.text = current?.nickname; if (selectedAvatarImage != null) selectedAvatarImage.sprite = (avatarCatalog?.Find(selectedAvatarId) ?? avatarCatalog?.Default)?.Sprite; BuildOptions(); }
    public void Close() { if (root != null) root.SetActive(false); System.Action callback = closed; closed = null; callback?.Invoke(); }
    public void Save() { if (profileService == null) return; profileService.Save(nicknameInput != null ? nicknameInput.text : null, selectedAvatarId, null, Close); }
    public void ChangeAvatar() { if (selectedAvatarId == null && avatarCatalog != null) SelectAvatar(avatarCatalog.Default); }
    private void BuildOptions() { foreach (AvatarOptionController option in options) if (option != null) Destroy(option.gameObject); options.Clear(); if (avatarCatalog == null || avatarPrefab == null || avatarsContent == null) return; foreach (AvatarDefinition avatar in avatarCatalog.Avatars) { AvatarOptionController option = Instantiate(avatarPrefab, avatarsContent); option.Bind(avatar, SelectAvatar); options.Add(option); } }
    private void SelectAvatar(AvatarDefinition avatar) { if (avatar == null) return; selectedAvatarId = avatar.Id; if (selectedAvatarImage != null) selectedAvatarImage.sprite = avatar.Sprite; }
}

public class AvatarOptionController : MonoBehaviour
{
    [SerializeField] private Image image; [SerializeField] private Button button; [SerializeField] private TMP_Text nameText;
    public void Bind(AvatarDefinition avatar, System.Action<AvatarDefinition> selected) { if (image != null) image.sprite = avatar.Sprite; if (nameText != null) nameText.text = avatar.DisplayName; if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => selected?.Invoke(avatar)); } }
}

public class MatchHistoryCardController : MonoBehaviour
{
    [SerializeField] private TMP_Text placementText, eliminationsText, experienceText, dateText;
    public void Bind(MatchHistoryData data) { if (placementText != null) placementText.text = $"{data.placement}º lugar"; if (eliminationsText != null) eliminationsText.text = $"{data.eliminations} eliminações"; if (experienceText != null) experienceText.text = $"+{data.experience} XP"; if (dateText != null) dateText.text = data.playedAt; }
}

public class OtherProfileController : MonoBehaviour
{
    [SerializeField] private GameObject root; [SerializeField] private PanelNavigatorController panelNavigator; [SerializeField] private string panelId = "OtherProfile", returnPanelId = "Leaderboard"; [SerializeField] private PlayerProfileService profileService; [SerializeField] private FriendsService friendsService; [SerializeField] private AvatarCatalog avatarCatalog; [SerializeField] private CosmeticCatalog cosmeticCatalog; [SerializeField] private CapybaraAppearanceApplier capybaraPreview; [SerializeField] private UnityEngine.UI.Image avatarImage; [SerializeField] private TMP_Text nicknameText, idLevelText, statusText, matchesText, eliminationsText, winsText, playTimeText, socialFeedbackText; [SerializeField] private Button addFriendButton, inviteGroupButton;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private PartyService partyService;
    private string viewedPlayerId;
    private void Awake()
    {
        if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
        if (friendsService == null) friendsService = ServiceHub.Instance != null ? ServiceHub.Instance.Friends : FriendsService.Instance;
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (capybaraPreview == null && root != null) capybaraPreview = root.GetComponentInChildren<CapybaraAppearanceApplier>(true);
    }
    private void OnEnable() { if (friendsService != null) friendsService.Changed += RefreshSocialActions; }
    private void OnDisable() { if (friendsService != null) friendsService.Changed -= RefreshSocialActions; }
    public void Open(string playerId) { if (panelNavigator != null) panelNavigator.Show(panelId); else if (root != null) root.SetActive(true); profileService?.LoadOther(playerId, Bind); }
    public void Open(LeaderboardV2Service.PlayerEntry entry)
    {
        if (entry == null) return;
        if (panelNavigator != null) panelNavigator.Show(panelId); else if (root != null) root.SetActive(true);
        Bind(new PlayerProfileData { playerId = entry.playerId, nickname = entry.nickname, avatarId = entry.avatarId, level = entry.level, matches = entry.matches, eliminations = entry.eliminations, wins = entry.wins });
    }
    public void Close() { if (panelNavigator != null) panelNavigator.Show(returnPanelId); else if (root != null) root.SetActive(false); }
    public void SendFriendRequest()
    {
        if (string.IsNullOrWhiteSpace(viewedPlayerId) || friendsService == null) return;
        friendsService.Send(viewedPlayerId, () =>
        {
            if (socialFeedbackText != null) socialFeedbackText.text = "Solicitação enviada.";
            RefreshSocialActions();
        });
    }
    public void InviteToParty()
    {
        if (string.IsNullOrWhiteSpace(viewedPlayerId) || partyService == null) return;
        partyService.Invite(viewedPlayerId, () =>
        {
            if (socialFeedbackText != null) socialFeedbackText.text = "Convite enviado para o grupo.";
            RefreshSocialActions();
        });
    }
    private void Bind(PlayerProfileData data)
    {
        if (data == null) return;
        viewedPlayerId = data.playerId;
        if (nicknameText != null) nicknameText.text = data.nickname; if (idLevelText != null) idLevelText.text = $"ID #{data.playerId} · Nível {data.level:00}"; if (statusText != null) statusText.text = ""; if (matchesText != null) matchesText.text = data.matches.ToString(); if (eliminationsText != null) eliminationsText.text = data.eliminations.ToString(); if (winsText != null) winsText.text = data.wins.ToString(); if (playTimeText != null) playTimeText.text = System.TimeSpan.FromSeconds(data.playTimeSeconds).ToString(@"h\:mm"); AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(data.avatarId) ?? avatarCatalog.Default : null; if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite; capybaraPreview?.Apply(CapybaraGameplayCosmetics.ResolveLoadout(data.cosmetics), cosmeticCatalog);
        if (session == null || session.CanUseSocial) friendsService?.Refresh();
        RefreshSocialActions();
    }
    private void RefreshSocialActions()
    {
        if (session != null && !session.CanUseSocial)
        {
            if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
            if (inviteGroupButton != null) inviteGroupButton.gameObject.SetActive(false);
            return;
        }
        bool isSelf = profileService != null && profileService.Current != null && string.Equals(viewedPlayerId, profileService.Current.playerId, System.StringComparison.Ordinal);
        bool isFriend = !isSelf && friendsService != null && friendsService.IsFriend(viewedPlayerId);
        bool hasPendingRequest = !isFriend && friendsService != null && friendsService.HasPendingRequest(viewedPlayerId);
        if (addFriendButton != null) { addFriendButton.gameObject.SetActive(!isSelf && !isFriend); addFriendButton.interactable = !hasPendingRequest; }
        if (inviteGroupButton != null)
        {
            inviteGroupButton.gameObject.SetActive(isFriend);
            inviteGroupButton.interactable = isFriend && partyService != null && partyService.CanInvite && !partyService.Contains(viewedPlayerId) && !partyService.HasOutgoingInvite(viewedPlayerId);
        }
    }
}
