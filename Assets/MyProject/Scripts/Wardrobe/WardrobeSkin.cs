using UnityEngine;

[System.Serializable]
public class SkinColorData
{
    public Color color = Color.white;

    [Range(0f, 1f)]
    public float metallic;

    [Range(0f, 1f)]
    public float smoothness = 0.5f;
}

public enum SkinColorSlot
{
    Skin,
    Tail,
    Paw,
    Nose,
    Ears,
    Mouth,
    Eyes
}

[CreateAssetMenu(
    fileName = "New Capybara Skin",
    menuName = "Capivarada/Wardrobe/Skin"
)]
public class WardrobeSkin : WardrobeContent
{
    public const int MaterialSlotCount = 7;

    [SerializeField]
    private SkinColorData[] colors = new SkinColorData[MaterialSlotCount];

    public string SkinId => ContentId;
    public string SkinName => ContentName;
    public Rarity Rarity => ContentRarity;
    public SkinColorData[] Colors => colors;

    private void OnEnable()
    {
        EnsureColorData();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureColorData();
    }
#endif

    public SkinColorData GetColorData(int index)
    {
        EnsureColorData();

        if (index < 0 || index >= colors.Length)
            return null;

        return colors[index];
    }

    public SkinColorData GetColorData(SkinColorSlot slot)
    {
        return GetColorData((int)slot);
    }

    private void EnsureColorData()
    {
        if (colors == null)
            colors = new SkinColorData[MaterialSlotCount];

        if (colors.Length != MaterialSlotCount)
            System.Array.Resize(
                ref colors,
                MaterialSlotCount
            );

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == null)
                colors[i] = new SkinColorData();
        }
    }
}