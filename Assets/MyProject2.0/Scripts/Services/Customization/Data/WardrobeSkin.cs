using UnityEngine;

[System.Serializable]
public class SkinColorData
{
    public Color color = Color.white;
    [Range(0f, 1f)] public float metallic;
    [Range(0f, 1f)] public float smoothness = 0.5f;
}

public enum SkinColorSlot { Skin, Tail, Paw, Nose, Ears, Mouth, Eyes }

[CreateAssetMenu(fileName = "New Capybara Skin", menuName = "Capivarada/Customization/Skin")]
public class WardrobeSkin : ScriptableObject
{
    public const int MaterialSlotCount = 7;
    // Serialized names deliberately preserve all existing skin assets.
    [SerializeField] private string contentId;
    [SerializeField] private string contentName;
    [SerializeField] private Rarity rarity;
    [SerializeField] private SkinColorData[] colors = new SkinColorData[MaterialSlotCount];

    public string SkinId => contentId;
    public string SkinName => string.IsNullOrWhiteSpace(contentName) ? name : contentName;
    public Rarity Rarity => rarity;
    public SkinColorData[] Colors => colors;

    private void OnEnable() => EnsureColorData();
#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureColorData();
        string generatedId = ToCamelCaseId(string.IsNullOrWhiteSpace(contentName) ? name : contentName);
        if (!string.IsNullOrWhiteSpace(generatedId) && contentId != generatedId)
        {
            contentId = generatedId;
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
    public SkinColorData GetColorData(int index)
    {
        EnsureColorData();
        return index >= 0 && index < colors.Length ? colors[index] : null;
    }
    public SkinColorData GetColorData(SkinColorSlot slot) => GetColorData((int)slot);
    private void EnsureColorData()
    {
        if (colors == null) colors = new SkinColorData[MaterialSlotCount];
        if (colors.Length != MaterialSlotCount) System.Array.Resize(ref colors, MaterialSlotCount);
        for (int i = 0; i < colors.Length; i++) if (colors[i] == null) colors[i] = new SkinColorData();
    }
}
