using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCustomizationApplier : MonoBehaviour
{
    [Serializable]
    private class SkinMaterialSlot
    {
        [SerializeField] private Renderer renderer;
        [SerializeField] private int materialIndex;

        public SkinMaterialSlot()
        {
        }

        public SkinMaterialSlot(Renderer renderer, int materialIndex)
        {
            this.renderer = renderer;
            this.materialIndex = materialIndex;
        }

        public Renderer Renderer => renderer;
        public int MaterialIndex => materialIndex;
    }

    [Header("Equipment slots")]
    [SerializeField] private Transform headSlot;
    [SerializeField] private Transform accessorySlot;
    [SerializeField] private Transform clothingSlot;

    [Header("Skin material mapping")]
    [SerializeField] private SkinMaterialSlot[] skinMaterialSlots =
        new SkinMaterialSlot[WardrobeSkin.MaterialSlotCount];

    private readonly Dictionary<Renderer, Material[]> runtimeMaterials = new();
    private readonly Dictionary<WardrobeCategory, GameObject> equippedObjects = new();

    private void Awake()
    {
        EnsureEquipmentSlots();
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
        ApplyEquipment(WardrobeCategory.Cabeca, customization.Head, headSlot);
        ApplyEquipment(WardrobeCategory.Acessorios, customization.Accessory, accessorySlot);
        ApplyEquipment(WardrobeCategory.Roupas, customization.Clothing, clothingSlot);
    }

    public void ApplySkin(WardrobeSkin skin)
    {
        if (skin == null)
            return;

        EnsureSkinMaterialMapping();

        for (int i = 0; i < skinMaterialSlots.Length; i++)
        {
            SkinMaterialSlot slot = skinMaterialSlots[i];
            if (slot == null || slot.Renderer == null)
                continue;

            Material[] materials = GetRuntimeMaterials(slot.Renderer);
            if (slot.MaterialIndex < 0 || slot.MaterialIndex >= materials.Length)
                continue;

            SkinColorData data = skin.GetColorData(i);
            Material material = materials[slot.MaterialIndex];

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", data.color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", data.color);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", data.metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", data.smoothness);
        }
    }

    public void ClearEquipment(WardrobeCategory category)
    {
        if (equippedObjects.TryGetValue(category, out GameObject equipped) &&
            equipped != null)
        {
            Destroy(equipped);
        }

        equippedObjects.Remove(category);
    }

    private void ApplyEquipment(
        WardrobeCategory category,
        WardrobeEquipment item,
        Transform slot)
    {
        ClearEquipment(category);

        if (item == null || item.Prefab == null || slot == null)
            return;

        GameObject equipped = Instantiate(item.Prefab, slot);
        equipped.transform.localPosition = Vector3.zero;
        equipped.transform.localRotation = Quaternion.identity;
        equipped.transform.localScale = Vector3.one;
        equippedObjects[category] = equipped;
    }

    private Material[] GetRuntimeMaterials(Renderer renderer)
    {
        if (runtimeMaterials.TryGetValue(renderer, out Material[] materials))
            return materials;

        Material[] sharedMaterials = renderer.sharedMaterials;
        materials = new Material[sharedMaterials.Length];

        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            if (sharedMaterials[i] != null)
                materials[i] = new Material(sharedMaterials[i]);
        }

        renderer.materials = materials;
        runtimeMaterials.Add(renderer, materials);
        return materials;
    }

    private void EnsureSkinMaterialMapping()
    {
        if (HasValidExplicitMapping())
            return;

        List<SkinMaterialSlot> discoveredSlots = new();
        SkinnedMeshRenderer[] renderers =
            GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            int materialCount = renderer.sharedMaterials.Length;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                discoveredSlots.Add(CreateSlot(renderer, materialIndex));
            }
        }

        if (discoveredSlots.Count < WardrobeSkin.MaterialSlotCount)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerCustomizationApplier)}] {name} has fewer than " +
                $"{WardrobeSkin.MaterialSlotCount} skinned material slots."
            );
        }

        skinMaterialSlots = discoveredSlots.ToArray();
    }

    private bool HasValidExplicitMapping()
    {
        if (skinMaterialSlots == null ||
            skinMaterialSlots.Length != WardrobeSkin.MaterialSlotCount)
            return false;

        for (int i = 0; i < skinMaterialSlots.Length; i++)
        {
            if (skinMaterialSlots[i] == null || skinMaterialSlots[i].Renderer == null)
                return false;
        }

        return true;
    }

    private SkinMaterialSlot CreateSlot(Renderer renderer, int materialIndex)
    {
        return new SkinMaterialSlot(renderer, materialIndex);
    }

    private void EnsureEquipmentSlots()
    {
        headSlot = EnsureSlot(headSlot, "HeadSlot");
        accessorySlot = EnsureSlot(accessorySlot, "AccessorySlot");
        clothingSlot = EnsureSlot(clothingSlot, "ClothingSlot");
    }

    private Transform EnsureSlot(Transform slot, string slotName)
    {
        if (slot != null)
            return slot;

        GameObject slotObject = new(slotName);
        slotObject.transform.SetParent(transform, false);
        return slotObject.transform;
    }

    private void OnDestroy()
    {
        foreach (Material[] materials in runtimeMaterials.Values)
        {
            foreach (Material material in materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }

}
