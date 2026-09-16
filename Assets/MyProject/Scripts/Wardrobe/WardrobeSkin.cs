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

[CreateAssetMenu(
    fileName = "New Capybara Skin",
    menuName = "Capivarada/Wardrobe/Skin"
)]
public class WardrobeSkin : ScriptableObject
{
    public const int MaterialSlotCount = 7;

    [SerializeField] private string skinId;
    [SerializeField] private string skinName;
    [SerializeField] private string rarity;
    [SerializeField] private SkinColorData[] colors = new SkinColorData[MaterialSlotCount];

    public string SkinId => skinId;
    public string SkinName => skinName;
    public string Rarity => rarity;
    public SkinColorData[] Colors => colors;

    private void OnEnable()
    {
        EnsureColorData();
    }

    private void OnValidate()
    {
        EnsureColorData();
    }

    public SkinColorData GetColorData(int index)
    {
        EnsureColorData();
        return colors[index];
    }

    private void EnsureColorData()
    {
        if (colors == null)
            colors = new SkinColorData[MaterialSlotCount];

        if (colors.Length != MaterialSlotCount)
            System.Array.Resize(ref colors, MaterialSlotCount);

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == null)
                colors[i] = new SkinColorData();
        }
    }
}
