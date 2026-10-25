using UnityEngine;

public static class WardrobePreviewUtility
{
    public static GameObject GetCapybaraPreviewPrefab()
    {
        WardrobePreviewSettings settings = GetSettings();
        if (settings != null && settings.CapybaraPreviewPrefab != null)
            return settings.CapybaraPreviewPrefab;

        return null;
    }

    public static WardrobePreviewSettings GetSettings()
    {
        return Resources.Load<WardrobePreviewSettings>("Wardrobe/WardrobePreviewSettings");
    }
}
