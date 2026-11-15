using System;
using System.Collections.Generic;
using UnityEngine;

public enum CosmeticItemType { Expression, HeadAccessory }
public enum CapybaraColorSlot { Body, Snout, Ears, Hooves }

[Serializable]
public struct CapybaraColorSelection
{
    [Range(0f, 1f)] public float Hue;
    [Range(0f, 1f)] public float Saturation;
    [Range(0f, 1f)] public float Brightness;

    public CapybaraColorSelection(float hue, float saturation, float brightness = 1f)
    {
        Hue = hue;
        Saturation = saturation;
        Brightness = brightness;
    }

    public float EffectiveBrightness => Mathf.Clamp01(Brightness);
}

[Serializable]
public sealed class PlayerCosmeticLoadout
{
    // Version 2 stores brightness as HSV value (black through white). Older
    // loadouts retain their authored FBX value when displayed.
    public int colorVersion = 2;
    public CapybaraColorSelection body = new(.075f, .55f);
    public CapybaraColorSelection snout = new(.06f, .25f);
    public CapybaraColorSelection ears = new(.075f, .45f);
    public CapybaraColorSelection hooves = new(.05f, .45f);
    public string expressionId = "expression.default";
    public string headAccessoryId = string.Empty;

    public CapybaraColorSelection Get(CapybaraColorSlot slot) => slot switch
    {
        CapybaraColorSlot.Body => body,
        CapybaraColorSlot.Snout => snout,
        CapybaraColorSlot.Ears => ears,
        _ => hooves
    };

    public void Set(CapybaraColorSlot slot, CapybaraColorSelection value)
    {
        switch (slot)
        {
            case CapybaraColorSlot.Body: body = value; break;
            case CapybaraColorSlot.Snout: snout = value; break;
            case CapybaraColorSlot.Ears: ears = value; break;
            default: hooves = value; break;
        }
    }

    public PlayerCosmeticLoadout Copy() => new()
    {
        colorVersion = colorVersion,
        body = body,
        snout = snout,
        ears = ears,
        hooves = hooves,
        expressionId = expressionId,
        headAccessoryId = headAccessoryId
    };
}
public abstract class CosmeticDefinition : ScriptableObject { [SerializeField] private string itemId; [SerializeField] private string displayName; [SerializeField] private Sprite icon; public string ItemId => itemId; public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName; public Sprite Icon => icon; }
[CreateAssetMenu(fileName = "Expression", menuName = "Capivarada/Cosmetics/Eye Expression")] public sealed class EyeExpressionDefinition : CosmeticDefinition { [SerializeField] private Sprite openEyes, closedEyes, pupilSprite; [SerializeField, Min(.05f)] private float blinkDuration = .12f; public Sprite OpenEyes => openEyes; public Sprite ClosedEyes => closedEyes != null ? closedEyes : openEyes; public Sprite PupilSprite => pupilSprite; public float BlinkDuration => blinkDuration; }
[Serializable] public sealed class AccessoryMaterialColor { [SerializeField] private string rendererPath; [SerializeField] private int materialIndex; [SerializeField] private Color color = Color.white; public string RendererPath => rendererPath; public int MaterialIndex => materialIndex; public Color Color => color; }
[CreateAssetMenu(fileName = "HeadAccessory", menuName = "Capivarada/Cosmetics/Head Accessory")] public sealed class HeadAccessoryDefinition : CosmeticDefinition { [SerializeField] private GameObject prefab; [SerializeField] private Vector3 localPosition, localEulerAngles, localScale = Vector3.one; [SerializeField] private List<AccessoryMaterialColor> materialColors = new(); public GameObject Prefab => prefab; public Vector3 LocalPosition => localPosition; public Vector3 LocalEulerAngles => localEulerAngles; public Vector3 LocalScale => localScale; public IReadOnlyList<AccessoryMaterialColor> MaterialColors => materialColors; }
[CreateAssetMenu(fileName = "CosmeticCatalog", menuName = "Capivarada/Cosmetics/Catalog")] public sealed class CosmeticCatalog : ScriptableObject { [SerializeField] private EyeExpressionDefinition defaultExpression; [SerializeField] private List<EyeExpressionDefinition> expressions = new(); [SerializeField] private List<HeadAccessoryDefinition> headAccessories = new(); public EyeExpressionDefinition DefaultExpression => defaultExpression; public IReadOnlyList<EyeExpressionDefinition> Expressions => expressions; public IReadOnlyList<HeadAccessoryDefinition> HeadAccessories => headAccessories; public EyeExpressionDefinition FindExpression(string id) => expressions.Find(x => x != null && x.ItemId == id) ?? (defaultExpression != null && defaultExpression.ItemId == id ? defaultExpression : null); public HeadAccessoryDefinition FindHeadAccessory(string id) => headAccessories.Find(x => x != null && x.ItemId == id); }
