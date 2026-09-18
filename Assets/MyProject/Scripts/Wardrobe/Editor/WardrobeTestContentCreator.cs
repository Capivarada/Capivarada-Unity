using System.IO;
using UnityEditor;
using UnityEngine;

public static class WardrobeTestContentCreator
{
    private const string RootPath = "Assets/MyProject/Datas/Wardrobe";

    [MenuItem("Capivarada/Wardrobe/Create Test Content")]
    public static void CreateTestContent()
    {
        EnsureFolder("Skins");
        EnsureFolder("Head");
        EnsureFolder("Acessory");
        EnsureFolder("Cloth");
        EnsureFolder("Boxes");
        EnsureFolder("Rarity");
        EnsureFolder("Preview");
        EnsureFolder("Test");

        CreateOrUpdateSkin("Skin_Default", new Color(0.52f, 0.26f, 0.15f), new Color(0.23f, 0.15f, 0.13f), new Color(0.09f, 0.09f, 0.09f), new Color(0.29f, 0.21f, 0.18f), new Color(0.98f, 0.45f, 0.77f), new Color(0.41f, 0.15f, 0.15f), new Color(0.09f, 0.11f, 0.13f));
        CreateOrUpdateSkin("Skin_Green", new Color(0.22f, 0.59f, 0.28f), new Color(0.14f, 0.24f, 0.18f), new Color(0.08f, 0.12f, 0.09f), new Color(0.22f, 0.36f, 0.28f), new Color(0.42f, 0.70f, 0.60f), new Color(0.18f, 0.32f, 0.22f), new Color(0.05f, 0.10f, 0.08f));
        CreateOrUpdateSkin("Skin_Blue", new Color(0.28f, 0.48f, 0.80f), new Color(0.19f, 0.21f, 0.40f), new Color(0.08f, 0.11f, 0.22f), new Color(0.31f, 0.38f, 0.52f), new Color(0.75f, 0.82f, 0.94f), new Color(0.19f, 0.20f, 0.42f), new Color(0.09f, 0.13f, 0.19f));
        CreateOrUpdateSkin("Skin_Purple", new Color(0.62f, 0.45f, 0.90f), new Color(0.28f, 0.20f, 0.38f), new Color(0.12f, 0.11f, 0.17f), new Color(0.33f, 0.28f, 0.46f), new Color(0.90f, 0.88f, 0.97f), new Color(0.27f, 0.16f, 0.39f), new Color(0.12f, 0.10f, 0.18f));

        CreateOrUpdateEquipment<WardrobeHead>("Head_Cowboy", "Head_Cowboy");
        CreateOrUpdateEquipment<WardrobeHead>("Head_Crown", "Head_Crown");
        CreateOrUpdateEquipment<WardrobeHead>("Head_Cap", "Head_Cap");

        CreateOrUpdateEquipment<WardrobeAccessory>("Accessory_Glasses", "Accessory_Glasses");
        CreateOrUpdateEquipment<WardrobeAccessory>("Accessory_Backpack", "Accessory_Backpack");
        CreateOrUpdateEquipment<WardrobeAccessory>("Accessory_Scarf", "Accessory_Scarf");

        CreateOrUpdateEquipment<WardrobeCloth>("Cloth_Shirt", "Cloth_Shirt");
        CreateOrUpdateEquipment<WardrobeCloth>("Cloth_Jersey", "Cloth_Jersey");
        CreateOrUpdateEquipment<WardrobeCloth>("Cloth_Jacket", "Cloth_Jacket");

        CreateOrUpdateBox("Box_Skin", WardrobeContentType.Skin, 100, "Assets/MyProject/Prefabs/Lobby/Wardrobe/ShopBox.prefab");
        CreateOrUpdateBox("Box_Head", WardrobeContentType.Head, 120, "Assets/MyProject/Prefabs/Lobby/Wardrobe/ShopBox.prefab");
        CreateOrUpdateBox("Box_Accessory", WardrobeContentType.Accessory, 130, "Assets/MyProject/Prefabs/Lobby/Wardrobe/ShopBox.prefab");
        CreateOrUpdateBox("Box_Cloth", WardrobeContentType.Cloth, 140, "Assets/MyProject/Prefabs/Lobby/Wardrobe/ShopBox.prefab");

        EnsureRaritySettings();
        EnsurePreviewSettings();
    }

    private static void EnsureFolder(string folderName)
    {
        string path = Path.Combine(RootPath, folderName);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private static void CreateOrUpdateSkin(
        string name,
        Color skin,
        Color tail,
        Color paw,
        Color nose,
        Color ears,
        Color mouth,
        Color eyes)
    {
        string path = Path.Combine(RootPath, "Skins", name + ".asset").Replace('\\', '/');
        WardrobeSkin asset = AssetDatabase.LoadAssetAtPath<WardrobeSkin>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WardrobeSkin>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.name = name;
        asset.SetName(name);
        SetSkinColor(asset, SkinColorSlot.Skin, skin);
        SetSkinColor(asset, SkinColorSlot.Tail, tail);
        SetSkinColor(asset, SkinColorSlot.Paw, paw);
        SetSkinColor(asset, SkinColorSlot.Nose, nose);
        SetSkinColor(asset, SkinColorSlot.Ears, ears);
        SetSkinColor(asset, SkinColorSlot.Mouth, mouth);
        SetSkinColor(asset, SkinColorSlot.Eyes, eyes);

        EditorUtility.SetDirty(asset);
    }

    private static void SetSkinColor(WardrobeSkin skin, SkinColorSlot slot, Color color)
    {
        if (skin == null)
            return;

        SkinColorData data = skin.GetColorData(slot);
        if (data == null)
            return;

        data.color = color;
        data.metallic = 0f;
        data.smoothness = 0.5f;
    }

    private static void CreateOrUpdateEquipment<T>(string assetName, string displayName) where T : WardrobeEquipment
    {
        string folder = GetFolderForEquipment(typeof(T));
        string path = Path.Combine(RootPath, folder, assetName + ".asset").Replace('\\', '/');
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.name = assetName;
        asset.SetName(displayName);
        EditorUtility.SetDirty(asset);
    }

    private static string GetFolderForEquipment(System.Type type)
    {
        if (type == typeof(WardrobeHead))
            return "Heads";
        if (type == typeof(WardrobeAccessory))
            return "Accessories";
        if (type == typeof(WardrobeCloth))
            return "Clothes";

        return "Test";
    }

    private static void CreateOrUpdateBox(string name, WardrobeContentType type, int price, string referenceAsset)
    {
        string path = Path.Combine(RootPath, "Boxes", name + ".asset").Replace('\\', '/');
        WardrobeBox asset = AssetDatabase.LoadAssetAtPath<WardrobeBox>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WardrobeBox>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.name = name;
        asset.SetName(name);
        asset.SetPrice(price);
        asset.SetType(type);

        if (!string.IsNullOrWhiteSpace(referenceAsset))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(referenceAsset);
            asset.SetPrefab(prefab);
        }

        EditorUtility.SetDirty(asset);
    }

    private static void EnsureRaritySettings()
    {
        string path = Path.Combine(RootPath, "Rarity", "Wardrobe_RaritySettings.asset").Replace('\\', '/');
        RaritySettings asset = AssetDatabase.LoadAssetAtPath<RaritySettings>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<RaritySettings>();
            AssetDatabase.CreateAsset(asset, path);
        }

        EditorUtility.SetDirty(asset);
    }

    private static void EnsurePreviewSettings()
    {
        string path = Path.Combine(RootPath, "Preview", "Wardrobe_PreviewSettings.asset").Replace('\\', '/');
        WardrobePreviewSettings asset = AssetDatabase.LoadAssetAtPath<WardrobePreviewSettings>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WardrobePreviewSettings>();
            AssetDatabase.CreateAsset(asset, path);
        }

        EditorUtility.SetDirty(asset);
    }
}

public static class WardrobeAssetExtensions
{
    public static void SetName(this WardrobeContent content, string name)
    {
        if (content == null)
            return;

        content.name = name;
    }

    public static void SetName(this WardrobeBox box, string name)
    {
        if (box == null)
            return;

        box.name = name;
    }

    public static void SetPrice(this WardrobeBox box, int price)
    {
        if (box == null)
            return;

        var field = typeof(WardrobeBox).GetField("price", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(box, price);
    }

    public static void SetType(this WardrobeBox box, WardrobeContentType type)
    {
        if (box == null)
            return;

        var field = typeof(WardrobeBox).GetField("type", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(box, type);
    }

    public static void SetPrefab(this WardrobeBox box, GameObject prefab)
    {
        if (box == null)
            return;

        var field = typeof(WardrobeBox).GetField("prefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(box, prefab);
    }
}
