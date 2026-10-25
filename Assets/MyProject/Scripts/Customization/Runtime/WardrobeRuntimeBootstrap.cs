using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the one persistent skin/inventory service required by Lobby and Game.
/// It deliberately does not edit any scene sprites or UI layout.
/// </summary>
public sealed class WardrobeRuntimeBootstrap : MonoBehaviour
{
    private const string CatalogResourcePath = "Wardrobe/SkinCatalog";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<WardrobeRuntimeBootstrap>() != null) return;
        var root = new GameObject("Capivarada Wardrobe Runtime");
        DontDestroyOnLoad(root);
        root.AddComponent<WardrobeRuntimeBootstrap>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (WardrobeManager.Instance != null)
            WardrobeManager.Instance.OnCustomizationChanged -= HandleCustomizationChanged;
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        EnsureRuntimeManager();
        StartCoroutine(ApplySkinToScenePreview());
    }
    private void HandleCustomizationChanged(PlayerCustomization _) => StartCoroutine(ApplySkinToScenePreview());

    private IEnumerator ApplySkinToScenePreview()
    {
        // Wait until scene objects have completed Awake/Start before locating renderers.
        yield return null;
        WardrobeSkin selected = WardrobeManager.Instance?.CurrentCustomization?.Skin;
        if (selected == null) yield break;

        foreach (SkinnedMeshRenderer renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // Preview3D creates isolated capybara clones for every item card.
            // They already receive their own skin in WardrobeItemCard; applying
            // the equipped skin here overwrote every card with the same color.
            if (IsItemPreviewRenderer(renderer.transform))
                continue;
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length < WardrobeSkin.MaterialSlotCount)
                continue;

            PlayerCustomizationApplier applier = renderer.GetComponent<PlayerCustomizationApplier>();
            if (applier == null) applier = renderer.gameObject.AddComponent<PlayerCustomizationApplier>();
            applier.ApplySkinOnly(selected);
        }
    }

    private static bool IsItemPreviewRenderer(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            // Both the former per-card renderer and the atlas runtime contain
            // skin-specific clones. They must never receive the equipped skin
            // from the lobby's global scene-preview pass.
            if ((current.name == "Preview3D Runtime" ||
                 current.name == "Preview3D Atlas Runtime" ||
                 current.name == "Preview3D Capture Runtime" ||
                 current.name == "Preview3D Preview Factory") &&
                (current.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0)
                return true;
        }
        return false;
    }

    private void EnsureRuntimeManager()
    {
        WardrobeManager manager = WardrobeManager.Instance;
        if (manager != null)
        {
            manager.EnsureInitialized();
            manager.OnCustomizationChanged -= HandleCustomizationChanged;
            manager.OnCustomizationChanged += HandleCustomizationChanged;
            return;
        }

        SkinCatalog catalog = Resources.Load<SkinCatalog>(CatalogResourcePath);
        if (catalog == null)
        {
            Debug.LogError("[Wardrobe] Catálogo não encontrado em Resources/Wardrobe/SkinCatalog.");
            return;
        }
        InventoryManager inventory = GetComponent<InventoryManager>() ?? gameObject.AddComponent<InventoryManager>();
        PlayFabEconomyService economy = GetComponent<PlayFabEconomyService>() ?? gameObject.AddComponent<PlayFabEconomyService>();
        manager = GetComponent<WardrobeManager>() ?? gameObject.AddComponent<WardrobeManager>();
        manager.Configure(catalog, inventory, economy);
        manager.OnCustomizationChanged += HandleCustomizationChanged;
    }
}
