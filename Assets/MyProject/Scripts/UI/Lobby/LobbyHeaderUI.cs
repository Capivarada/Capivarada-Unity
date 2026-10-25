using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One header binder shared by lobby panels; bind it once in the Lobby scene.</summary>
public sealed class LobbyHeaderUI : MonoBehaviour
{
    [SerializeField] private LobbyPlayerProgress progress;
    [SerializeField] private PlayFabEconomyService economy;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private int xpPerLevel = 1500;
    [SerializeField] private List<Button> guestRestrictedButtons = new();

    private void OnEnable()
    {
        EnsureGuestRestrictedButtons();
        if (progress != null) progress.OnProgressLoaded += UpdateProgress;
        if (economy != null) economy.OnBalanceChanged += UpdatePoints;
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnNicknameChanged += UpdateNickname;
            PlayFabAuthManager.Instance.OnLoginSuccess += HandleLoginStateChanged;
        }
        if (economy != null && PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn) economy.Refresh(Debug.LogWarning);
        LoadProgress();
        UpdateGuestAccess();
        UpdateNickname(PlayFabAuthManager.Instance != null ? PlayFabAuthManager.Instance.Nickname : string.Empty);
    }
    private void OnDisable()
    {
        if (progress != null) progress.OnProgressLoaded -= UpdateProgress;
        if (economy != null) economy.OnBalanceChanged -= UpdatePoints;
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnNicknameChanged -= UpdateNickname;
            PlayFabAuthManager.Instance.OnLoginSuccess -= HandleLoginStateChanged;
        }
    }
    public void SetSectionTitle(string value) { if (titleText != null) titleText.text = value ?? string.Empty; }
    private void UpdateNickname(string value) { if (nicknameText != null) nicknameText.text = value ?? string.Empty; }
    private void UpdatePoints(int value) { if (pointsText != null) pointsText.text = value.ToString(); }

    private void UpdateProgress(int xp, int level)
    {
        if (levelText != null) levelText.text = $"Nível {level}";
        int levelXP = Mathf.Max(0, xp % xpPerLevel);
        if(xpText != null) xpText.text = $"{levelXP}/{xpPerLevel}";
        if (xpBar != null) { xpBar.maxValue = xpPerLevel; xpBar.value = levelXP; }
    }
    private void LoadProgress() { if (progress != null) progress.LoadProgress(); }
    public void ConfigureGuestRestrictedButtons(IEnumerable<Button> buttons)
    {
        guestRestrictedButtons = buttons == null ? new List<Button>() : new List<Button>(buttons);
        UpdateGuestAccess();
    }
    private void HandleLoginStateChanged()
    {
        LoadProgress();
        UpdateGuestAccess();
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsGuest) UpdatePoints(0);
    }
    private void UpdateGuestAccess()
    {
        if (PlayFabAuthManager.Instance == null) return;
        bool canUseRestrictedButtons = !PlayFabAuthManager.Instance.IsGuest;
        foreach (Button button in guestRestrictedButtons)
            if (button != null) button.interactable = canUseRestrictedButtons;
        if (PlayFabAuthManager.Instance.IsGuest) UpdatePoints(0);
    }

    private void EnsureGuestRestrictedButtons()
    {
        // This remains correct even when the scene was assembled without the
        // installer serializing the three button references.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || !button.gameObject.scene.IsValid()) continue;
            if (button.name != "Button.Shop" && button.name != "Button.Restroom" && button.name != "Button.Leaderboards") continue;
            if (!guestRestrictedButtons.Contains(button)) guestRestrictedButtons.Add(button);
        }
    }
}
