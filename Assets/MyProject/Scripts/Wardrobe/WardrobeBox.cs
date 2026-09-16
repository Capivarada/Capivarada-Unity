using System;
using System.Collections.Generic;
using UnityEngine;

public enum WardrobeContentType
{
    Skin,
    Head,
    Cloth,
    Accessory
}

[Serializable]
public class RarityDrop
{
    public Rarity rarity;

    [Min(0f)]
    public float chance;

    public List<WardrobeContent> items = new();
}

[CreateAssetMenu(
    fileName = "New Wardrobe Box",
    menuName = "Capivarada/Shop/Wardrobe Box"
)]
public class WardrobeBox : ScriptableObject
{
    [SerializeField] private string boxId;
    [SerializeField] private string boxName;
    [SerializeField] private WardrobeContentType type;
    [SerializeField] private int price;
    [SerializeField] private GameObject prefab;
    [SerializeField] private List<RarityDrop> dropRates = new();

    public string BoxId => boxId;
    public string BoxName => string.IsNullOrWhiteSpace(boxName) ? name : boxName;
    public WardrobeContentType Type => type;
    public int Price => price;
    public GameObject Prefab => prefab;
    public IReadOnlyList<RarityDrop> DropRates => dropRates;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(boxId))
        {
            boxId = UnityEditor.AssetDatabase.AssetPathToGUID(
                UnityEditor.AssetDatabase.GetAssetPath(this));
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
