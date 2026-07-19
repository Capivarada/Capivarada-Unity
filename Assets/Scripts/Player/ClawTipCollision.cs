using UnityEngine;

namespace ArenadasAlmas.Player
{
    [RequireComponent(typeof(Collider))]
    public class ClawTipCollision : MonoBehaviour
    {
        [SerializeField] private SoulClawController clawController;

        private void OnEnable()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (clawController == null)
                clawController = GetComponentInParent<SoulClawController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (clawController == null)
                return;

            clawController.OnClawTipTriggered(other);
        }
    }
}
