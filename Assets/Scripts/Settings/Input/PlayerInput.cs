using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputActions inputActions;

    public Vector2 Look => inputActions.Player.Look.ReadValue<Vector2>();

    public bool AddChargePressed =>
        inputActions.Player.AddCharge.WasPressedThisFrame();

    public bool RemoveChargePressed =>
        inputActions.Player.RemoveCharge.WasPressedThisFrame();

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}