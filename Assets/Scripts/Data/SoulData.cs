using UnityEngine;

namespace ArenadasAlmas.Data
{
    [CreateAssetMenu(fileName = "Soul_", menuName = "ArenadasAlmas/Soul", order = 1)]
    public class SoulData : ScriptableObject
    {
        [SerializeField] private string soulName;
        [SerializeField] private string description;
        [SerializeField] private Sprite sprite;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private int level;
        [SerializeField] private float healthValue;
        [SerializeField] private float activeCooldown;
        [SerializeField] private BuffType passiveBuffType;
        [SerializeField] private float passiveBuffValue;

        public string SoulName => soulName;
        public string Description => description;
        public Sprite Sprite => sprite;
        public GameObject VisualPrefab => visualPrefab;
        public int Level => level;
        public float HealthValue => healthValue;
        public float ActiveCooldown => activeCooldown;
        public BuffType PassiveBuffType => passiveBuffType;
        public float PassiveBuffValue => passiveBuffValue;
    }
}
