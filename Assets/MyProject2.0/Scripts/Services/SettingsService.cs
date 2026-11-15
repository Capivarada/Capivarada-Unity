using UnityEngine;
using UnityEngine.Audio;

/// <summary>Persists and applies runtime display, graphics and mixer settings.</summary>
public sealed class SettingsService : MonoBehaviour
{
    public static SettingsService Instance { get; private set; }
    public const string Master = "settings.master", Music = "settings.music", Sfx = "settings.sfx", Voice = "settings.voice", MuteAll = "settings.muteAll", Brightness = "settings.brightness";
    public const string ResolutionIndex = "settings.screen.resolution", DisplayMode = "settings.screen.mode", VSync = "settings.screen.vsync";
    public const string Quality = "settings.graphics.quality", Shadows = "settings.graphics.shadows", Effects = "settings.graphics.effects", FpsLimit = "settings.graphics.fps";
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParameter = "MasterVolume";
    [SerializeField] private string musicParameter = "MusicVolume";
    [SerializeField] private string sfxParameter = "SFXVolume";
    [Tooltip("Optional. Leave blank until the mixer has a voice group exposed.")]
    [SerializeField] private string voiceParameter = "";
    public event System.Action<float> BrightnessChanged;
    private float baseLodBias;
    private float baseShadowDistance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        baseLodBias = Mathf.Max(.01f, QualitySettings.lodBias);
        baseShadowDistance = Mathf.Max(1f, QualitySettings.shadowDistance);
        ApplyDisplayAndGraphics();
        ApplyAudio();
    }

    public float Get(string key, float fallback = 1f) => PlayerPrefs.GetFloat(key, fallback);
    public int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(key, fallback);
    public bool GetBool(string key, bool fallback = false) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
    public void Set(string key, float value) => PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    public void Save() => PlayerPrefs.Save();

    public void ApplyDisplayAndGraphics()
    {
        ApplyQuality(GetInt(Quality, QualitySettings.GetQualityLevel()));
        ApplyShadows(GetBool(Shadows, true));
        ApplyEffects(GetInt(Effects, 1));
        ApplyFrameRate(GetInt(FpsLimit, 60), GetBool(VSync, true));
        ApplyResolution(GetInt(ResolutionIndex, -1), GetInt(DisplayMode, 0));
    }

    public void ApplyAudio()
    {
        float master = GetBool(MuteAll) ? 0f : Get(Master);
        SetMixer(masterParameter, master);
        SetMixer(musicParameter, Get(Music));
        SetMixer(sfxParameter, Get(Sfx));
        SetMixer(voiceParameter, Get(Voice));
    }

    public void ApplyBrightness(float value)
    {
        BrightnessChanged?.Invoke(Mathf.Clamp01(value));
    }

    public void ApplyQuality(int qualityIndex)
    {
        if (QualitySettings.names.Length == 0) return;
        QualitySettings.SetQualityLevel(Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1), true);
    }

    public void ApplyShadows(bool enabled) => QualitySettings.shadows = enabled ? ShadowQuality.All : ShadowQuality.Disable;

    /// <summary>
    /// Applies the UI's Low / Medium / High effects choice to engine settings
    /// which are meaningful in every current arena.  Individual VFX can later
    /// subscribe to this value, but this already changes LOD, shadow range and
    /// skinning quality instead of being a cosmetic PlayerPrefs-only option.
    /// </summary>
    public void ApplyEffects(int value)
    {
        int effects = Mathf.Clamp(value, 0, 2);
        float multiplier = effects == 0 ? .55f : effects == 1 ? .78f : 1f;
        QualitySettings.lodBias = baseLodBias * multiplier;
        QualitySettings.shadowDistance = baseShadowDistance * multiplier;
        // Reducing skin weights deforms some of the imported capybara meshes.
        // Keep the original skinning fidelity and lower only the effect settings
        // that do not change the model's silhouette.
        QualitySettings.skinWeights = SkinWeights.FourBones;
    }

    public void ApplyFrameRate(int target, bool useVSync)
    {
        QualitySettings.vSyncCount = useVSync ? 1 : 0;
        Application.targetFrameRate = useVSync ? -1 : Mathf.Max(30, target);
    }

    public void ApplyResolution(int resolutionIndex, int displayMode)
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0 || resolutionIndex < 0 || resolutionIndex >= resolutions.Length) return;
        Resolution option = resolutions[resolutionIndex];
        FullScreenMode mode = displayMode switch
        {
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.Windowed,
            _ => FullScreenMode.ExclusiveFullScreen
        };
        Screen.SetResolution(option.width, option.height, mode, option.refreshRateRatio);
    }

    private void SetMixer(string parameter, float linear)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameter)) return;
        audioMixer.SetFloat(parameter, LinearToDecibels(linear));
    }

    private static float LinearToDecibels(float linear) => Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
}
