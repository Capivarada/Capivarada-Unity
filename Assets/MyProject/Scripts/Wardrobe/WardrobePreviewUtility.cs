using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class WardrobePreviewUtility
{
    public static GameObject GetPlayerPreviewPrefab()
    {
        if (WardrobeManager.Instance != null && WardrobeManager.Instance.CapybaraPreviewPrefab != null)
            return WardrobeManager.Instance.CapybaraPreviewPrefab;

        WardrobePreviewSettings settings = GetSettings();
        if (settings != null && settings.CapybaraPreviewPrefab != null)
            return settings.CapybaraPreviewPrefab;

        return null;
    }

    public static GameObject GetContentPreviewPrefab(WardrobeContent content)
    {
        if (content == null)
            return null;

        if (content is WardrobeSkin)
            return null;

        GameObject previewPrefab = content.PreviewPrefab;
        if (previewPrefab == null)
            return null;

        GameObject playerPrefab = GetPlayerPreviewPrefab();
        if (playerPrefab != null && previewPrefab == playerPrefab)
        {
            Debug.LogError(
                $"[{nameof(WardrobePreviewUtility)}] {content.name} references the player preview prefab. " +
                "Assign the item's own preview prefab.");
            return null;
        }

        return previewPrefab;
    }

    public static GameObject GetCapybaraPreviewPrefab() => GetPlayerPreviewPrefab();

    public static WardrobePreviewSettings GetSettings()
    {
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
