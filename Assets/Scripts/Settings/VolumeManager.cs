using UnityEngine;
using UnityEngine.Audio;
using System;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance { get; private set; }

    public static event Action OnVolumesChanged;

    const string KEY_MASTER = "audio_master";
    const string KEY_MUSIC = "audio_music";
    const string KEY_SFX = "audio_sfx";

    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    public AudioMixer audioMixer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        Load();
        ApplyVolumes();
    }

    public void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 1f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        Save();
        OnVolumesChanged?.Invoke();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        Save();
        OnVolumesChanged?.Invoke();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        Save();
        OnVolumesChanged?.Invoke();
    }

    void ApplyVolumes()
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat("Master", LinearToDb(masterVolume));
        audioMixer.SetFloat("Music", LinearToDb(musicVolume));
        audioMixer.SetFloat("SFX", LinearToDb(sfxVolume));
    }

    float LinearToDb(float linear)
    {
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        return Mathf.Log10(linear) * 20f;
    }
}