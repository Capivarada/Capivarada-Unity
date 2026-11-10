using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SettingsController : MonoBehaviour
{
    [SerializeField] private SettingsService settings;
    [Header("Panels")] [SerializeField] private PanelNavigatorController panelNavigator;
    [Header("Audio")] [SerializeField] private Slider master, music, sfx, voice; [SerializeField] private Toggle muteAll; [SerializeField] private TMP_Text masterValue, musicValue, sfxValue, voiceValue;
    [Header("Screen")] [SerializeField] private TMP_Dropdown resolution, displayMode; [SerializeField] private Toggle vSync; [SerializeField] private Slider brightness; [SerializeField] private TMP_Text brightnessValue;
    [Header("Graphics")] [SerializeField] private TMP_Dropdown quality, effects, fpsLimit; [SerializeField] private Toggle shadows;
    [Header("Sensitivity")] [SerializeField] private Slider horizontal, vertical; [SerializeField] private Toggle invertHorizontal, invertVertical; [SerializeField] private TMP_Text horizontalValue, verticalValue; [SerializeField] private string trainingScene = "Pratice_Scene";
    [Header("Account")] [SerializeField] private TMP_Text emailText; [SerializeField] private Toggle publicProfile, receiveInvites; [SerializeField] private PlayFabAuthManager authentication;

    private void Awake()
    {
        if (settings == null) settings = ServiceHub.Instance != null ? ServiceHub.Instance.Settings : SettingsService.Instance;
        if (authentication == null) authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : PlayFabAuthManager.Instance;
        foreach (Slider slider in new[] { master, music, sfx, voice, brightness, horizontal, vertical }) if (slider != null) slider.onValueChanged.AddListener(_ => RefreshLabels());
        if (brightness != null) brightness.onValueChanged.AddListener(value => settings?.ApplyBrightness(value));
    }

    private void OnEnable() => Load();
    public void OpenPanel(string id) => panelNavigator?.Show(id);
    public void SaveAudio()
    {
        if (settings == null) return;
        settings.Set(SettingsService.Master, Value(master)); settings.Set(SettingsService.Music, Value(music)); settings.Set(SettingsService.Sfx, Value(sfx)); settings.Set(SettingsService.Voice, Value(voice)); settings.SetBool(SettingsService.MuteAll, muteAll != null && muteAll.isOn); settings.ApplyAudio(); settings.Save();
    }
    public void SaveScreen()
    {
        if (settings == null) return;
        settings.Set(SettingsService.Brightness, Value(brightness)); settings.ApplyBrightness(Value(brightness)); settings.SetInt(SettingsService.ResolutionIndex, resolution != null ? resolution.value : -1); settings.SetInt(SettingsService.DisplayMode, displayMode != null ? displayMode.value : 0); settings.SetBool(SettingsService.VSync, vSync != null && vSync.isOn);
        settings.ApplyResolution(settings.GetInt(SettingsService.ResolutionIndex, -1), settings.GetInt(SettingsService.DisplayMode)); settings.ApplyFrameRate(settings.GetInt(SettingsService.FpsLimit, 60), settings.GetBool(SettingsService.VSync)); settings.Save();
    }
    public void SaveGraphics()
    {
        if (settings == null) return;
        int[] options = { 240, 120, 60, 30 }; int fps = options[Mathf.Clamp(fpsLimit != null ? fpsLimit.value : 2, 0, options.Length - 1)]; int qualityIndex = quality != null ? quality.value : QualitySettings.GetQualityLevel(); bool shadowsEnabled = shadows == null || shadows.isOn;
        settings.SetInt(SettingsService.Quality, qualityIndex); settings.SetBool(SettingsService.Shadows, shadowsEnabled); settings.SetInt(SettingsService.Effects, effects != null ? effects.value : 0); settings.SetInt(SettingsService.FpsLimit, fps);
        settings.ApplyQuality(qualityIndex); settings.ApplyShadows(shadowsEnabled); settings.ApplyEffects(settings.GetInt(SettingsService.Effects)); settings.ApplyFrameRate(fps, settings.GetBool(SettingsService.VSync)); settings.Save();
    }
    public void SaveControls() => settings?.Save();
    public void SaveSensitivity()
    {
        if (settings == null) return;
        settings.Set("settings.sensitivity.horizontal", Value(horizontal)); settings.Set("settings.sensitivity.vertical", Value(vertical)); settings.SetBool("settings.invert.horizontal", invertHorizontal != null && invertHorizontal.isOn); settings.SetBool("settings.invert.vertical", invertVertical != null && invertVertical.isOn); settings.Save();
    }
    public void SaveAccount()
    {
        if (settings == null) return;
        settings.SetBool("settings.public", publicProfile != null && publicProfile.isOn); settings.SetBool("settings.invites", receiveInvites != null && receiveInvites.isOn); settings.Save();
    }
    public void TestTraining() { if (!string.IsNullOrWhiteSpace(trainingScene)) SceneManager.LoadScene(trainingScene); }
    public void RefreshLabels() { SetPercent(masterValue, master); SetPercent(musicValue, music); SetPercent(sfxValue, sfx); SetPercent(voiceValue, voice); SetPercent(brightnessValue, brightness); SetPercent(horizontalValue, horizontal); SetPercent(verticalValue, vertical); }
    private void Load()
    {
        if (settings == null) return;
        PopulateResolutions(); Set(master, settings.Get(SettingsService.Master)); Set(music, settings.Get(SettingsService.Music)); Set(sfx, settings.Get(SettingsService.Sfx)); Set(voice, settings.Get(SettingsService.Voice)); Set(brightness, settings.Get(SettingsService.Brightness)); Set(horizontal, settings.Get("settings.sensitivity.horizontal", .5f)); Set(vertical, settings.Get("settings.sensitivity.vertical", .5f));
        if (displayMode != null) displayMode.SetValueWithoutNotify(settings.GetInt(SettingsService.DisplayMode)); if (vSync != null) vSync.SetIsOnWithoutNotify(settings.GetBool(SettingsService.VSync, true)); if (quality != null) quality.SetValueWithoutNotify(Mathf.Clamp(settings.GetInt(SettingsService.Quality, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, quality.options.Count - 1))); if (shadows != null) shadows.SetIsOnWithoutNotify(settings.GetBool(SettingsService.Shadows, true)); if (effects != null) effects.SetValueWithoutNotify(settings.GetInt(SettingsService.Effects)); if (fpsLimit != null) fpsLimit.SetValueWithoutNotify(IndexForFrameRate(settings.GetInt(SettingsService.FpsLimit, 60)));
        if (muteAll != null) muteAll.SetIsOnWithoutNotify(settings.GetBool(SettingsService.MuteAll)); if (invertHorizontal != null) invertHorizontal.SetIsOnWithoutNotify(settings.GetBool("settings.invert.horizontal")); if (invertVertical != null) invertVertical.SetIsOnWithoutNotify(settings.GetBool("settings.invert.vertical")); if (publicProfile != null) publicProfile.SetIsOnWithoutNotify(settings.GetBool("settings.public", true)); if (receiveInvites != null) receiveInvites.SetIsOnWithoutNotify(settings.GetBool("settings.invites", true)); if (emailText != null) emailText.text = authentication != null ? authentication.Email : string.Empty; settings.ApplyAudio(); settings.ApplyBrightness(settings.Get(SettingsService.Brightness)); RefreshLabels();
    }
    private void PopulateResolutions()
    {
        if (resolution == null) return;
        Resolution[] options = Screen.resolutions; resolution.ClearOptions(); var labels = new System.Collections.Generic.List<string>(); for (int index = 0; index < options.Length; index++) labels.Add($"{options[index].width} × {options[index].height}"); resolution.AddOptions(labels); int saved = settings != null ? settings.GetInt(SettingsService.ResolutionIndex, -1) : -1; resolution.SetValueWithoutNotify(saved >= 0 && saved < options.Length ? saved : FindCurrentResolution(options));
    }
    private static int FindCurrentResolution(Resolution[] options) { for (int index = 0; index < options.Length; index++) if (options[index].width == Screen.width && options[index].height == Screen.height) return index; return 0; }
    private static int IndexForFrameRate(int target) => target >= 240 ? 0 : target >= 120 ? 1 : target >= 60 ? 2 : 3;
    private static float Value(Slider slider) => slider != null ? slider.value : 1f;
    private static void Set(Slider slider, float value) { if (slider != null) slider.SetValueWithoutNotify(value); }
    private static void SetPercent(TMP_Text label, Slider slider) { if (label != null && slider != null) label.text = $"{slider.value * 100f:0}%"; }
}
