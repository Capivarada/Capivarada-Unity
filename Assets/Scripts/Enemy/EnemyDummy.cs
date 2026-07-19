using UnityEngine;
using ArenadasAlmas.Data;

namespace ArenadasAlmas.Enemy
{
    public class EnemyDummy : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private int maxSouls = 3;

        private float currentHealth;
        private int currentSouls;
        private bool isAlive = true;
        private bool isBeingExtracted = false;

        public delegate void HealthEventHandler(float currentHealth, float maxHealth);
        public delegate void SoulEventHandler(int currentSouls, int maxSouls);
        public delegate void DeathEventHandler();
        public delegate void ExtractionEventHandler();

        public event HealthEventHandler OnHealthChanged;
        public event SoulEventHandler OnSoulCountChanged;
        public event DeathEventHandler OnEnemyDeath;
        public event ExtractionEventHandler OnExtractionStarted;
        public event ExtractionEventHandler OnExtractionEnded;

        public bool IsBeingExtracted => isBeingExtracted;

        private void OnEnable()
        {
            ResetStats();
        }

        private void ResetStats()
        {
            currentHealth = maxHealth;
            currentSouls = maxSouls;
            isAlive = true;
            isBeingExtracted = false;
        }

        public void TakeDamage(float damage)
        {
            if (!isAlive)
                return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void StartExtraction()
        {
            if (!isAlive || isBeingExtracted)
                return;

            isBeingExtracted = true;
            OnExtractionStarted?.Invoke();
        }

        public void EndExtraction()
        {
            if (!isBeingExtracted)
                return;

            isBeingExtracted = false;
            OnExtractionEnded?.Invoke();
        }

        public void RemoveSoul()
        {
            if (!isAlive || currentSouls <= 0)
                return;

            EndExtraction();
            currentSouls--;
            OnSoulCountChanged?.Invoke(currentSouls, maxSouls);

            if (currentSouls <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isAlive = false;
            isBeingExtracted = false;
            OnEnemyDeath?.Invoke();
            Destroy(gameObject);
        }

        public float GetHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
        public int GetSoulCount() => currentSouls;
        public int GetMaxSouls() => maxSouls;
        public bool IsAlive() => isAlive;
    }
}
