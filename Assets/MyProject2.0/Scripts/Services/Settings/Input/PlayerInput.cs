using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private int pendingAddChargePresses;
    private int pendingRemoveChargePresses;

    public Vector2 Look => inputActions.Player.Look.ReadValue<Vector2>();

    public bool AddChargePressed =>
        inputActions.Player.AddCharge.WasPressedThisFrame();

    public bool RemoveChargePressed =>
        inputActions.Player.RemoveCharge.WasPressedThisFrame();

    /// <summary>
    /// Fusion ticks do not necessarily align with Unity input frames. These
    /// methods preserve every key-down edge until the local network owner
    /// consumes it during its next simulation tick.
    /// </summary>
    public int ConsumeAddChargePresses()
    {
        int presses = pendingAddChargePresses;
        pendingAddChargePresses = 0;
        return presses;
    }

    public int ConsumeRemoveChargePresses()
    {
        int presses = pendingRemoveChargePresses;
        pendingRemoveChargePresses = 0;
        return presses;
    }

    public void ClearPendingChargePresses()
    {
        pendingAddChargePresses = 0;
        pendingRemoveChargePresses = 0;
    }

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

    private void Update()
    {
        if (inputActions == null) return;
        if (inputActions.Player.AddCharge.WasPressedThisFrame) pendingAddChargePresses++;
        if (inputActions.Player.RemoveCharge.WasPressedThisFrame) pendingRemoveChargePresses++;
    }

    private void OnDisable()
    {
        ClearPendingChargePresses();
        inputActions.Disable();
    }
}
