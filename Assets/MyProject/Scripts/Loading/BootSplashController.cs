using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the optional intro video, then reveals the game logo before Auth.</summary>
public sealed class BootSplashController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoSurface;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private RectTransform logoTransform;
    [SerializeField, Tooltip("Nome do MP4 dentro de Assets/StreamingAssets, usado no build WebGL.")]
    private string webGlVideoFileName = "Intro.mp4";
    [SerializeField, Min(0.01f)] private float logoRevealDuration = 1.25f;
    [SerializeField] private float logoStartScale = 0.75f;
    [SerializeField] private float logoEndScale = 1.08f;
    [SerializeField] private string authSceneName = "Auth";

    private bool revealStarted;
    private RenderTexture runtimeVideoTexture;

    private void Awake()
    {
        if (videoPlayer != null) videoPlayer.playOnAwake = false;
        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;
        if (logoTransform != null) logoTransform.localScale = Vector3.one * logoStartScale;
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            BeginLogoReveal();
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(webGlVideoFileName))
        {
            Debug.LogWarning("[Boot] Defina o nome do vídeo em StreamingAssets para o WebGL.");
            BeginLogoReveal();
            return;
        }
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Path.Combine(Application.streamingAssetsPath, webGlVideoFileName).Replace("\\", "/");
        // AudioSource output is not fully supported by WebGL VideoPlayer.
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
#else
        if (videoPlayer.clip == null)
        {
            BeginLogoReveal();
            return;
        }
        videoPlayer.source = VideoSource.VideoClip;
#endif

        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived += HandleVideoError;
        videoPlayer.prepareCompleted += HandleVideoPrepared;
        if (videoImage != null)
        {
            runtimeVideoTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            runtimeVideoTexture.Create();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = runtimeVideoTexture;
            videoImage.texture = runtimeVideoTexture;
        }
        if (videoSurface != null) videoSurface.SetActive(true);
        videoPlayer.Prepare();
    }

    private void HandleVideoPrepared(VideoPlayer player) => player.Play();
    private void HandleVideoFinished(VideoPlayer _) => BeginLogoReveal();
    private void HandleVideoError(VideoPlayer _, string message)
    {
        Debug.LogWarning("[Boot] Não foi possível tocar a introdução: " + message);
        BeginLogoReveal();
    }

    private void BeginLogoReveal()
    {
        if (revealStarted) return;
        revealStarted = true;
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            // The controller and VideoPlayer live on the same object in the
            // generated scene; disabling the GameObject would stop this fade.
            videoPlayer.enabled = false;
        }
        if (videoSurface != null) videoSurface.SetActive(false);
        if (videoImage != null) videoImage.texture = null;
        StartCoroutine(RevealAndLoadAuth());
    }

    private IEnumerator RevealAndLoadAuth()
    {
        float elapsed = 0f;
        while (elapsed < logoRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / logoRevealDuration);
            float eased = progress * progress * (3f - 2f * progress);
            if (logoCanvasGroup != null) logoCanvasGroup.alpha = eased;
            if (logoTransform != null)
                logoTransform.localScale = Vector3.one * Mathf.Lerp(logoStartScale, logoEndScale, eased);
            yield return null;
        }
        SceneManager.LoadSceneAsync(authSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer == null) return;
        videoPlayer.loopPointReached -= HandleVideoFinished;
        videoPlayer.errorReceived -= HandleVideoError;
        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        if (runtimeVideoTexture != null) { runtimeVideoTexture.Release(); Destroy(runtimeVideoTexture); }
    }

#if UNITY_EDITOR
    public void Configure(VideoPlayer player, GameObject surface, RawImage image, CanvasGroup logoGroup, RectTransform logo)
    {
        videoPlayer = player;
        videoSurface = surface;
        videoImage = image;
        logoCanvasGroup = logoGroup;
        logoTransform = logo;
    }
#endif
}
