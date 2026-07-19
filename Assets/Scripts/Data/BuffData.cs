using UnityEngine;

namespace ArenadasAlmas.Data
{
    [CreateAssetMenu(fileName = "Buff_", menuName = "ArenadasAlmas/Buff", order = 3)]
    public class BuffData : ScriptableObject
    {
        [SerializeField] private BuffType buffType;
        [SerializeField] private float value;
        [SerializeField] private float duration;
        [SerializeField] private int maxStack;

        public BuffType BuffType => buffType;
        public float Value => value;
        public float Duration => duration;
        public int MaxStack => maxStack;
    }
}
