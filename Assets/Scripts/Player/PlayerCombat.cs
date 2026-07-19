using UnityEngine;
using ArenadasAlmas.Systems;

namespace ArenadasAlmas.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private AimController aimController;
        [SerializeField] private SoulClawController soulClawController;
        [SerializeField] private GameObject shieldPrefab;
        [SerializeField] private Transform clawOriginPoint;

        private float lastClawAttackTime = -999f;
        private float lastDefenseTime = -999f;
        private GameObject activeShield;

        public delegate void SoulStolenEventHandler();
        public delegate void ShieldActivatedEventHandler();

        public event SoulStolenEventHandler OnSoulStolen;
        public event ShieldActivatedEventHandler OnShieldActivated;

        private void OnEnable()
        {
            if (playerConfig == null)
                playerConfig = GetComponent<PlayerConfig>();
            if (playerStats == null)
                playerStats = GetComponent<PlayerStats>();
            if (aimController == null)
                aimController = GetComponent<AimController>();
            if (soulClawController == null)
                soulClawController = GetComponentInChildren<SoulClawController>();
        }

        private void Update()
        {
            HandleClawAttack();
            HandleDefense();
            HandleSoulFunnel();
        }

        private void HandleClawAttack()
        {
            if (Input.GetMouseButtonDown(0))
            {
                float clawCooldown = playerConfig.GetClawCooldown();
                if (Time.time >= lastClawAttackTime + clawCooldown)
                {
                    AttemptClawAttack();
                    lastClawAttackTime = Time.time;
                }
            }
        }

        private void AttemptClawAttack()
        {
            if (aimController == null || soulClawController == null)
                return;

            Vector3 clawOrigin = GetClawOrigin();
            
            if (aimController.GetAimPoint(out Vector3 aimPoint))
            {
                float maxDistance = playerConfig.GetClawMaxDistance();
                if (Vector3.Distance(clawOrigin, aimPoint) > maxDistance)
                {
                    aimPoint = clawOrigin + (aimPoint - clawOrigin).normalized * maxDistance;
                }

                soulClawController.FireClaw(clawOrigin, aimPoint);
                playerStats.SetState(Data.PlayerState.Attacking);
            }
        }

        private void HandleDefense()
        {
            if (Input.GetMouseButtonDown(1))
            {
                float defenseCooldown = playerConfig.GetDefenseCooldown();
                if (Time.time >= lastDefenseTime + defenseCooldown)
                {
                    ActivateShield();
                    lastDefenseTime = Time.time;
                }
            }
        }

        private void ActivateShield()
        {
            if (shieldPrefab == null)
                return;

            if (activeShield != null)
                Destroy(activeShield);

            activeShield = Instantiate(shieldPrefab, transform.position, Quaternion.identity, transform);
            OnShieldActivated?.Invoke();

            float defenseDuration = playerConfig.GetDefenseDuration();
            Destroy(activeShield, defenseDuration);
        }

        private void HandleSoulFunnel()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                UseSoulFunnel();
            }
        }

        private void UseSoulFunnel()
        {
            // Placeholder for future development
        }

        private Vector3 GetClawOrigin()
        {
            if (clawOriginPoint != null)
                return clawOriginPoint.position;
            return transform.position + Vector3.up * 0.5f;
        }

        public bool IsAttacking() => playerStats.GetCurrentState() == Data.PlayerState.Attacking;
        public float GetClawCooldownRemaining() => Mathf.Max(0, playerConfig.GetClawCooldown() - (Time.time - lastClawAttackTime));
        public float GetDefenseCooldownRemaining() => Mathf.Max(0, playerConfig.GetDefenseCooldown() - (Time.time - lastDefenseTime));
    }
}
