using UnityEngine;
using ArenadasAlmas.Enemy;

namespace ArenadasAlmas.Player
{
    public class SoulClawController : MonoBehaviour
    {
        [SerializeField] private SoulClawVisual clawVisual;
        [SerializeField] private float travelSpeed = 30f;
        [SerializeField] private float extractionDuration = 2f;

        private Vector3 startPoint;
        private Vector3 targetPoint;
        private float travelDistance;
        private float traveledDistance;
        private float extractionTimer;
        private EnemyDummy targetEnemy;
        private bool isExtracting;
        private bool isReturning;
        private PlayerStats playerStats;

        public delegate void ClawEventHandler();
        public event ClawEventHandler OnClawHit;
        public event ClawEventHandler OnClawReturned;

        private void OnEnable()
        {
            if (clawVisual == null)
                clawVisual = GetComponent<SoulClawVisual>();
            
            playerStats = GetComponentInParent<PlayerStats>();
        }

        private void Update()
        {
            if (isExtracting)
                UpdateExtraction();
            else if (isReturning)
                UpdateReturn();
            else
                UpdateTravel();

            if (clawVisual != null)
                clawVisual.UpdateVisuals(startPoint, transform.position);
        }

        public void FireClaw(Vector3 origin, Vector3 target)
        {
            startPoint = origin;
            targetPoint = target;
            traveledDistance = 0f;
            travelDistance = Vector3.Distance(origin, target);
            isExtracting = false;
            isReturning = false;
            transform.position = origin;
            targetEnemy = null;

            if (clawVisual != null)
                clawVisual.SetActive(true);
        }

        private void UpdateTravel()
        {
            if (travelDistance <= 0.01f)
            {
                isReturning = true;
                return;
            }

            float moveDistance = travelSpeed * Time.deltaTime;
            traveledDistance += moveDistance;

            Vector3 direction = (targetPoint - startPoint).normalized;
            transform.position = startPoint + direction * Mathf.Min(traveledDistance, travelDistance);

            // NOTE: collision detection is performed by ClawTip trigger collider.

            if (traveledDistance >= travelDistance && !isExtracting)
            {
                isReturning = true;
            }
        }

        private void HitEnemy(EnemyDummy enemy, Vector3 hitPoint)
        {
            targetEnemy = enemy;
            isExtracting = true;
            extractionTimer = 0f;
            // lock claw to enemy's transform position
            targetPoint = targetEnemy.transform.position;

            targetEnemy.StartExtraction();
            OnClawHit?.Invoke();
        }

        // Called by ClawTipCollision when its trigger hits something
        public void OnClawTipTriggered(Collider other)
        {
            if (isExtracting)
                return;

            EnemyDummy enemy = other.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                // Confirm hit only via collider
                HitEnemy(enemy, other.ClosestPoint(transform.position));
            }
        }

        private void UpdateExtraction()
        {
            if (targetEnemy == null)
            {
                isExtracting = false;
                isReturning = true;
                return;
            }

            extractionTimer += Time.deltaTime;
            transform.position = targetEnemy.transform.position;

            if (extractionTimer >= extractionDuration)
            {
                targetEnemy.RemoveSoul();
                isExtracting = false;
                isReturning = true;
            }
        }

        private void UpdateReturn()
        {
            float moveDistance = travelSpeed * Time.deltaTime;
            Vector3 direction = (startPoint - transform.position).normalized;
            float distanceToStart = Vector3.Distance(transform.position, startPoint);

            if (distanceToStart <= moveDistance)
            {
                transform.position = startPoint;
                isReturning = false;
                targetEnemy = null;

                if (clawVisual != null)
                    clawVisual.SetActive(false);

                OnClawReturned?.Invoke();
            }
            else
            {
                transform.position += direction * moveDistance;
            }
        }

        public bool IsActive() => isExtracting || isReturning || traveledDistance < travelDistance;
    }
}
