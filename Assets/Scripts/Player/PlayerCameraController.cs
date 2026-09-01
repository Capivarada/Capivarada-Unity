using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachineCamera;
    
    public void SetTarget(Transform target)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError(
                "[Camera] Cinemachine Camera não encontrada."
            );

            return;
        }

        cinemachineCamera.Follow = target;
        cinemachineCamera.LookAt = target;

        cinemachineCamera.Priority = 100;
    }
}