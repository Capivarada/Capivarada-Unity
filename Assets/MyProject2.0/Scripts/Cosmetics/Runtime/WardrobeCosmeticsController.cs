using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Owns the editable wardrobe draft. Preview changes are local until SaveAll is invoked.</summary>
public sealed class WardrobeCosmeticsController : MonoBehaviour
{
    private const int ColorSpectrumResolution = 256;
    private const float MinimumBrightness = .1f;

    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private CosmeticCatalog catalog;
    [SerializeField] private CosmeticInventory inventory;
    [SerializeField] private CapybaraAppearanceApplier preview;

    [Header("Free colors")]
    [SerializeField] private Slider bodyHue;
    [SerializeField] private Slider bodySaturation;
    [SerializeField] private Slider bodyBrightness;
    [SerializeField] private Slider snoutHue;
    [SerializeField] private Slider snoutSaturation;
    [SerializeField] private Slider snoutBrightness;
    [SerializeField] private Slider earsHue;
    [SerializeField] private Slider earsSaturation;
    [SerializeField] private Slider earsBrightness;
    [SerializeField] private Slider hoovesHue;
    [SerializeField] private Slider hoovesSaturation;
    [SerializeField] private Slider hoovesBrightness;

    [Header("Free expressions")]
    [SerializeField] private Transform expressionsContent;
    [SerializeField] private CosmeticChoiceCard expressionPrefab;

    [Header("Owned accessories")]
    [SerializeField] private Transform accessoriesContent;
    [SerializeField] private CosmeticChoiceCard accessoryPrefab;

    private readonly List<CosmeticChoiceCard> cards = new();
    private readonly List<SliderSpectrum> colorSpectra = new();
    private PlayerCosmeticLoadout workingLoadout;

    private void Awake()
    {
        if (profile == null) profile = PlayerProfileService.Instance;
        if (inventory == null) inventory = GetComponent<CosmeticInventory>();
        if (preview == null) preview = FindFirstObjectByType<CapybaraAppearanceApplier>();
        ConfigureColorSpectra();
    }

    private void OnEnable()
    {
        if (profile != null) profile.Changed += OnProfileChanged;
        if (inventory != null) inventory.Changed += OnInventoryChanged;
        foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Enable();
        inventory?.Refresh();
        RefreshFromSavedLoadout();
    }

    private void OnDisable()
    {
        if (profile != null) profile.Changed -= OnProfileChanged;
        if (inventory != null) inventory.Changed -= OnInventoryChanged;
        foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Disable();
    }

    private void OnDestroy()
    {
        foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Dispose();
    }

    /// <summary>Inspector-safe slider callback. This never persists the profile.</summary>
    public void PreviewColors()
    {
        EnsureWorkingLoadout();
        Set(workingLoadout, CapybaraColorSlot.Body, bodyHue, bodySaturation, bodyBrightness);
        Set(workingLoadout, CapybaraColorSlot.Snout, snoutHue, snoutSaturation, snoutBrightness);
        Set(workingLoadout, CapybaraColorSlot.Ears, earsHue, earsSaturation, earsBrightness);
        Set(workingLoadout, CapybaraColorSlot.Hooves, hoovesHue, hoovesSaturation, hoovesBrightness);
        ApplyWorkingPreview();
    }

    public void PreviewColors(float _) => PreviewColors();

    /// <summary>General Save button callback for colors, expression and accessory together.</summary>
    public void SaveAll()
    {
        if (profile == null) return;
        PreviewColors();
        profile.SaveCosmetics(workingLoadout.Copy(), OnCosmeticsSaved);
    }

    /// <summary>Compatibility for the pre-existing Save button binding.</summary>
    public void SaveColors() => SaveAll();

    /// <summary>Returns the editable draft to its base capybara state. Press SaveAll to persist it.</summary>
    public void RestoreDefaults()
    {
        workingLoadout = CreateMaterialDefaults();
        BindControls(workingLoadout);
        RefreshSpectra();
        ApplyWorkingPreview();
        RebuildChoiceCards();
    }

    private void OnProfileChanged(PlayerProfileData _) => RefreshFromSavedLoadout();
    private void OnInventoryChanged() => RebuildChoiceCards();
    private void OnCosmeticsSaved() => RefreshFromSavedLoadout();

    private void RefreshFromSavedLoadout()
    {
        if (profile == null) return;
        PlayerCosmeticLoadout saved = CapybaraGameplayCosmetics.ResolveLoadout(profile.Current.cosmetics);
        workingLoadout = saved != null ? saved.Copy() : CreateMaterialDefaults();
        BindControls(workingLoadout);
        RefreshSpectra();
        ApplyWorkingPreview();
        RebuildChoiceCards();
    }

    private void BindControls(PlayerCosmeticLoadout loadout)
    {
        Bind(loadout.Get(CapybaraColorSlot.Body), bodyHue, bodySaturation, bodyBrightness);
        Bind(loadout.Get(CapybaraColorSlot.Snout), snoutHue, snoutSaturation, snoutBrightness);
        Bind(loadout.Get(CapybaraColorSlot.Ears), earsHue, earsSaturation, earsBrightness);
        Bind(loadout.Get(CapybaraColorSlot.Hooves), hoovesHue, hoovesSaturation, hoovesBrightness);
    }

    private void ApplyWorkingPreview()
    {
        EnsureWorkingLoadout();
        preview?.Apply(workingLoadout, catalog);
    }

    private void EnsureWorkingLoadout()
    {
        if (workingLoadout != null) return;
        PlayerCosmeticLoadout saved = profile != null ? CapybaraGameplayCosmetics.ResolveLoadout(profile.Current.cosmetics) : null;
        workingLoadout = saved != null ? saved.Copy() : CreateMaterialDefaults();
    }

    private PlayerCosmeticLoadout CreateMaterialDefaults()
    {
        return preview != null ? preview.CreateDefaultLoadoutFromMaterials() : new PlayerCosmeticLoadout();
    }

    private void ConfigureColorSpectra()
    {
        AddColorSpectra(bodyHue, bodySaturation, bodyBrightness);
        AddColorSpectra(snoutHue, snoutSaturation, snoutBrightness);
        AddColorSpectra(earsHue, earsSaturation, earsBrightness);
        AddColorSpectra(hoovesHue, hoovesSaturation, hoovesBrightness);
    }

    private void AddColorSpectra(Slider hue, Slider saturation, Slider brightness)
    {
        if (hue != null) colorSpectra.Add(new SliderSpectrum(hue, null, null, SpectrumType.Hue));
        if (saturation != null) colorSpectra.Add(new SliderSpectrum(saturation, hue, null, SpectrumType.Saturation));
        if (brightness != null) colorSpectra.Add(new SliderSpectrum(brightness, hue, saturation, SpectrumType.Brightness));
    }

    private void RefreshSpectra()
    {
        foreach (SliderSpectrum spectrum in colorSpectra) spectrum.Refresh();
    }

    private void RebuildChoiceCards()
    {
        EnsureWorkingLoadout();
        Clear();
        SpawnExpressions();
        SpawnAccessories();
    }

    private void SpawnExpressions()
    {
        if (catalog == null || expressionPrefab == null || expressionsContent == null) return;
        foreach (EyeExpressionDefinition expression in catalog.Expressions)
        {
            if (expression == null) continue;
            Add(expressionPrefab, expressionsContent).Bind(expression.DisplayName, expression.Icon, true,
                expression.ItemId == workingLoadout.expressionId,
                () => SelectExpression(expression.ItemId));
        }
    }

    private void SpawnAccessories()
    {
        if (catalog == null || accessoryPrefab == null || accessoriesContent == null) return;
        foreach (HeadAccessoryDefinition accessory in catalog.HeadAccessories)
        {
            if (accessory == null) continue;
            bool owned = inventory != null && inventory.Owns(accessory.ItemId);
            Add(accessoryPrefab, accessoriesContent).Bind(accessory.DisplayName, accessory.Icon, owned,
                accessory.ItemId == workingLoadout.headAccessoryId,
                () => SelectAccessory(accessory.ItemId, owned));
        }
    }

    private void SelectExpression(string itemId)
    {
        EnsureWorkingLoadout();
        workingLoadout.expressionId = itemId;
        ApplyWorkingPreview();
        RebuildChoiceCards();
    }

    private void SelectAccessory(string itemId, bool owned)
    {
        if (!owned) return;
        EnsureWorkingLoadout();
        workingLoadout.headAccessoryId = itemId;
        ApplyWorkingPreview();
        RebuildChoiceCards();
    }

    private CosmeticChoiceCard Add(CosmeticChoiceCard prefab, Transform parent)
    {
        CosmeticChoiceCard card = Instantiate(prefab, parent);
        cards.Add(card);
        return card;
    }

    private void Clear()
    {
        foreach (CosmeticChoiceCard card in cards)
            if (card != null) Destroy(card.gameObject);
        cards.Clear();
    }

    private static void Set(PlayerCosmeticLoadout data, CapybaraColorSlot slot, Slider hue, Slider saturation, Slider brightness)
    {
        CapybaraColorSelection current = data.Get(slot);
        data.Set(slot, new CapybaraColorSelection(
            hue != null ? hue.value : current.Hue,
            saturation != null ? saturation.value : current.Saturation,
            brightness != null ? brightness.value : current.EffectiveBrightness));
    }

    private static void Bind(CapybaraColorSelection value, Slider hue, Slider saturation, Slider brightness)
    {
        if (hue != null) hue.SetValueWithoutNotify(value.Hue);
        if (saturation != null) saturation.SetValueWithoutNotify(value.Saturation);
        if (brightness != null)
        {
            brightness.minValue = MinimumBrightness;
            brightness.maxValue = 1f;
            brightness.SetValueWithoutNotify(value.EffectiveBrightness);
        }
    }

    private enum SpectrumType { Hue, Saturation, Brightness }

    /// <summary>Runtime-only visual track for a color slider. The Slider value remains authoritative.</summary>
    private sealed class SliderSpectrum
    {
        private readonly Slider hueSource;
        private readonly Slider saturationSource;
        private readonly SpectrumType type;
        private readonly RawImage target;
        private readonly Texture2D texture;
        private readonly Color[] pixels = new Color[ColorSpectrumResolution];
        private readonly UnityAction<float> refreshAction;
        private bool isEnabled;

        public SliderSpectrum(Slider slider, Slider hueSource, Slider saturationSource, SpectrumType type)
        {
            this.hueSource = hueSource;
            this.saturationSource = saturationSource;
            this.type = type;
            target = FindOrCreateTarget(slider);
            texture = new Texture2D(ColorSpectrumResolution, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            if (target != null)
            {
                target.texture = texture;
                target.raycastTarget = false;
            }
            if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
                fill.color = new Color(fill.color.r, fill.color.g, fill.color.b, 0f);
            refreshAction = _ => Refresh();
            Refresh();
        }

        public void Enable()
        {
            if (isEnabled) return;
            isEnabled = true;
            if (hueSource != null) hueSource.onValueChanged.AddListener(refreshAction);
            if (saturationSource != null) saturationSource.onValueChanged.AddListener(refreshAction);
            Refresh();
        }

        public void Disable()
        {
            if (!isEnabled) return;
            isEnabled = false;
            if (hueSource != null) hueSource.onValueChanged.RemoveListener(refreshAction);
            if (saturationSource != null) saturationSource.onValueChanged.RemoveListener(refreshAction);
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
            float saturation = saturationSource != null ? saturationSource.value : 1f;
            for (int index = 0; index < pixels.Length; index++)
            {
                float position = index / (float)(pixels.Length - 1);
                pixels[index] = type switch
                {
                    SpectrumType.Hue => Color.HSVToRGB(position, 1f, 1f),
                    SpectrumType.Saturation => Color.HSVToRGB(hue, position, 1f),
                    _ => Color.HSVToRGB(hue, saturation, position)
                };
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
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockVisual;
    [SerializeField] private GameObject selectedVisual;

    public void Bind(string title, Sprite sprite, bool owned, bool selected, Action clicked)
    {
        if (nameText != null) nameText.text = title;
        if (stateText != null) stateText.text = selected ? "Equipado" : owned ? "Obtido" : "Bloqueado";
        if (icon != null) icon.sprite = sprite;
        if (lockVisual != null) lockVisual.SetActive(!owned);
        if (selectedVisual != null) selectedVisual.SetActive(selected);
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.interactable = owned;
        button.onClick.AddListener(() => clicked?.Invoke());
    }
}
