using UnityEngine;
using UnityEngine.Audio;
using System;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance { get; private set; }

    public static event Action OnVolumesChanged;

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
        
        ApplyVolumes();
    }

    public void Load()
    {
        // Volume settings intentionally live only for this execution.
    }

    public void Save()
    {
        // Kept as an API-compatible no-op: the project has no local persistence.
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
