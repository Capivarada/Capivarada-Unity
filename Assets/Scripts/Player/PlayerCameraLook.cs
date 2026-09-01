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

    private void Awake()
    {
        if (playerInput == null)
            playerInput =
                GetComponentInParent<PlayerInput>();
    }

    private void Update()
    {
        if (playerInput == null)
            return;

        Vector2 look =
            playerInput.Look;

        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;

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