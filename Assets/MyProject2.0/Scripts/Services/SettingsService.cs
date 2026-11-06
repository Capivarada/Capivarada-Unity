using UnityEngine;

/// <summary>Persists and applies runtime settings. Audio routing remains UI-facing until AudioMixer groups are wired.</summary>
public sealed class SettingsService : MonoBehaviour
{
    public static SettingsService Instance { get; private set; }
    public const string Master = "settings.master", Music = "settings.music", Sfx = "settings.sfx", Voice = "settings.voice", Brightness = "settings.brightness";
    public const string ResolutionIndex = "settings.screen.resolution", DisplayMode = "settings.screen.mode", VSync = "settings.screen.vsync";
    public const string Quality = "settings.graphics.quality", Shadows = "settings.graphics.shadows", Effects = "settings.graphics.effects", FpsLimit = "settings.graphics.fps";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        ApplyDisplayAndGraphics();
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
        ApplyFrameRate(GetInt(FpsLimit, 60), GetBool(VSync, true));
        ApplyResolution(GetInt(ResolutionIndex, -1), GetInt(DisplayMode, 0));
    }

    public void ApplyQuality(int qualityIndex)
    {
        if (QualitySettings.names.Length == 0) return;
        QualitySettings.SetQualityLevel(Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1), true);
    }

    public void ApplyShadows(bool enabled) => QualitySettings.shadows = enabled ? ShadowQuality.All : ShadowQuality.Disable;

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
}
