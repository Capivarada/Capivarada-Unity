using UnityEngine;

public abstract class WardrobeContent : ScriptableObject
{
    [SerializeField] private string contentId;
    [SerializeField] private string contentName;
    [SerializeField] private Rarity rarity;
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private GameObject equippedPrefab;

    public string ContentId => contentId;
    public string ContentName => string.IsNullOrWhiteSpace(contentName) ? name : contentName;
    public Rarity ContentRarity => rarity;
    public GameObject PreviewPrefab => previewPrefab;
    public GameObject EquippedPrefab => equippedPrefab;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        EnsureStableId();
    }

    protected void EnsureStableId()
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            contentId = UnityEditor.AssetDatabase.AssetPathToGUID(
                UnityEditor.AssetDatabase.GetAssetPath(this));
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

public abstract class WardrobeEquipment : WardrobeContent
{
}

[CreateAssetMenu(fileName = "New Wardrobe Head", menuName = "Capivarada/Wardrobe/Head")]
public class WardrobeHead : WardrobeEquipment
{
}

[CreateAssetMenu(fileName = "New Wardrobe Cloth", menuName = "Capivarada/Wardrobe/Cloth")]
public class WardrobeCloth : WardrobeEquipment
{
}

[CreateAssetMenu(fileName = "New Wardrobe Accessory", menuName = "Capivarada/Wardrobe/Accessory")]
public class WardrobeAccessory : WardrobeEquipment
{
}
