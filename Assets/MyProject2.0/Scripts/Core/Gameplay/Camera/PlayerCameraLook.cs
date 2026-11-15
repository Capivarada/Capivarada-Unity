using UnityEngine;

public class PlayerCameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Target")]
    [SerializeField] private Transform cameraTarget;

    [Header("Sensitivity")]
    [SerializeField] private float sensitivity = 0.15f;

    [Header("Vertical Limits")]
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch;
    private SettingsService settings;

    /// <summary>The actual aim pivot used by the local input, independent of Cinemachine output timing.</summary>
    public Vector3 AimForward => cameraTarget != null ? cameraTarget.forward : transform.forward;

    private void Awake()
    {
        if (playerInput == null)
            playerInput =
                GetComponentInParent<PlayerInput>();
        settings = SettingsService.Instance;
    }

    private void Update()
    {
        if (playerInput == null)
            return;

        Vector2 look =
            playerInput.Look;

        if (settings == null) settings = SettingsService.Instance;
        float horizontal = settings != null ? settings.Get("settings.sensitivity.horizontal", .5f) : .5f;
        float vertical = settings != null ? settings.Get("settings.sensitivity.vertical", .5f) : .5f;
        bool invertHorizontal = settings != null && settings.GetBool("settings.invert.horizontal");
        bool invertVertical = settings != null && settings.GetBool("settings.invert.vertical");
        float horizontalScale = Mathf.Lerp(.25f, 2f, Mathf.Clamp01(horizontal));
        float verticalScale = Mathf.Lerp(.25f, 2f, Mathf.Clamp01(vertical));

        yaw += look.x * sensitivity * horizontalScale * (invertHorizontal ? -1f : 1f);
        pitch -= look.y * sensitivity * verticalScale * (invertVertical ? -1f : 1f);

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );

        cameraTarget.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }
}
