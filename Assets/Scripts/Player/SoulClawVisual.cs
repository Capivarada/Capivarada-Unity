using UnityEngine;

namespace ArenadasAlmas.Player
{
    public class SoulClawVisual : MonoBehaviour
    {
        [SerializeField] private Transform cylinderBody;
        [SerializeField] private Transform clawTip;

        private Vector3 baseCylinderScale = Vector3.one;

        private void Awake()
        {
            if (cylinderBody != null)
                baseCylinderScale = cylinderBody.localScale;
        }

        public void UpdateVisuals(Vector3 startPoint, Vector3 endPoint)
        {
            if (cylinderBody == null || clawTip == null)
                return;

            Vector3 direction = endPoint - startPoint;
            float distance = direction.magnitude;
            if (distance <= 0.0001f) distance = 0.0001f;

            // Position the root at midpoint
            transform.position = (startPoint + endPoint) * 0.5f;

            // Rotate so local Y aligns with direction
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            transform.rotation = rotation;

            // Adjust scale on Y axis. Unity cylinder default height is 2 units.
            Vector3 newScale = baseCylinderScale;
            newScale.y = (distance * 0.5f) / (baseCylinderScale.y != 0f ? baseCylinderScale.y : 1f);
            cylinderBody.localScale = newScale;

            // Ensure the claw tip is exactly at the end point
            clawTip.position = endPoint;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
