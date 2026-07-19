using UnityEngine;

namespace ArenadasAlmas.Data
{
    [CreateAssetMenu(fileName = "Attack_", menuName = "ArenadasAlmas/Attack", order = 2)]
    public class AttackData : ScriptableObject
    {
        [SerializeField] private string attackName;
        [SerializeField] private float damage;
        [SerializeField] private float cooldown;
        [SerializeField] private float range;
        [SerializeField] private float speed;
        [SerializeField] private float duration;
        [SerializeField] private GameObject effectPrefab;

        public string AttackName => attackName;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public float Range => range;
        public float Speed => speed;
        public float Duration => duration;
        public GameObject EffectPrefab => effectPrefab;
    }
}
