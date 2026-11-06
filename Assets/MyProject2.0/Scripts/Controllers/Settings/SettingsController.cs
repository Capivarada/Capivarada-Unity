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
    [Header("Sensitivity")] [SerializeField] private Slider horizontal, vertical; [SerializeField] private Toggle invertHorizontal, invertVertical; [SerializeField] private TMP_Text horizontalValue, verticalValue; [SerializeField] private string trainingScene = "Training_Scene";
    [Header("Account")] [SerializeField] private TMP_Text emailText; [SerializeField] private Toggle publicProfile, receiveInvites; [SerializeField] private PlayFabAuthManager authentication;
    private void Awake() { if (settings == null) settings = ServiceHub.Instance != null ? ServiceHub.Instance.Settings : SettingsService.Instance; if (authentication == null) authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : PlayFabAuthManager.Instance; foreach (Slider slider in new[] { master, music, sfx, voice, brightness, horizontal, vertical }) if (slider != null) slider.onValueChanged.AddListener(_ => RefreshLabels()); }
    private void OnEnable() { Load(); }
    public void OpenPanel(string id) => panelNavigator?.Show(id);
    public void SaveAudio() { settings.Set(SettingsService.Master, muteAll != null && muteAll.isOn ? 0 : master.value); settings.Set(SettingsService.Music, music.value); settings.Set(SettingsService.Sfx, sfx.value); settings.Set(SettingsService.Voice, voice.value); settings.Save(); }
    public void SaveScreen() { settings.Set(SettingsService.Brightness, brightness.value); QualitySettings.vSyncCount = vSync != null && vSync.isOn ? 1 : 0; if (quality != null) QualitySettings.SetQualityLevel(quality.value); if (resolution != null && resolution.value < Screen.resolutions.Length) { Resolution option = Screen.resolutions[resolution.value]; FullScreenMode mode = displayMode != null && displayMode.value == 2 ? FullScreenMode.Windowed : displayMode != null && displayMode.value == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen; Screen.SetResolution(option.width, option.height, mode); } settings.Save(); }
    public void SaveGraphics() { if (quality != null) QualitySettings.SetQualityLevel(quality.value); if (shadows != null) QualitySettings.shadows = shadows.isOn ? ShadowQuality.All : ShadowQuality.Disable; if (fpsLimit != null) { int[] options = { 240, 120, 60, 30 }; Application.targetFrameRate = options[Mathf.Clamp(fpsLimit.value, 0, options.Length - 1)]; } settings.Save(); }
    public void SaveControls() => settings.Save();
    public void SaveSensitivity() { settings.Set("settings.sensitivity.horizontal", horizontal.value); settings.Set("settings.sensitivity.vertical", vertical.value); settings.SetBool("settings.invert.horizontal", invertHorizontal.isOn); settings.SetBool("settings.invert.vertical", invertVertical.isOn); settings.Save(); }
    public void SaveAccount() { settings.SetBool("settings.public", publicProfile.isOn); settings.SetBool("settings.invites", receiveInvites.isOn); settings.Save(); }
    public void TestTraining() { if (!string.IsNullOrWhiteSpace(trainingScene)) SceneManager.LoadScene(trainingScene); }
    public void RefreshLabels() { SetPercent(masterValue, master); SetPercent(musicValue, music); SetPercent(sfxValue, sfx); SetPercent(voiceValue, voice); SetPercent(brightnessValue, brightness); SetPercent(horizontalValue, horizontal); SetPercent(verticalValue, vertical); }
    private void Load() { if (settings == null) return; Set(master, settings.Get(SettingsService.Master)); Set(music, settings.Get(SettingsService.Music)); Set(sfx, settings.Get(SettingsService.Sfx)); Set(voice, settings.Get(SettingsService.Voice)); Set(brightness, settings.Get(SettingsService.Brightness)); Set(horizontal, settings.Get("settings.sensitivity.horizontal", .5f)); Set(vertical, settings.Get("settings.sensitivity.vertical", .5f)); if (invertHorizontal != null) invertHorizontal.isOn = settings.GetBool("settings.invert.horizontal"); if (invertVertical != null) invertVertical.isOn = settings.GetBool("settings.invert.vertical"); if (publicProfile != null) publicProfile.isOn = settings.GetBool("settings.public", true); if (receiveInvites != null) receiveInvites.isOn = settings.GetBool("settings.invites", true); if (emailText != null) emailText.text = ""; RefreshLabels(); }
    private static void Set(Slider slider, float value) { if (slider != null) slider.value = value; } private static void SetPercent(TMP_Text label, Slider slider) { if (label != null && slider != null) label.text = $"{slider.value * 100f:0}%"; }
}
