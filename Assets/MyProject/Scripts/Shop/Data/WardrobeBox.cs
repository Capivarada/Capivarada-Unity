using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RarityDrop
{
    public Rarity rarity;

    [Min(0f)]
    public float chance;

}

[CreateAssetMenu(
    fileName = "New Wardrobe Box",
    menuName = "Capivarada/Shop/Wardrobe Box"
)]
public class WardrobeBox : ScriptableObject
{
    [SerializeField] private string boxId;
    [SerializeField] private string boxName;
    [SerializeField] private int price;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Rarity visualRarity;
    [SerializeField] private bool isAvailable = true;
    [SerializeField] private List<RarityDrop> dropRates = new();

    public string BoxId => boxId;
    public string BoxName => string.IsNullOrWhiteSpace(boxName) ? name : boxName;
    public int Price => price;
    public Sprite Sprite => sprite;
    public Rarity VisualRarity => visualRarity;
    public bool IsAvailable => isAvailable;
    public IReadOnlyList<RarityDrop> DropRates => dropRates;

    public bool Validate(SkinCatalog catalog, out string error)
    {
        if (catalog == null) { error = "SkinCatalog ausente."; return false; }
        if (string.IsNullOrWhiteSpace(boxId)) { error = $"Caixa sem ID: {name}."; return false; }
        float total = 0f;
        foreach (RarityDrop drop in dropRates)
        {
            if (drop == null || drop.chance < 0f) { error = $"Drop inválido em {name}."; return false; }
            total += drop.chance;
            if (drop.chance > 0f && catalog.GetEligibleForBox(drop.rarity).Count == 0) { error = $"{name} possui chance para {drop.rarity}, sem skins elegíveis no SkinCatalog."; return false; }
        }
        if (Mathf.Abs(total - 100f) > 0.01f) { error = $"As chances de {name} somam {total:0.##}, não 100%."; return false; }
        error = null; return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string generatedId = ToCamelCaseId(string.IsNullOrWhiteSpace(boxName) ? name : boxName);
        if (!string.IsNullOrWhiteSpace(generatedId) && boxId != generatedId)
        {
            boxId = generatedId;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    private static string ToCamelCaseId(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return string.Empty;
        string normalized = source.Normalize(System.Text.NormalizationForm.FormD);
        var result = new System.Text.StringBuilder();
        bool capitalize = false;
        foreach (char character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character))
            {
                result.Append(result.Length == 0 ? char.ToLowerInvariant(character) : (capitalize ? char.ToUpperInvariant(character) : character));
                capitalize = false;
            }
            else capitalize = result.Length > 0;
        }
        return result.ToString();
    }
#endif
}
