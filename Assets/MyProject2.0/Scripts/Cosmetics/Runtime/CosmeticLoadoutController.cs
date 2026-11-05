using UnityEngine;

/// <summary>Connects the saved profile loadout to a world or RenderTexture capybara instance.</summary>
public sealed class CosmeticLoadoutController : MonoBehaviour
{
    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private CosmeticCatalog catalog;
    [SerializeField] private CapybaraAppearanceApplier appearance;
    private void Awake() { if (profile == null) profile = PlayerProfileService.Instance; if (appearance == null) appearance = GetComponent<CapybaraAppearanceApplier>(); }
    private void OnEnable() { if (profile != null) profile.Changed += Apply; Apply(profile != null ? profile.Current : null); }
    private void OnDisable() { if (profile != null) profile.Changed -= Apply; }
    private void Apply(PlayerProfileData data) => appearance?.Apply(data != null ? data.cosmetics : new PlayerCosmeticLoadout(), catalog);
    public void PreviewColor(CapybaraColorSlot slot, float hue, float saturation) { PlayerCosmeticLoadout value = profile != null ? profile.Current.cosmetics : new PlayerCosmeticLoadout(); value.Set(slot, new CapybaraColorSelection(hue, saturation)); appearance?.Apply(value, catalog); }
    public void EquipExpression(string itemId) { if (profile == null) return; profile.Current.cosmetics.expressionId = itemId; profile.SaveCosmetics(profile.Current.cosmetics); }
    public void EquipHeadAccessory(string itemId) { if (profile == null) return; profile.Current.cosmetics.headAccessoryId = itemId; profile.SaveCosmetics(profile.Current.cosmetics); }
}
