#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Exports current ScriptableObjects as a reviewable V2 catalog contract. It never changes catalog assets or scenes.</summary>
public static class CapivaradaV2CatalogExporter
{
    [MenuItem("Capivarada/Economy V2/Export Catalog Contract")]
    private static void Export()
    {
        SkinCatalog catalog = FindSingle<SkinCatalog>();
        if (catalog == null) { EditorUtility.DisplayDialog("Economy V2", "SkinCatalog não encontrado.", "OK"); return; }
        if (!catalog.Validate(out string catalogError)) { EditorUtility.DisplayDialog("Economy V2", catalogError, "OK"); return; }
        List<WardrobeBox> boxes = FindAll<WardrobeBox>();
        foreach (WardrobeBox box in boxes) if (!box.Validate(catalog, out string error)) { EditorUtility.DisplayDialog("Economy V2", error, "OK"); return; }

        CatalogContract contract = new() { currency = new CurrencyContract { id = "PA", displayName = "Pontos de Batalha" } };
        foreach (WardrobeSkin skin in catalog.AllSkins)
            if (skin != null) contract.skins.Add(new SkinContract { id = skin.SkinId, displayName = skin.SkinName, rarity = skin.Rarity.ToString(), isDefault = skin == catalog.DefaultSkin });
        foreach (WardrobeBox box in boxes)
        {
            if (box == null || !box.IsAvailable) continue;
            BoxContract destination = new() { id = box.BoxId, displayName = box.BoxName, visualRarity = box.VisualRarity.ToString(), pricePA = box.Price, duplicatePoints = DuplicatePoints(box.VisualRarity) };
            foreach (RarityDrop rate in box.DropRates)
            {
                List<WardrobeSkin> eligible = catalog.GetEligibleForBox(rate.rarity);
                if (eligible.Count == 0 || rate.chance <= 0f) continue;
                int eachWeight = Mathf.Max(1, Mathf.RoundToInt(rate.chance * 1000f / eligible.Count));
                foreach (WardrobeSkin skin in eligible) destination.drops.Add(new DropContract { skinId = skin.SkinId, rarity = skin.Rarity.ToString(), weight = eachWeight });
            }
            contract.boxes.Add(destination);
        }
        string path = EditorUtility.SaveFilePanel("Exportar contrato Economy V2", Application.dataPath, "CapivaradaV2CatalogContract", "json");
        if (string.IsNullOrWhiteSpace(path)) return;
        // This contract is sent to an HTTP JSON endpoint. Make the file encoding explicit so
        // names such as "Baú" and "Épico" are never exported using the Windows ANSI code page.
        File.WriteAllText(path, JsonUtility.ToJson(contract, true), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        EditorUtility.RevealInFinder(path);
    }
    private static int DuplicatePoints(Rarity rarity) => rarity switch { Rarity.Incomum => 20, Rarity.Raro => 40, Rarity.Epico => 80, Rarity.Lendario => 160, Rarity.Mitico => 320, _ => 10 };
    private static T FindSingle<T>() where T : UnityEngine.Object { string[] ids = AssetDatabase.FindAssets("t:" + typeof(T).Name); return ids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(ids[0])); }
    private static List<T> FindAll<T>() where T : UnityEngine.Object { List<T> result = new(); foreach (string id in AssetDatabase.FindAssets("t:" + typeof(T).Name)) { T item = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(id)); if (item != null) result.Add(item); } return result; }
    [Serializable] private sealed class CatalogContract { public CurrencyContract currency; public List<SkinContract> skins = new(); public List<BoxContract> boxes = new(); }
    [Serializable] private sealed class CurrencyContract { public string id; public string displayName; }
    [Serializable] private sealed class SkinContract { public string id; public string displayName; public string rarity; public bool isDefault; }
    [Serializable] private sealed class BoxContract { public string id; public string displayName; public string visualRarity; public int pricePA; public int duplicatePoints; public List<DropContract> drops = new(); }
    [Serializable] private sealed class DropContract { public string skinId; public string rarity; public int weight; }
}
#endif
