using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private List<WardrobeSkin> ownedSkins = new();

    public IReadOnlyList<WardrobeSkin> OwnedSkins => ownedSkins;

    public bool Contains(WardrobeSkin skin)
    {
        return skin != null && ownedSkins.Contains(skin);
    }

    public bool Grant(WardrobeSkin skin)
    {
        if (skin == null || Contains(skin))
            return false;

        ownedSkins.Add(skin);
        return true;
    }

    /// <summary>Updates the runtime projection after a server-confirmed inventory response.</summary>
    public void ReplaceOwnedSkins(IReadOnlyList<WardrobeSkin> skins, WardrobeSkin defaultSkin)
    {
        ownedSkins.Clear();
        if (defaultSkin != null)
            ownedSkins.Add(defaultSkin);

        if (skins == null)
            return;

        foreach (WardrobeSkin skin in skins)
            if (skin != null && !ownedSkins.Contains(skin))
                ownedSkins.Add(skin);
    }
}
