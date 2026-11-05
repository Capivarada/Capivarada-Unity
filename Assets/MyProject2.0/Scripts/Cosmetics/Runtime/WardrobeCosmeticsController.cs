using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>New wardrobe base: free colors/expressions and owned 3D head accessories.</summary>
public sealed class WardrobeCosmeticsController : MonoBehaviour
{
    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private CosmeticCatalog catalog;
    [SerializeField] private CosmeticInventory inventory;
    [SerializeField] private CapybaraAppearanceApplier preview;
    [Header("Free colors")]
    [SerializeField] private Slider bodyHue, bodySaturation, snoutHue, snoutSaturation, earsHue, earsSaturation, hoovesHue, hoovesSaturation;
    [Header("Free expressions")]
    [SerializeField] private Transform expressionsContent;
    [SerializeField] private CosmeticChoiceCard expressionPrefab;
    [Header("Owned accessories")]
    [SerializeField] private Transform accessoriesContent;
    [SerializeField] private CosmeticChoiceCard accessoryPrefab;
    private readonly List<CosmeticChoiceCard> cards = new();

    private void Awake() { if (profile == null) profile = PlayerProfileService.Instance; if (inventory == null) inventory = GetComponent<CosmeticInventory>(); }
    private void OnEnable() { if (profile != null) profile.Changed += OnProfileChanged; if (inventory != null) inventory.Changed += OnInventoryChanged; inventory?.Refresh(); Refresh(); }
    private void OnDisable() { if (profile != null) profile.Changed -= OnProfileChanged; if (inventory != null) inventory.Changed -= OnInventoryChanged; }
    public void PreviewColors()
    {
        PlayerCosmeticLoadout loadout = profile != null ? profile.Current.cosmetics : new PlayerCosmeticLoadout();
        Set(loadout, CapybaraColorSlot.Body, bodyHue, bodySaturation); Set(loadout, CapybaraColorSlot.Snout, snoutHue, snoutSaturation); Set(loadout, CapybaraColorSlot.Ears, earsHue, earsSaturation); Set(loadout, CapybaraColorSlot.Hooves, hoovesHue, hoovesSaturation);
        preview?.Apply(loadout, catalog);
    }
    public void SaveColors() { PreviewColors(); profile?.SaveCosmetics(profile.Current.cosmetics, OnCosmeticsSaved); }
    private void OnProfileChanged(PlayerProfileData _) => Refresh();
    private void OnInventoryChanged() => Refresh();
    private void OnCosmeticsSaved() => Refresh();
    private void Refresh()
    {
        if (profile == null || catalog == null) return;
        PlayerCosmeticLoadout loadout = profile.Current.cosmetics ??= new PlayerCosmeticLoadout();
        Bind(loadout.Get(CapybaraColorSlot.Body), bodyHue, bodySaturation); Bind(loadout.Get(CapybaraColorSlot.Snout), snoutHue, snoutSaturation); Bind(loadout.Get(CapybaraColorSlot.Ears), earsHue, earsSaturation); Bind(loadout.Get(CapybaraColorSlot.Hooves), hoovesHue, hoovesSaturation);
        preview?.Apply(loadout, catalog); Clear(); SpawnExpressions(loadout); SpawnAccessories(loadout);
    }
    private void SpawnExpressions(PlayerCosmeticLoadout loadout) { if (expressionPrefab == null || expressionsContent == null) return; foreach (EyeExpressionDefinition expression in catalog.Expressions) if (expression != null) Add(expressionPrefab, expressionsContent).Bind(expression.DisplayName, expression.Icon, true, expression.ItemId == loadout.expressionId, () => { loadout.expressionId = expression.ItemId; profile.SaveCosmetics(loadout, OnCosmeticsSaved); }); }
    private void SpawnAccessories(PlayerCosmeticLoadout loadout) { if (accessoryPrefab == null || accessoriesContent == null) return; foreach (HeadAccessoryDefinition accessory in catalog.HeadAccessories) if (accessory != null) { bool owned = inventory != null && inventory.Owns(accessory.ItemId); Add(accessoryPrefab, accessoriesContent).Bind(accessory.DisplayName, accessory.Icon, owned, accessory.ItemId == loadout.headAccessoryId, () => { if (!owned) return; loadout.headAccessoryId = accessory.ItemId; profile.SaveCosmetics(loadout, OnCosmeticsSaved); }); } }
    private CosmeticChoiceCard Add(CosmeticChoiceCard prefab, Transform parent) { CosmeticChoiceCard card = Instantiate(prefab, parent); cards.Add(card); return card; }
    private void Clear() { foreach (CosmeticChoiceCard card in cards) if (card != null) Destroy(card.gameObject); cards.Clear(); }
    private static void Set(PlayerCosmeticLoadout data, CapybaraColorSlot slot, Slider hue, Slider saturation) { if (hue != null && saturation != null) data.Set(slot, new CapybaraColorSelection(hue.value, saturation.value)); }
    private static void Bind(CapybaraColorSelection value, Slider hue, Slider saturation) { if (hue != null) hue.SetValueWithoutNotify(value.Hue); if (saturation != null) saturation.SetValueWithoutNotify(value.Saturation); }
}

public sealed class CosmeticChoiceCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText, stateText;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockVisual, selectedVisual;
    public void Bind(string title, Sprite sprite, bool owned, bool selected, Action clicked) { if (nameText != null) nameText.text = title; if (stateText != null) stateText.text = selected ? "Equipado" : owned ? "Obtido" : "Bloqueado"; if (icon != null) icon.sprite = sprite; if (lockVisual != null) lockVisual.SetActive(!owned); if (selectedVisual != null) selectedVisual.SetActive(selected); if (button != null) { button.onClick.RemoveAllListeners(); button.interactable = owned; button.onClick.AddListener(() => clicked?.Invoke()); } }
}
