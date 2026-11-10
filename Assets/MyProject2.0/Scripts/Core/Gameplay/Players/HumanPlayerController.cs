using UnityEngine;

public class HumanPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraTransform;

    private RoundManager roundManager;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput =
                GetComponent<PlayerInput>();
        }

        if (playerController == null)
        {
            playerController =
                GetComponent<PlayerController>();
        }
    }

    private void Start()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                cameraTransform =
                    mainCamera.transform;
            }
        }

        roundManager =
            FindObjectOfType<RoundManager>();
    }

    private void Update()
    {
        if (!CanChoose())
            return;

        HandleAim();
        HandleCharge();
    }

    private bool CanChoose()
    {
        if (roundManager == null)
            return false;

        return
            roundManager.CurrentState ==
            RoundState.Choosing;
    }

    private void HandleAim()
    {
        if (cameraTransform == null)
            return;

        Vector3 direction =
            cameraTransform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        playerController.SetAimDirection(
            direction
        );

        // Practice has no Fusion bridge to present this private selection.
        // Turn only the local presentation until directions are revealed.
        playerController.PreviewLocalAimRotation();
    }

    private void HandleCharge()
    {
        if (playerInput.AddChargePressed)
        {
            playerController.IncreaseCharge();
        }

        if (playerInput.RemoveChargePressed)
        {
            playerController.DecreaseCharge();
        }

        if (playerController.CurrentCharge > 0f)
            playerController.ShowIndicator();
        else
            playerController.HideIndicator();
    }
}
