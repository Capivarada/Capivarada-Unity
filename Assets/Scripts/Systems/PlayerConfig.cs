using UnityEngine;

namespace ArenadasAlmas.Systems
{
    public class PlayerConfig : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dashForce = 20f;
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float clawCooldown = 0.5f;
        [SerializeField] private float clawTravelSpeed = 30f;
        [SerializeField] private float clawExtractionDuration = 2f;
        [SerializeField] private float clawMaxDistance = 50f;
        [SerializeField] private float defenseDuration = 3f;
        [SerializeField] private float defenseCooldown = 5f;

        private float currentMoveSpeed;
        private float currentDashForce;
        private float currentDashCooldown;
        private float currentClawCooldown;
        private float currentClawTravelSpeed;
        private float currentClawExtractionDuration;
        private float currentClawMaxDistance;
        private float currentDefenseDuration;
        private float currentDefenseCooldown;

        private void OnEnable()
        {
            ResetToBase();
        }

        public void ResetToBase()
        {
            currentMoveSpeed = moveSpeed;
            currentDashForce = dashForce;
            currentDashCooldown = dashCooldown;
            currentClawCooldown = clawCooldown;
            currentClawTravelSpeed = clawTravelSpeed;
            currentClawExtractionDuration = clawExtractionDuration;
            currentClawMaxDistance = clawMaxDistance;
            currentDefenseDuration = defenseDuration;
            currentDefenseCooldown = defenseCooldown;
        }

        public void ApplyBuffModifier(Data.BuffType buffType, float value)
        {
            switch (buffType)
            {
                case Data.BuffType.MoveSpeed:
                    currentMoveSpeed += value;
                    break;
                case Data.BuffType.DashForce:
                    currentDashForce += value;
                    break;
                case Data.BuffType.DashCooldown:
                    currentDashCooldown += value;
                    break;
                case Data.BuffType.ClawSpeed:
                    currentClawTravelSpeed += value;
                    break;
                case Data.BuffType.DefenseDuration:
                    currentDefenseDuration += value;
                    break;
                case Data.BuffType.AbilityCooldown:
                    currentClawCooldown += value;
                    break;
            }
        }

        public float GetMoveSpeed() => currentMoveSpeed;
        public float GetDashForce() => currentDashForce;
        public float GetDashCooldown() => currentDashCooldown;
        public float GetDashDuration() => dashDuration;
        public float GetClawCooldown() => currentClawCooldown;
        public float GetClawTravelSpeed() => currentClawTravelSpeed;
        public float GetClawExtractionDuration() => currentClawExtractionDuration;
        public float GetClawMaxDistance() => currentClawMaxDistance;
        public float GetDefenseDuration() => currentDefenseDuration;
        public float GetDefenseCooldown() => currentDefenseCooldown;
    }
}
