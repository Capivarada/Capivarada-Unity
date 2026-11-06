using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachineCamera;
    private Transform gameplayTarget;
    
    public void SetTarget(Transform target)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError(
                "[Camera] Cinemachine Camera não encontrada."
            );

            return;
        }

        gameplayTarget = target;
        cinemachineCamera.Follow = gameplayTarget;
        cinemachineCamera.LookAt = gameplayTarget;

        cinemachineCamera.Priority = 100;
    }

    /// <summary>Temporarily takes the player camera over for the pre-match flyover.</summary>
    public void SetCinematicPose(Vector3 position, Quaternion rotation)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError("[Camera] Cinemachine Camera não encontrada.");
            return;
        }

        cinemachineCamera.transform.SetPositionAndRotation(position, rotation);
        cinemachineCamera.Follow = null;
        cinemachineCamera.LookAt = null;
        cinemachineCamera.Priority = 200;
    }

    /// <summary>Returns camera control to the local player's CameraTarget.</summary>
    public void EndCinematic()
    {
        if (cinemachineCamera == null) return;
        cinemachineCamera.Follow = gameplayTarget;
        cinemachineCamera.LookAt = gameplayTarget;
        cinemachineCamera.Priority = 100;
    }
}
