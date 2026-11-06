using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns a full-screen black overlay into a real brightness control. Attach it
/// to the overlay Image in each persistent canvas; it has no input blocking.
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class BrightnessOverlayController : MonoBehaviour
{
    [SerializeField, Range(0f, .9f)] private float darkestOverlayAlpha = .65f;
    private Image overlay;
    private SettingsService settings;

    private void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        settings = ServiceHub.Instance != null ? ServiceHub.Instance.Settings : SettingsService.Instance;
    }

    private void OnEnable()
    {
        if (settings == null) settings = ServiceHub.Instance != null ? ServiceHub.Instance.Settings : SettingsService.Instance;
        if (settings != null)
        {
            settings.BrightnessChanged += Apply;
            Apply(settings.Get(SettingsService.Brightness));
        }
    }

    private void OnDisable()
    {
        if (settings != null) settings.BrightnessChanged -= Apply;
    }

    private void Apply(float brightness)
    {
        if (overlay == null) return;
        Color color = overlay.color;
        color.a = Mathf.Lerp(darkestOverlayAlpha, 0f, Mathf.Clamp01(brightness));
        overlay.color = color;
    }
}
