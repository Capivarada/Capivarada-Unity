using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        WardrobePreviewSettings runtimeSettings = Resources.Load<WardrobePreviewSettings>("Wardrobe/WardrobePreviewSettings");
        if (runtimeSettings != null)
            return runtimeSettings;
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:WardrobePreviewSettings");
        if (guids == null || guids.Length == 0)
            return null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WardrobePreviewSettings settings = AssetDatabase.LoadAssetAtPath<WardrobePreviewSettings>(path);
            if (settings != null)
                return settings;
        }
#endif
        return null;
    }
}
