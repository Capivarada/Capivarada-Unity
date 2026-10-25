using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Creates the project-owned Boot scene without changing the supplied art.</summary>
public static class CapivaradaBootSceneCreator
{
    private const string ScenePath = "Assets/MyProject/Scenes/Boot.unity";
    private const string LogoPath = "Assets/MyProject/Sprites/Lobby/Logo.png";
    [MenuItem("Capivarada/Configurar Splash da Cena Boot")]
    public static void CreateBootScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Capivarada] A cena Boot não possui Canvas para receber a splash.");
            return;
        }
        Transform existing = canvas.transform.Find("BootSplashOverlay");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = new("BootSplashOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());

        Image background = CreateImage(overlay.transform, "Background", null);
        Stretch(background.rectTransform); background.color = new Color(.025f, .035f, .06f, 1f);

        RawImage videoSurface = CreateRawImage(overlay.transform, "IntroVideo", null);
        Stretch(videoSurface.rectTransform);

        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        Image logo = CreateImage(overlay.transform, "Logo", logoSprite);
        logo.preserveAspect = true;
        RectTransform logoRect = logo.rectTransform;
        logoRect.anchorMin = logoRect.anchorMax = new Vector2(.5f, .5f);
        logoRect.sizeDelta = new Vector2(700, 350);
        logoRect.anchoredPosition = Vector2.zero;
        CanvasGroup logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

        GameObject controllerObject = new("BootSplashController", typeof(VideoPlayer), typeof(AudioSource), typeof(BootSplashController));
        VideoPlayer player = controllerObject.GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.SetTargetAudioSource(0, controllerObject.GetComponent<AudioSource>());
        controllerObject.GetComponent<BootSplashController>().Configure(player, videoSurface.gameObject, videoSurface, logoGroup, logoRect);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddBootToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Capivarada] Splash adicionada à cena Boot. Atribua o VideoClip no BootSplashController.");
    }

    private static void AddBootToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(entry => entry.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Image CreateImage(Transform parent, string objectName, Sprite sprite)
    {
        GameObject target = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(parent, false);
        Image image = target.GetComponent<Image>(); image.sprite = sprite;
        return image;
    }
    private static RawImage CreateRawImage(Transform parent, string objectName, Texture texture)
    {
        GameObject target = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        target.transform.SetParent(parent, false);
        RawImage image = target.GetComponent<RawImage>(); image.texture = texture;
        return image;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}
