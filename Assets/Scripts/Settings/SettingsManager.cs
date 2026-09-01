using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    [SerializeField] public GameObject panel;
    [SerializeField] public GameObject background;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    [SerializeField] public GameObject backToMenuButton;
    [SerializeField] public GameObject settingsButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
#if UNITY_ANDROID
        SetObjectsActive("Key");
#endif
    }

    void Start()
    {
        if (VolumeManager.Instance != null)
        {
            if (masterSlider != null) masterSlider.value = VolumeManager.Instance.masterVolume;
            if (musicSlider != null) musicSlider.value = VolumeManager.Instance.musicVolume;
            if (sfxSlider != null) sfxSlider.value = VolumeManager.Instance.sfxVolume;
        }
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleSettingsPanel();

        if ((SceneManager.GetActiveScene().buildIndex == 0))
        {
            backToMenuButton.SetActive(false);
            settingsButton.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            backToMenuButton.SetActive(true);
            settingsButton.SetActive(true);
        }
        
#if UNITY_ANDROID
        SetObjectsActive("Key");
#endif
    }
    
    public void SetObjectsActive(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }
    }

    void OnMasterChanged(float v)
    {
        if (VolumeManager.Instance != null) VolumeManager.Instance.SetMasterVolume(v);
    }

    void OnMusicChanged(float v)
    {
        if (VolumeManager.Instance != null) VolumeManager.Instance.SetMusicVolume(v);
    }

    void OnSfxChanged(float v)
    {
        if (VolumeManager.Instance != null) VolumeManager.Instance.SetSFXVolume(v);
    }

    public void ToggleSettingsPanel()
    {
        panel.SetActive(!panel.activeSelf);
        background.SetActive(!background.activeSelf);
        if (panel.activeSelf)
            Cursor.lockState = CursorLockMode.None;
        else if (SceneManager.GetActiveScene().buildIndex == 1 || SceneManager.GetActiveScene().buildIndex == 2)
            Cursor.lockState = CursorLockMode.Locked;
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("0.Menu");
        ToggleSettingsPanel();
    }
}