using UnityEngine;

public abstract class WardrobeContent : ScriptableObject
{
    [SerializeField] private string contentId;
    [SerializeField] private string contentName;
    [SerializeField] private Rarity rarity;

    public string ContentId => contentId;

    public string ContentName =>
        string.IsNullOrWhiteSpace(contentName)
            ? name
            : contentName;

    public Rarity ContentRarity => rarity;



#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureStableId();
    }

    protected void EnsureStableId()
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            contentId =
                UnityEditor.AssetDatabase.AssetPathToGUID(
                    UnityEditor.AssetDatabase.GetAssetPath(this)
                );

            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

public abstract class WardrobeEquipment : WardrobeContent
{
    [SerializeField] private GameObject prefab;
    public GameObject Prefab => prefab;
}

[CreateAssetMenu(
    fileName = "New Wardrobe Head",
    menuName = "Capivarada/Wardrobe/Head"
)]
public class WardrobeHead : WardrobeEquipment
{
}

[CreateAssetMenu(
    fileName = "New Wardrobe Cloth",
    menuName = "Capivarada/Wardrobe/Cloth"
)]
public class WardrobeCloth : WardrobeEquipment
{
}

[CreateAssetMenu(
    fileName = "New Wardrobe Accessory",
    menuName = "Capivarada/Wardrobe/Accessory"
)]
public class WardrobeAccessory : WardrobeEquipment
{
}
