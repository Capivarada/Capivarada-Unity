using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkinCatalog", menuName = "Capivarada/Customization/Skin Catalog")]
public sealed class SkinCatalog : ScriptableObject
{
    [SerializeField] private WardrobeSkin defaultSkin;
    [SerializeField] private List<WardrobeSkin> skins = new();
    public WardrobeSkin DefaultSkin => defaultSkin;
    public IReadOnlyList<WardrobeSkin> AllSkins => skins;
    public WardrobeSkin FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        for (int i = 0; i < skins.Count; i++) if (skins[i] != null && string.Equals(skins[i].SkinId, id, StringComparison.Ordinal)) return skins[i];
        return null;
    }
    public List<WardrobeSkin> GetByRarity(Rarity rarity, bool includeDefault = true)
    {
        List<WardrobeSkin> result = new();
        for (int i = 0; i < skins.Count; i++) { WardrobeSkin skin = skins[i]; if (skin != null && skin.Rarity == rarity && (includeDefault || skin != defaultSkin)) result.Add(skin); }
        return result;
    }
    public List<WardrobeSkin> GetEligibleForBox(Rarity rarity) => GetByRarity(rarity, false);
    public List<WardrobeSkin> ResolveOwned(IReadOnlyList<string> ids)
    {
        List<WardrobeSkin> result = new(); if (defaultSkin != null) result.Add(defaultSkin); if (ids == null) return result;
        for (int i = 0; i < ids.Count; i++) { WardrobeSkin skin = FindById(ids[i]); if (skin != null && !result.Contains(skin)) result.Add(skin); }
        return result;
    }
    public bool Validate(out string error)
    {
        if (defaultSkin == null) { error = "SkinCatalog sem skin padrão."; return false; }
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < skins.Count; i++) { WardrobeSkin skin = skins[i]; if (skin == null) { error = $"Referência nula no índice {i}."; return false; } if (string.IsNullOrWhiteSpace(skin.SkinId)) { error = $"Skin sem ID: {skin.name}."; return false; } if (!ids.Add(skin.SkinId)) { error = $"ID duplicado: {skin.SkinId}."; return false; } }
        if (!skins.Contains(defaultSkin)) { error = "A skin padrão não está no catálogo."; return false; }
        error = null; return true;
    }
}
