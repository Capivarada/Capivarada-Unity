using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies cosmetics while preserving the source model's brightness per material.</summary>
public sealed class CapybaraAppearanceApplier : MonoBehaviour
{
    [Serializable]
    private sealed class MaterialSlot
    {
        [SerializeField] private CapybaraColorSlot slot;
        [SerializeField] private Renderer renderer;
        [SerializeField] private int materialIndex;

        public MaterialSlot(CapybaraColorSlot slot, Renderer renderer, int materialIndex)
        {
            this.slot = slot;
            this.renderer = renderer;
            this.materialIndex = materialIndex;
        }

        public CapybaraColorSlot Slot { get { return slot; } }
        public Renderer Renderer { get { return renderer; } }
        public int MaterialIndex { get { return materialIndex; } }
    }

    [SerializeField] private MaterialSlot[] materialSlots = Array.Empty<MaterialSlot>();
    [SerializeField] private CapybaraEyeController eyes;
    [SerializeField] private HeadAccessoryApplier headAccessory;

    private readonly Dictionary<Renderer, MaterialPropertyBlock> blocks = new Dictionary<Renderer, MaterialPropertyBlock>();

    public PlayerCosmeticLoadout AppliedLoadout { get; private set; } = new PlayerCosmeticLoadout();

    /// <summary>
    /// Returns the colour currently authored for a cosmetic slot, retaining the
    /// source material's value/brightness. This is deliberately the same
    /// calculation used by <see cref="Apply"/>, so UI and gameplay effects
    /// (such as the impulse arrow) always match the rendered capybara.
    /// </summary>
    public Color GetAppliedColor(CapybaraColorSlot slot)
    {
        EnsureMaterialSlots();

        foreach (MaterialSlot item in materialSlots)
        {
            if (item == null || item.Slot != slot || item.Renderer == null ||
                item.MaterialIndex < 0 || item.MaterialIndex >= item.Renderer.sharedMaterials.Length)
                continue;

            Material material = item.Renderer.sharedMaterials[item.MaterialIndex];
            if (material == null)
                continue;

            Color.RGBToHSV(ReadMaterialColor(material), out _, out _, out float sourceValue);
            CapybaraColorSelection choice = AppliedLoadout.Get(slot);
            return Color.HSVToRGB(
                Mathf.Repeat(choice.Hue, 1f),
                Mathf.Clamp01(choice.Saturation),
                Mathf.Max(.02f, sourceValue * choice.EffectiveBrightness));
        }

        return Color.white;
    }

    public void Apply(PlayerCosmeticLoadout loadout, CosmeticCatalog catalog)
    {
        EnsureMaterialSlots();
        AppliedLoadout = loadout ?? CreateDefaultLoadoutFromMaterials();

        foreach (MaterialSlot item in materialSlots)
        {
            if (item == null || item.Renderer == null || item.MaterialIndex < 0 || item.MaterialIndex >= item.Renderer.sharedMaterials.Length)
                continue;

            Material material = item.Renderer.sharedMaterials[item.MaterialIndex];
            if (material == null)
                continue;

            Color color = GetAppliedColor(item.Slot);

            MaterialPropertyBlock block;
            if (!blocks.TryGetValue(item.Renderer, out block))
            {
                block = new MaterialPropertyBlock();
                blocks.Add(item.Renderer, block);
            }

            item.Renderer.GetPropertyBlock(block, item.MaterialIndex);
            if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) block.SetColor("_Color", color);
            item.Renderer.SetPropertyBlock(block, item.MaterialIndex);

            // Some imported model shaders do not render per-submesh property blocks
            // consistently. Keep the isolated renderer material in sync so previews
            // and instantiated players always show the selected colour.
            Material[] runtimeMaterials = item.Renderer.materials;
            if (item.MaterialIndex < runtimeMaterials.Length)
            {
                Material runtimeMaterial = runtimeMaterials[item.MaterialIndex];
                if (runtimeMaterial != null)
                {
                    if (runtimeMaterial.HasProperty("_BaseColor")) runtimeMaterial.SetColor("_BaseColor", color);
                    else if (runtimeMaterial.HasProperty("_Color")) runtimeMaterial.SetColor("_Color", color);
                }
            }
        }

        if (catalog == null)
            return;

        if (eyes != null)
            eyes.SetExpression(catalog.FindExpression(AppliedLoadout.expressionId) ?? catalog.DefaultExpression);
        if (headAccessory != null)
            headAccessory.SetAccessory(catalog.FindHeadAccessory(AppliedLoadout.headAccessoryId));
    }

    /// <summary>Reads the actual material palette of the current prefab as the base appearance.</summary>
    public PlayerCosmeticLoadout CreateDefaultLoadoutFromMaterials()
    {
        EnsureMaterialSlots();
        PlayerCosmeticLoadout result = new PlayerCosmeticLoadout();

        foreach (MaterialSlot item in materialSlots)
        {
            if (item == null || item.Renderer == null || item.MaterialIndex < 0 || item.MaterialIndex >= item.Renderer.sharedMaterials.Length)
                continue;

            Material material = item.Renderer.sharedMaterials[item.MaterialIndex];
            if (material == null)
                continue;

            Color.RGBToHSV(ReadMaterialColor(material), out float hue, out float saturation, out _);
            result.Set(item.Slot, new CapybaraColorSelection(hue, saturation));
        }

        return result;
    }

    private void EnsureMaterialSlots()
    {
        if (HasValidSlots())
            return;

        Renderer exactFourMaterialRenderer = null;
        Renderer fallbackRenderer = null;
        int fallbackMaterialCount = 0;

        foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
        {
            int count = candidate != null && candidate.sharedMaterials != null ? candidate.sharedMaterials.Length : 0;
            if (count == 4 && exactFourMaterialRenderer == null)
                exactFourMaterialRenderer = candidate;
            if (count >= 4 && count > fallbackMaterialCount)
            {
                fallbackRenderer = candidate;
                fallbackMaterialCount = count;
            }
        }

        Renderer renderer = exactFourMaterialRenderer != null ? exactFourMaterialRenderer : fallbackRenderer;
        if (renderer == null)
            return;

        if (renderer.sharedMaterials.Length == 4)
        {
            // Capivarada.fbx: body, ears, snout, hooves.
            materialSlots = new[]
            {
                new MaterialSlot(CapybaraColorSlot.Body, renderer, 0),
                new MaterialSlot(CapybaraColorSlot.Ears, renderer, 1),
                new MaterialSlot(CapybaraColorSlot.Snout, renderer, 2),
                new MaterialSlot(CapybaraColorSlot.Hooves, renderer, 3)
            };
            return;
        }

        // Existing network visual: skin, tail, paws, nose, ears, mouth, eyes.
        materialSlots = new[]
        {
            new MaterialSlot(CapybaraColorSlot.Body, renderer, 0),
            new MaterialSlot(CapybaraColorSlot.Hooves, renderer, 2),
            new MaterialSlot(CapybaraColorSlot.Snout, renderer, 3),
            new MaterialSlot(CapybaraColorSlot.Ears, renderer, 4)
        };
    }

    private bool HasValidSlots()
    {
        if (materialSlots == null || materialSlots.Length == 0)
            return false;

        foreach (MaterialSlot item in materialSlots)
        {
            if (item != null && item.Renderer != null && item.MaterialIndex >= 0 && item.MaterialIndex < item.Renderer.sharedMaterials.Length)
                return true;
        }

        return false;
    }

    private static Color ReadMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }
}
