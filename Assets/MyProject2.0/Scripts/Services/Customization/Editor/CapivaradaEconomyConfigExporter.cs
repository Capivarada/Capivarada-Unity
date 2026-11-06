
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CapivaradaEconomyConfigExporter
{
    private const string OutputPath = "Assets/MyProject/CloudScript/CapivaradaEconomyConfig.json";
    // This is the runtime catalogue loaded by WardrobeRuntimeBootstrap. A generic
    // FindAssets query is ambiguous because the project retains the older config copy.
    private const string RuntimeCatalogPath = "Assets/MyProject/Resources/Wardrobe/SkinCatalog.asset";

    [MenuItem("Capivarada/Export PlayFab Economy Config")]
    public static void Export()
    {
        SkinCatalog catalog = FindCatalog();
        if (catalog == null)
            throw new System.InvalidOperationException("SkinCatalog not found.");
        if (!catalog.Validate(out string catalogError))
            throw new System.InvalidOperationException(catalogError);

        List<BoxJson> boxes = new();
        foreach (string guid in AssetDatabase.FindAssets("t:WardrobeBox"))
        {
            WardrobeBox box = AssetDatabase.LoadAssetAtPath<WardrobeBox>(AssetDatabase.GUIDToAssetPath(guid));
            if (box == null || !box.IsAvailable) continue;
            if (!box.Validate(catalog, out string boxError))
                throw new System.InvalidOperationException(boxError);

            BoxJson result = new() { id = box.BoxId, price = box.Price, available = true };
            foreach (RarityDrop drop in box.DropRates)
                if (drop != null && drop.chance > 0f)
                    result.drops.Add(new DropJson
                    {
                        rarity = drop.rarity.ToString(),
                        chance = drop.chance,
                        skinIds = catalog.GetEligibleForBox(drop.rarity).ConvertAll(skin => skin.SkinId)
                    });
            boxes.Add(result);
        }

        File.WriteAllText(OutputPath, JsonUtility.ToJson(new EconomyJson { boxes = boxes, duplicatePoints = new DuplicatePointsJson() }, true));
        AssetDatabase.ImportAsset(OutputPath);
        UnityEngine.Debug.Log($"[Capivarada] PlayFab economy configuration exported: {OutputPath}");
    }

    [MenuItem("Capivarada/Export PlayFab Legacy Skin Catalog")]
    public static void ExportLegacySkinCatalog()
    {
        const string outputPath = "Assets/MyProject/CloudScript/CapivaradaLegacySkinCatalog.json";
        SkinCatalog catalog = FindCatalog();
        if (catalog == null) throw new System.InvalidOperationException("SkinCatalog not found.");
        if (!catalog.Validate(out string error)) throw new System.InvalidOperationException(error);

        LegacyCatalogJson export = new() { CatalogVersion = "CapivaradaSkins", SetAsDefaultCatalog = false, Catalog = new List<LegacyItemJson>() };
        foreach (WardrobeSkin skin in catalog.AllSkins)
        {
            if (skin == null) continue;
            export.Catalog.Add(new LegacyItemJson { ItemId = skin.SkinId, DisplayName = skin.SkinName, Description = "Skin de capivara: " + skin.SkinName, Tags = new List<string> { "skin", skin.Rarity.ToString() }, CanBecomeCharacter = false });
        }
        File.WriteAllText(outputPath, JsonUtility.ToJson(export, true));
        AssetDatabase.ImportAsset(outputPath);
        Debug.Log("[Capivarada] Legacy skin catalog exported: " + outputPath);
    }

    private static SkinCatalog FindCatalog()
    {
        SkinCatalog runtimeCatalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(RuntimeCatalogPath);
        if (runtimeCatalog != null) return runtimeCatalog;

        // Compatibility fallback for projects that have not yet moved the catalog
        // into Resources. Never require there to be exactly one matching asset.
        foreach (string guid in AssetDatabase.FindAssets("t:SkinCatalog"))
        {
            SkinCatalog catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            if (catalog != null) return catalog;
        }
        return null;
    }

    [System.Serializable] private class EconomyJson { public List<BoxJson> boxes = new(); public DuplicatePointsJson duplicatePoints; }
    [System.Serializable] private class LegacyCatalogJson { public string CatalogVersion; public bool SetAsDefaultCatalog; public List<LegacyItemJson> Catalog; }
    [System.Serializable] private class LegacyItemJson { public string ItemId; public string DisplayName; public string Description; public List<string> Tags; public bool CanBecomeCharacter; }
    [System.Serializable] private class BoxJson { public string id; public int price; public bool available; public List<DropJson> drops = new(); }
    [System.Serializable] private class DropJson { public string rarity; public float chance; public List<string> skinIds = new(); }
    [System.Serializable] private class DuplicatePointsJson { public int Comum = 10; public int Incomum = 20; public int Raro = 40; public int Epico = 80; public int Lendario = 160; public int Mitico = 320; }
}
