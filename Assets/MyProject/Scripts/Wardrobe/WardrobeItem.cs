    using UnityEngine;

    [CreateAssetMenu(
        fileName = "New Wardrobe Item",
        menuName = "Capivarada/Wardrobe/Item"
    )]
    public class WardrobeItem : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private WardrobeCategory category;
        [SerializeField] private string rarity;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private GameObject equippedPrefab;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public WardrobeCategory Category => category;
        public string Rarity => rarity;
        public GameObject PreviewPrefab => previewPrefab;
        public GameObject EquippedPrefab => equippedPrefab;

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = UnityEditor.AssetDatabase.AssetPathToGUID(
                    UnityEditor.AssetDatabase.GetAssetPath(this));
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    #endif
    }
