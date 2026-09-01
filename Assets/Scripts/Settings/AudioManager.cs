using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")] public AudioClip backgroundMusic;
    public float musicVolume = 0.6f;
    private AudioSource musicSource;

    [Header("SFX")] public AudioClip clickClip;
    public float sfxVolume = 1f;
    private AudioSource sfxSource;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
            PlayOneShot(clickClip);
    }

    void Start()
    {
        VolumeManager.OnVolumesChanged += ApplySettingsFromSettingsManager;
        ApplySettingsFromSettingsManager();

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    void OnDestroy()
    {
        VolumeManager.OnVolumesChanged -= ApplySettingsFromSettingsManager;
    }

    void ApplySettingsFromSettingsManager()
    {
        float master = 1f, music = musicVolume, sfx = sfxVolume;
        if (VolumeManager.Instance != null)
        {
            master = VolumeManager.Instance.masterVolume;
            music = VolumeManager.Instance.musicVolume;
            sfx = VolumeManager.Instance.sfxVolume;
        }

        if (musicSource != null) musicSource.volume = master * music;
        if (sfxSource != null) sfxSource.volume = master * sfx;
    }

    public void TogglePlayMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
        else
            musicSource.Play();
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }
}