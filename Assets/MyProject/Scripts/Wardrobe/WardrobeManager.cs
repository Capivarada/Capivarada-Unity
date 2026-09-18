using System;
using System.Collections.Generic;
using UnityEngine;

public class WardrobeManager : MonoBehaviour
{
    public static WardrobeManager Instance { get; private set; }

    [Header("Configured content")]
    [SerializeField] private GameObject capybaraPreviewPrefab;
    [SerializeField] private WardrobeSkin defaultSkin;
    [SerializeField] private List<WardrobeSkin> availableSkins = new();
    [SerializeField] private List<WardrobeHead> availableHeads = new();
    [SerializeField] private List<WardrobeAccessory> availableAccessories = new();
    [SerializeField] private List<WardrobeCloth> availableClothes = new();

    [Header("Owned content")]
    [SerializeField] private List<WardrobeSkin> ownedSkins = new();
    [SerializeField] private List<WardrobeHead> ownedHeads = new();
    [SerializeField] private List<WardrobeAccessory> ownedAccessories = new();
    [SerializeField] private List<WardrobeCloth> ownedClothes = new();

    [Header("Bot content")]
    [SerializeField] private List<WardrobeSkin> botSkins = new();
    [SerializeField] private List<WardrobeHead> botHeads = new();
    [SerializeField] private List<WardrobeAccessory> botAccessories = new();
    [SerializeField] private List<WardrobeCloth> botClothes = new();

    [Header("Inventory")]
    [SerializeField] private InventoryManager inventoryManager;

    public event Action<PlayerCustomization> OnCustomizationChanged;
    public PlayerCustomization CurrentCustomization { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentCustomization = new PlayerCustomization
        {
            Skin = defaultSkin != null ? defaultSkin : First(GetOwnedSkins()),
            Head = First(GetOwnedHeads()),
            Accessory = First(GetOwnedAccessories()),
            Clothing = First(GetOwnedClothes())
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (availableSkins.Count == 0)
            AddAssets(availableSkins, "t:WardrobeSkin");
        if (availableHeads.Count == 0)
            AddAssets(availableHeads, "t:WardrobeHead");
        if (availableAccessories.Count == 0)
            AddAssets(availableAccessories, "t:WardrobeAccessory");
        if (availableClothes.Count == 0)
            AddAssets(availableClothes, "t:WardrobeCloth");

        if (defaultSkin == null)
            defaultSkin = FindNamed(availableSkins, "Skin Default");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static void AddAssets<T>(List<T> target, string filter) where T : UnityEngine.Object
    {
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(filter))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && !target.Contains(asset))
                target.Add(asset);
        }
    }

    private static T FindNamed<T>(IReadOnlyList<T> values, string assetName) where T : UnityEngine.Object
    {
        foreach (T value in values)
            if (value != null && value.name == assetName) return value;
        return null;
    }
#endif

    public GameObject CapybaraPreviewPrefab => capybaraPreviewPrefab;
    public WardrobeSkin DefaultSkin => defaultSkin;

    public IReadOnlyList<WardrobeSkin> GetOwnedSkins() => GetOwned(inventoryManager, ownedSkins, availableSkins);
    public IReadOnlyList<WardrobeHead> GetOwnedHeads() => GetOwned(inventoryManager, ownedHeads, availableHeads);
    public IReadOnlyList<WardrobeAccessory> GetOwnedAccessories() => GetOwned(inventoryManager, ownedAccessories, availableAccessories);
    public IReadOnlyList<WardrobeCloth> GetOwnedClothes() => GetOwned(inventoryManager, ownedClothes, availableClothes);
    public IReadOnlyList<WardrobeSkin> GetConfiguredSkins() => availableSkins;

    public bool IsDefaultSkin(WardrobeSkin skin) => defaultSkin != null && defaultSkin == skin;

    public IEnumerable<WardrobeContent> GetItems(WardrobeCategory category)
    {
        switch (category)
        {
            case WardrobeCategory.Cabeca:
                foreach (WardrobeHead item in GetOwnedHeads()) yield return item;
                break;
            case WardrobeCategory.Acessorios:
                foreach (WardrobeAccessory item in GetOwnedAccessories()) yield return item;
                break;
            case WardrobeCategory.Roupas:
                foreach (WardrobeCloth item in GetOwnedClothes()) yield return item;
                break;
            case WardrobeCategory.Todos:
                foreach (WardrobeContent item in GetAllOwnedContent()) yield return item;
                break;
        }
    }

    public IEnumerable<WardrobeContent> GetAllOwnedContent()
    {
        foreach (WardrobeSkin item in GetOwnedSkins())
            if (item != null) yield return item;
        foreach (WardrobeHead item in GetOwnedHeads())
            if (item != null) yield return item;
        foreach (WardrobeAccessory item in GetOwnedAccessories())
            if (item != null) yield return item;
        foreach (WardrobeCloth item in GetOwnedClothes())
            if (item != null) yield return item;
    }

    public void EquipSkin(WardrobeSkin skin)
    {
        if (skin == null)
            skin = defaultSkin;

        if (skin == null || !Contains(GetOwnedSkins(), skin)) return;
        CurrentCustomization.Skin = skin;
        NotifyCustomizationChanged();
    }

    public void EquipDefaultSkin()
    {
        if (defaultSkin == null)
            return;

        EquipSkin(defaultSkin);
    }

    public void EquipItem(WardrobeContent item)
    {
        switch (item)
        {
            case WardrobeHead head when Contains(GetOwnedHeads(), head):
                CurrentCustomization.Head = head;
                break;
            case WardrobeAccessory accessory when Contains(GetOwnedAccessories(), accessory):
                CurrentCustomization.Accessory = accessory;
                break;
            case WardrobeCloth cloth when Contains(GetOwnedClothes(), cloth):
                CurrentCustomization.Clothing = cloth;
                break;
            default:
                return;
        }

        NotifyCustomizationChanged();
    }

    public void Unequip(WardrobeCategory category)
    {
        switch (category)
        {
            case WardrobeCategory.Cores:
                if (defaultSkin != null)
                    CurrentCustomization.Skin = defaultSkin;
                break;
            case WardrobeCategory.Cabeca: CurrentCustomization.Head = null; break;
            case WardrobeCategory.Acessorios: CurrentCustomization.Accessory = null; break;
            case WardrobeCategory.Roupas: CurrentCustomization.Clothing = null; break;
            default: return;
        }

        NotifyCustomizationChanged();
    }

    public PlayerCustomization CreateRandomBotCustomization()
    {
        IReadOnlyList<WardrobeSkin> skins = botSkins.Count > 0 ? botSkins : GetOwnedSkins();
        IReadOnlyList<WardrobeHead> heads = botHeads.Count > 0 ? botHeads : GetOwnedHeads();
        IReadOnlyList<WardrobeAccessory> accessories = botAccessories.Count > 0 ? botAccessories : GetOwnedAccessories();
        IReadOnlyList<WardrobeCloth> clothes = botClothes.Count > 0 ? botClothes : GetOwnedClothes();

        return new PlayerCustomization
        {
            Skin = Pick(skins, CurrentCustomization.Skin),
            Head = PickOptional(heads),
            Accessory = PickOptional(accessories),
            Clothing = PickOptional(clothes)
        };
    }

    private void NotifyCustomizationChanged() => OnCustomizationChanged?.Invoke(CurrentCustomization);

    private static IReadOnlyList<T> UseOwned<T>(List<T> owned, List<T> available) =>
        owned.Count > 0 ? owned : available;

    private static IReadOnlyList<T> GetOwned<T>(
        InventoryManager inventory,
        List<T> owned,
        List<T> available) where T : WardrobeContent
    {
        if (inventory != null)
        {
            IReadOnlyList<T> inventoryItems = inventory.GetOwned<T>();
            if (inventoryItems.Count > 0)
                return inventoryItems;
        }

        return UseOwned(owned, available);
    }

    private static bool Contains<T>(IReadOnlyList<T> values, T value)
    {
        for (int i = 0; i < values.Count; i++)
            if (EqualityComparer<T>.Default.Equals(values[i], value)) return true;
        return false;
    }

    private static T First<T>(IReadOnlyList<T> values) where T : class
    {
        foreach (T value in values)
            if (value != null) return value;
        return null;
    }

    private static T Pick<T>(IReadOnlyList<T> values, T fallback) where T : class
    {
        List<T> valid = new();
        foreach (T value in values)
            if (value != null) valid.Add(value);
        return valid.Count == 0 ? fallback : valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private static T PickOptional<T>(IReadOnlyList<T> values) where T : class
    {
        return UnityEngine.Random.value < 0.25f ? null : Pick(values, null);
    }
}
