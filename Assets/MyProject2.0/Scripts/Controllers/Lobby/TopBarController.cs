using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TopBarController : MonoBehaviour
{
    [SerializeField] private PlayerProfileService profileService;
    [SerializeField] private EconomyService economyService;
    [SerializeField] private EconomyV2Service inventoryService;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nicknameText, levelText, experienceText, pointsText;
    [SerializeField] private Slider experienceSlider;
    private void Awake() { if (profileService == null) profileService = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; if (economyService == null) economyService = ServiceHub.Instance != null ? ServiceHub.Instance.Economy : EconomyService.Instance; if (inventoryService == null) inventoryService = ServiceHub.Instance != null ? ServiceHub.Instance.EconomyV2 : EconomyV2Service.Instance; }
    private void OnEnable()
    {
        if (profileService != null) { profileService.Changed += RefreshProfile; profileService.Load(); RefreshProfile(profileService.Current); }
        if (inventoryService != null) { inventoryService.InventoryChanged += RefreshV2Inventory; inventoryService.RefreshInventory(); RefreshPoints(inventoryService.Points); }
        else if (economyService != null) { economyService.PointsChanged += RefreshPoints; economyService.Refresh(); RefreshPoints(economyService.Points); }
    }
    private void OnDisable() { if (profileService != null) profileService.Changed -= RefreshProfile; if (inventoryService != null) inventoryService.InventoryChanged -= RefreshV2Inventory; else if (economyService != null) economyService.PointsChanged -= RefreshPoints; }
    private void RefreshProfile(PlayerProfileData profile)
    {
        if (profile == null) return; if (nicknameText != null) nicknameText.text = profile.nickname; if (levelText != null) levelText.text = $"Nível {profile.level:00}";
        int required = profile.experienceToNext > 0 ? profile.experienceToNext : 1500; int localXp = Mathf.Max(0, profile.experience % required); if (experienceText != null) experienceText.text = $"{localXp}/{required} XP"; if (experienceSlider != null) experienceSlider.value = (float)localXp / required;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(profile.avatarId) ?? avatarCatalog.Default : null; if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
    }
    private void RefreshPoints(int points) { if (pointsText != null) pointsText.text = points.ToString("N0"); }
    private void RefreshV2Inventory(System.Collections.Generic.IReadOnlyList<PlayFab.EconomyModels.InventoryItem> _, int points) => RefreshPoints(points);
}
