using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>New wardrobe base: free colors/expressions and owned 3D head accessories.</summary>
public sealed class WardrobeCosmeticsController : MonoBehaviour
{
    private const int ColorSpectrumResolution = 256;
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
    private readonly List<SliderSpectrum> colorSpectra = new();

    private void Awake()
    {
        if (profile == null) profile = PlayerProfileService.Instance;
        if (inventory == null) inventory = GetComponent<CosmeticInventory>();
        if (preview == null) preview = FindFirstObjectByType<CapybaraAppearanceApplier>();
        ConfigureColorSpectra();
    }
    private void OnEnable() { if (profile != null) profile.Changed += OnProfileChanged; if (inventory != null) inventory.Changed += OnInventoryChanged; foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Enable(); inventory?.Refresh(); Refresh(); }
    private void OnDisable() { if (profile != null) profile.Changed -= OnProfileChanged; if (inventory != null) inventory.Changed -= OnInventoryChanged; foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Disable(); }
    private void OnDestroy() { foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Dispose(); }
    public void PreviewColors()
    {
        PlayerCosmeticLoadout loadout = profile != null ? profile.Current.cosmetics : new PlayerCosmeticLoadout();
        Set(loadout, CapybaraColorSlot.Body, bodyHue, bodySaturation); Set(loadout, CapybaraColorSlot.Snout, snoutHue, snoutSaturation); Set(loadout, CapybaraColorSlot.Ears, earsHue, earsSaturation); Set(loadout, CapybaraColorSlot.Hooves, hoovesHue, hoovesSaturation);
        preview?.Apply(loadout, catalog);
    }
    public void PreviewColors(float _) => PreviewColors();
    public void SaveColors()
    {
        if (profile == null) return;
        PreviewColors();
        profile.SaveCosmetics(profile.Current.cosmetics, OnCosmeticsSaved);
    }
    private void OnProfileChanged(PlayerProfileData _) => Refresh();
    private void OnInventoryChanged() => Refresh();
    private void OnCosmeticsSaved() => Refresh();
    private void Refresh()
    {
        if (profile == null) return;
        PlayerCosmeticLoadout loadout = profile.Current.cosmetics ??= new PlayerCosmeticLoadout();
        Bind(loadout.Get(CapybaraColorSlot.Body), bodyHue, bodySaturation); Bind(loadout.Get(CapybaraColorSlot.Snout), snoutHue, snoutSaturation); Bind(loadout.Get(CapybaraColorSlot.Ears), earsHue, earsSaturation); Bind(loadout.Get(CapybaraColorSlot.Hooves), hoovesHue, hoovesSaturation);
        foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Refresh();
        preview?.Apply(loadout, catalog); Clear(); SpawnExpressions(loadout); SpawnAccessories(loadout);
    }
    private void ConfigureColorSpectra()
    {
        AddColorSpectra(bodyHue, bodySaturation);
        AddColorSpectra(snoutHue, snoutSaturation);
        AddColorSpectra(earsHue, earsSaturation);
        AddColorSpectra(hoovesHue, hoovesSaturation);
    }
    private void AddColorSpectra(Slider hue, Slider saturation)
    {
        if (hue != null) colorSpectra.Add(new SliderSpectrum(hue, null, true));
        if (saturation != null) colorSpectra.Add(new SliderSpectrum(saturation, hue, false));
    }
    private void SpawnExpressions(PlayerCosmeticLoadout loadout) { if (catalog == null || expressionPrefab == null || expressionsContent == null) return; foreach (EyeExpressionDefinition expression in catalog.Expressions) if (expression != null) Add(expressionPrefab, expressionsContent).Bind(expression.DisplayName, expression.Icon, true, expression.ItemId == loadout.expressionId, () => { loadout.expressionId = expression.ItemId; profile.SaveCosmetics(loadout, OnCosmeticsSaved); }); }
    private void SpawnAccessories(PlayerCosmeticLoadout loadout) { if (catalog == null || accessoryPrefab == null || accessoriesContent == null) return; foreach (HeadAccessoryDefinition accessory in catalog.HeadAccessories) if (accessory != null) { bool owned = inventory != null && inventory.Owns(accessory.ItemId); Add(accessoryPrefab, accessoriesContent).Bind(accessory.DisplayName, accessory.Icon, owned, accessory.ItemId == loadout.headAccessoryId, () => { if (!owned) return; loadout.headAccessoryId = accessory.ItemId; profile.SaveCosmetics(loadout, OnCosmeticsSaved); }); } }
    private CosmeticChoiceCard Add(CosmeticChoiceCard prefab, Transform parent) { CosmeticChoiceCard card = Instantiate(prefab, parent); cards.Add(card); return card; }
    private void Clear() { foreach (CosmeticChoiceCard card in cards) if (card != null) Destroy(card.gameObject); cards.Clear(); }
    private static void Set(PlayerCosmeticLoadout data, CapybaraColorSlot slot, Slider hue, Slider saturation) { if (hue != null && saturation != null) data.Set(slot, new CapybaraColorSelection(hue.value, saturation.value)); }
    private static void Bind(CapybaraColorSelection value, Slider hue, Slider saturation) { if (hue != null) hue.SetValueWithoutNotify(value.Hue); if (saturation != null) saturation.SetValueWithoutNotify(value.Saturation); }

    /// <summary>Runtime-only visual track for a color slider. The Slider value remains the authoritative input.</summary>
    private sealed class SliderSpectrum
    {
        private readonly Slider slider;
        private readonly Slider hueSource;
        private readonly bool isHueSpectrum;
        private readonly RawImage target;
        private readonly Texture2D texture;
        private readonly Color[] pixels = new Color[ColorSpectrumResolution];
        private readonly UnityEngine.Events.UnityAction<float> refreshAction;
        private bool enabled;

        public SliderSpectrum(Slider slider, Slider hueSource, bool isHueSpectrum)
        {
            this.slider = slider;
            this.hueSource = hueSource;
            this.isHueSpectrum = isHueSpectrum;
            target = FindOrCreateTarget(slider);
            texture = new Texture2D(ColorSpectrumResolution, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            if (target != null) { target.texture = texture; target.raycastTarget = false; }
            if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill)) fill.color = new Color(fill.color.r, fill.color.g, fill.color.b, 0f);
            refreshAction = _ => Refresh();
            Refresh();
        }

        public void Enable()
        {
            if (enabled) return;
            enabled = true;
            if (hueSource != null) hueSource.onValueChanged.AddListener(refreshAction);
            Refresh();
        }

        public void Disable()
        {
            if (!enabled) return;
            enabled = false;
            if (hueSource != null) hueSource.onValueChanged.RemoveListener(refreshAction);
        }

        public void Dispose()
        {
            Disable();
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }

        public void Refresh()
        {
            if (texture == null) return;
            float hue = hueSource != null ? hueSource.value : 0f;
            for (int index = 0; index < pixels.Length; index++)
            {
                float position = index / (float)(pixels.Length - 1);
                pixels[index] = isHueSpectrum
                    ? Color.HSVToRGB(position, 1f, 1f)
                    : Color.HSVToRGB(hue, position, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        private static RawImage FindOrCreateTarget(Slider slider)
        {
            if (slider == null) return null;
            Transform background = slider.transform.Find("Background");
            RawImage existing = background != null ? background.GetComponentInChildren<RawImage>(true) : null;
            if (existing != null) return existing;

            Transform parent = background != null ? background : slider.transform;
            GameObject gameObject = new("ColorGradient", typeof(RectTransform), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (background == null) gameObject.transform.SetSiblingIndex(Mathf.Min(1, slider.transform.childCount - 1));
            return gameObject.GetComponent<RawImage>();
        }
    }
}

public sealed class CosmeticChoiceCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText, stateText;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockVisual, selectedVisual;
    public void Bind(string title, Sprite sprite, bool owned, bool selected, Action clicked) { if (nameText != null) nameText.text = title; if (stateText != null) stateText.text = selected ? "Equipado" : owned ? "Obtido" : "Bloqueado"; if (icon != null) icon.sprite = sprite; if (lockVisual != null) lockVisual.SetActive(!owned); if (selectedVisual != null) selectedVisual.SetActive(selected); if (button != null) { button.onClick.RemoveAllListeners(); button.interactable = owned; button.onClick.AddListener(() => clicked?.Invoke()); } }
}
