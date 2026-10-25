using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies only the seven material slots of a capybara skin.</summary>
public class PlayerCustomizationApplier : MonoBehaviour
{
    [Serializable]
    private class SkinMaterialSlot
    {
        [SerializeField] private Renderer renderer;
        [SerializeField] private int materialIndex;

        public SkinMaterialSlot(Renderer renderer, int materialIndex)
        {
            this.renderer = renderer;
            this.materialIndex = materialIndex;
        }

        public Renderer Renderer => renderer;
        public int MaterialIndex => materialIndex;
    }

    [SerializeField] private SkinMaterialSlot[] skinMaterialSlots =
        new SkinMaterialSlot[WardrobeSkin.MaterialSlotCount];

    private readonly Dictionary<Renderer, MaterialPropertyBlock> propertyBlocks = new();

    private void Awake()
    {
        EnsureSkinMaterialMapping();
    }

    public void Apply(PlayerCustomization customization)
    {
        if (customization == null)
        {
            Debug.LogWarning($"[{nameof(PlayerCustomizationApplier)}] Customization is null on {name}.");
            return;
        }

        ApplySkin(customization.Skin);
    }

    public void ApplySkinOnly(WardrobeSkin skin) => ApplySkin(skin);

    public void ApplySkin(WardrobeSkin skin)
    {
        if (skin == null)
            return;

        EnsureSkinMaterialMapping();
        for (int i = 0; i < skinMaterialSlots.Length; i++)
        {
            SkinMaterialSlot slot = skinMaterialSlots[i];
            if (slot == null || slot.Renderer == null ||
                slot.MaterialIndex < 0 || slot.MaterialIndex >= slot.Renderer.sharedMaterials.Length)
                continue;

            SkinColorData data = skin.GetColorData(i);
            if (data == null)
                continue;

            if (!propertyBlocks.TryGetValue(slot.Renderer, out MaterialPropertyBlock block))
            {
                block = new MaterialPropertyBlock();
                propertyBlocks.Add(slot.Renderer, block);
            }

            // Do not carry a property block from the previous selected skin.
            // Residual material values were producing the dark/black preview.
            block.Clear();
            Material material = slot.Renderer.sharedMaterials[slot.MaterialIndex];
            if (material != null && material.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", data.color);
            else if (material != null && material.HasProperty("_Color"))
                block.SetColor("_Color", data.color);

            if (material != null && material.HasProperty("_Metallic"))
                block.SetFloat("_Metallic", data.metallic);
            if (material != null && material.HasProperty("_Smoothness"))
                block.SetFloat("_Smoothness", data.smoothness);
            else if (material != null && material.HasProperty("_Glossiness"))
                block.SetFloat("_Glossiness", data.smoothness);

            slot.Renderer.SetPropertyBlock(block, slot.MaterialIndex);
        }
    }

    private void EnsureSkinMaterialMapping()
    {
        if (HasValidExplicitMapping())
            return;

        List<SkinMaterialSlot> discoveredSlots = new();
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
                discoveredSlots.Add(new SkinMaterialSlot(renderer, materialIndex));
        }

        if (discoveredSlots.Count < WardrobeSkin.MaterialSlotCount)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerCustomizationApplier)}] {name} has fewer than " +
                $"{WardrobeSkin.MaterialSlotCount} skinned material slots.");
        }

        skinMaterialSlots = discoveredSlots.ToArray();
    }

    private bool HasValidExplicitMapping()
    {
        if (skinMaterialSlots == null || skinMaterialSlots.Length != WardrobeSkin.MaterialSlotCount)
            return false;

        for (int i = 0; i < skinMaterialSlots.Length; i++)
            if (skinMaterialSlots[i] == null || skinMaterialSlots[i].Renderer == null)
                return false;

        return true;
    }
}
