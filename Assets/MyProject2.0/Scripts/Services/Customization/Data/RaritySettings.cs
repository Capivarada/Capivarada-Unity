using System;
using UnityEngine;

public enum Rarity
{
    Comum,
    Incomum,
    Raro,
    Epico,
    Lendario,
    Mitico
}

[Serializable]
public class RarityVisualData
{
    public Rarity rarity;
    public Color color = Color.white;
}

[CreateAssetMenu(
    fileName = "Wardrobe Rarity Settings",
    menuName = "Capivarada/Wardrobe/Rarity Settings"
)]
public class RaritySettings : ScriptableObject
{
    [SerializeField] private RarityVisualData[] values =
    {
        new RarityVisualData { rarity = Rarity.Comum, color = new Color(0.72f, 0.72f, 0.72f) },
        new RarityVisualData { rarity = Rarity.Incomum, color = new Color(0.25f, 0.75f, 0.35f) },
        new RarityVisualData { rarity = Rarity.Raro, color = new Color(0.2f, 0.45f, 1f) },
        new RarityVisualData { rarity = Rarity.Epico, color = new Color(0.65f, 0.3f, 0.95f) },
        new RarityVisualData { rarity = Rarity.Lendario, color = new Color(1f, 0.62f, 0.1f) },
        new RarityVisualData { rarity = Rarity.Mitico, color = new Color(1f, 0.2f, 0.35f) }
    };

    public RarityVisualData[] Values => values;

    public Color GetColor(Rarity rarity)
    {
        if (values != null)
        {
            foreach (RarityVisualData value in values)
            {
                if (value != null && value.rarity == rarity)
                    return value.color;
            }
        }

        return Color.white;
    }
}
