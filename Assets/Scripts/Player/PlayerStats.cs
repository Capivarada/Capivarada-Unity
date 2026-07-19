using UnityEngine;
using ArenadasAlmas.Data;

namespace ArenadasAlmas.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int maxSouls = 3;

        private float currentHealth;
        private int currentSouls;
        private Data.SoulData primarySoul;
        private Data.SoulData secondarySoul;
        private PlayerState currentState = PlayerState.Normal;

        public delegate void HealthEventHandler(float currentHealth, float maxHealth);
        public delegate void SoulEventHandler(int currentSouls, int maxSouls);
        public delegate void StateChangeEventHandler(PlayerState newState);
        public delegate void DeathEventHandler();
        public delegate void SoulLostEventHandler();

        public event HealthEventHandler OnHealthChanged;
        public event SoulEventHandler OnSoulCountChanged;
        public event StateChangeEventHandler OnStateChanged;
        public event DeathEventHandler OnPlayerDeath;
        public event SoulLostEventHandler OnSoulLost;

        private void OnEnable()
        {
            ResetStats();
        }

        private void ResetStats()
        {
            currentHealth = maxHealth;
            currentSouls = maxSouls;
            SetState(PlayerState.Normal);
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                OnPlayerDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void AddSoul(Data.SoulData soul)
        {
            if (currentSouls < maxSouls)
            {
                currentSouls++;
                OnSoulCountChanged?.Invoke(currentSouls, maxSouls);
            }
        }

        public void RemoveSoul()
        {
            if (currentSouls > 0)
            {
                currentSouls--;
                OnSoulLost?.Invoke();
                OnSoulCountChanged?.Invoke(currentSouls, maxSouls);

                if (currentSouls == 0)
                    SetState(PlayerState.NoSouls);
            }
        }

        public void SetPrimarySoul(Data.SoulData soul)
        {
            primarySoul = soul;
        }

        public void SetSecondarySoul(Data.SoulData soul)
        {
            secondarySoul = soul;
        }

        public void SetState(PlayerState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(newState);
            }
        }

        public float GetHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
        public int GetSoulCount() => currentSouls;
        public int GetMaxSouls() => maxSouls;
        public PlayerState GetCurrentState() => currentState;
        public Data.SoulData GetPrimarySoul() => primarySoul;
        public Data.SoulData GetSecondarySoul() => secondarySoul;
    }
}
