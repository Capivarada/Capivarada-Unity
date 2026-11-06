#if UNITY_EDITOR
using System.Collections.Generic;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit, one-time authoring commands for the Boot, Loading, Lobby and Game scenes.
/// It never renames existing objects and only removes inaccessible panels from generated
/// guest LobbyUI copies. Run individual menu commands when reviewing scene changes.
/// </summary>
public static class CapivaradaSceneInstaller
{
    private const string ScenesRoot = "Assets/MyProject2.0/Scenes/";
    private const string BootPath = ScenesRoot + "Boot_Scene.unity";
    private const string LoadingPath = ScenesRoot + "Loading_Scene.unity";
    private const string LobbyPath = ScenesRoot + "Lobby_Scene.unity";
    private const string GamePath = ScenesRoot + "Game_Scene.unity";

    [MenuItem("Capivarada/Scenes/Install all multiplayer scene foundations")]
    public static void InstallAll()
    {
        InstallBootAndLoading();
        InstallGame();
        InstallLobbyVariants();
        Debug.Log("[CapivaradaSceneInstaller] Scene foundations installed. Review each scene and assign optional visual references.");
    }

    [MenuItem("Capivarada/Scenes/Install Boot and Loading")]
    public static void InstallBootAndLoading()
    {
        Scene boot = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
        GameObject services = Find("Services");
        if (services == null)
        {
            Debug.LogError("[CapivaradaSceneInstaller] Boot_Scene precisa conter o GameObject Services.");
            return;
        }

        // Boot owns services and its own controller. Loading must remain a lightweight view.
        LoadingController legacyLoading = Object.FindFirstObjectByType<LoadingController>(FindObjectsInactive.Include);
        if (legacyLoading != null) Object.DestroyImmediate(legacyLoading);
        BootController bootController = FindOrAdd<BootController>(services);
        FusionSessionService fusion = FindOrAdd<FusionSessionService>(services);
        AssignRunnerPrefab(fusion);
        MarkAndSave(boot);

        Scene loading = EditorSceneManager.OpenScene(LoadingPath, OpenSceneMode.Single);
        GameObject loadingServices = Find("Services");
        if (loadingServices != null) Object.DestroyImmediate(loadingServices);
        if (Object.FindFirstObjectByType<LoadingController>(FindObjectsInactive.Include) == null)
        {
            GameObject root = Find("LoadingUI") ?? new GameObject("LoadingUI");
            root.AddComponent<LoadingController>();
        }
        MarkAndSave(loading);
        Debug.Log($"[CapivaradaSceneInstaller] BootController added to {bootController.gameObject.name}; Loading_Scene keeps only loading UI and LoadingController.");
    }

    [MenuItem("Capivarada/Scenes/Install Game Scene Fusion foundation")]
    public static void InstallGame()
    {
        Scene game = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
        GameObject gameManager = Find("GameManager");
        if (gameManager == null)
        {
            Debug.LogError("[CapivaradaSceneInstaller] Game_Scene precisa conter GameManager.");
            return;
        }

        FindOrAdd<NetworkObject>(gameManager);
        FindOrAdd<FusionArenaMatchController>(gameManager);
        FindOrAdd<SceneReadinessController>(gameManager);
        MarkAndSave(game);
        Debug.Log("[CapivaradaSceneInstaller] FusionArenaMatchController and NetworkObject added to GameManager. Configure this scene in Fusion's NetworkProjectConfig before playtesting.");
    }

    [MenuItem("Capivarada/Scenes/Create Lobby variants")]
    public static void InstallLobbyVariants()
    {
        Scene lobby = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
        GameObject source = Find("LobbyUI");
        if (source == null)
        {
            Debug.LogError("[CapivaradaSceneInstaller] Lobby_Scene precisa conter LobbyUI.");
            return;
        }

        GameObject authenticated = source;
        GameObject guestOnline = CopyLobbyVariant(source, "LobbyUI_GuestOnline", new[] { "ShopUI", "WardrobeUI", "ProfileUI", "FriendsUI", "FriendsPanel" });
        GameObject guestOffline = CopyLobbyVariant(source, "LobbyUI_GuestOffline", new[] { "ShopUI", "LeaderboardUI", "ProfileUI", "OtherProfileUI", "FriendsUI", "FriendsPanel", "CreditsUI" });
        GameObject coordinator = Find("LobbyPresentation") ?? new GameObject("LobbyPresentation");
        LobbyPresentationController presentation = FindOrAdd<LobbyPresentationController>(coordinator);
        Assign(presentation, "authenticatedOnlineRoot", authenticated);
        Assign(presentation, "guestOnlineRoot", guestOnline);
        Assign(presentation, "guestOfflineRoot", guestOffline);
        MarkAndSave(lobby);
        Debug.Log("[CapivaradaSceneInstaller] Created/updated LobbyUI_GuestOnline and LobbyUI_GuestOffline. Existing LobbyUI remains the authenticated root.");
    }

    private static GameObject CopyLobbyVariant(GameObject source, string name, IEnumerable<string> panelNames)
    {
        GameObject current = Find(name);
        if (current != null) Object.DestroyImmediate(current);
        GameObject copy = Object.Instantiate(source, source.transform.parent);
        copy.name = name;
        copy.SetActive(false);
        foreach (string panelName in panelNames) DestroyDescendantsNamed(copy.transform, panelName);
        return copy;
    }

    private static void DestroyDescendantsNamed(Transform root, string objectName)
    {
        List<GameObject> targets = new();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child != root && child.name == objectName) targets.Add(child.gameObject);
        foreach (GameObject target in targets) Object.DestroyImmediate(target);
    }

    private static void AssignRunnerPrefab(FusionSessionService fusion)
    {
        string[] guids = AssetDatabase.FindAssets("t:prefab NetworkRunner", new[] { "Assets/MyProject2.0/Prefabs/Photon" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[CapivaradaSceneInstaller] Prefab NetworkRunner não encontrado em Assets/MyProject2.0/Prefabs/Photon. Arraste-o manualmente no FusionSessionService.");
            return;
        }
        NetworkRunner runner = AssetDatabase.LoadAssetAtPath<NetworkRunner>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (runner == null)
        {
            Debug.LogWarning("[CapivaradaSceneInstaller] O prefab encontrado não possui NetworkRunner.");
            return;
        }
        Assign(fusion, "runnerPrefab", runner);
    }

    private static T FindOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject Find(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == name) return candidate.gameObject;
        }
        return null;
    }

    private static void Assign(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) { Debug.LogWarning($"[CapivaradaSceneInstaller] Campo {propertyName} não foi encontrado em {target.GetType().Name}."); return; }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MarkAndSave(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
