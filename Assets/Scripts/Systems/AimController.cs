using UnityEngine;

namespace ArenadasAlmas.Systems
{
    public class AimController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float raycastDistance = 100f;

        private void OnEnable()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        public bool GetAimPoint(out Vector3 aimPoint)
        {
            aimPoint = Vector3.zero;

            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(new Vector3(
                mainCamera.pixelWidth / 2f,
                mainCamera.pixelHeight / 2f,
                0f
            ));

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                aimPoint = hit.point;
                return true;
            }

            aimPoint = ray.origin + ray.direction * raycastDistance;
            return false;
        }

        public Vector3 GetAimDirection()
        {
            if (mainCamera == null)
                return transform.forward;

            Ray ray = mainCamera.ScreenPointToRay(new Vector3(
                mainCamera.pixelWidth / 2f,
                mainCamera.pixelHeight / 2f,
                0f
            ));

            return ray.direction.normalized;
        }
    }
}
