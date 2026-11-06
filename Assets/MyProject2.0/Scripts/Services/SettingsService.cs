using UnityEngine;

public sealed class SettingsService : MonoBehaviour
{
    public static SettingsService Instance { get; private set; }
    public const string Master = "settings.master", Music = "settings.music", Sfx = "settings.sfx", Voice = "settings.voice", Brightness = "settings.brightness";
    private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) Destroy(this); }
    public float Get(string key, float fallback = 1f) => PlayerPrefs.GetFloat(key, fallback);
    public bool GetBool(string key, bool fallback = false) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
    public void Set(string key, float value) => PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
    public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    public void Save() => PlayerPrefs.Save();
}
