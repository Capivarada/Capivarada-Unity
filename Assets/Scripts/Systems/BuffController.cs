using UnityEngine;
using ArenadasAlmas.Data;
using System.Collections.Generic;

namespace ArenadasAlmas.Systems
{
    public class BuffController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig playerConfig;

        private class ActiveBuff
        {
            public BuffData buffData;
            public float remainingDuration;
            public int stackCount = 1;
        }

        private Dictionary<BuffType, List<ActiveBuff>> activeBuffs = new Dictionary<BuffType, List<ActiveBuff>>();

        public delegate void BuffEventHandler(BuffType buffType, float value, int stack);
        public event BuffEventHandler OnBuffApplied;
        public event BuffEventHandler OnBuffRemoved;

        private void OnEnable()
        {
            if (playerConfig == null)
                playerConfig = GetComponent<PlayerConfig>();
        }

        private void Update()
        {
            UpdateBuffDurations();
        }

        private void UpdateBuffDurations()
        {
            var buffTypesToRemove = new List<BuffType>();

            foreach (var buffType in activeBuffs.Keys)
            {
                var buffList = activeBuffs[buffType];
                for (int i = buffList.Count - 1; i >= 0; i--)
                {
                    buffList[i].remainingDuration -= Time.deltaTime;
                    if (buffList[i].remainingDuration <= 0)
                    {
                        RemoveBuffAt(buffType, i);
                    }
                }

                if (buffList.Count == 0)
                    buffTypesToRemove.Add(buffType);
            }

            foreach (var buffType in buffTypesToRemove)
                activeBuffs.Remove(buffType);
        }

        public void ApplyBuff(BuffData buffData)
        {
            if (buffData == null || playerConfig == null)
                return;

            BuffType buffType = buffData.BuffType;
            int stackCount = 1;

            if (!activeBuffs.ContainsKey(buffType))
                activeBuffs[buffType] = new List<ActiveBuff>();

            var buffList = activeBuffs[buffType];

            if (buffList.Count > 0 && buffData.MaxStack > 1)
            {
                var lastBuff = buffList[buffList.Count - 1];
                if (lastBuff.stackCount < buffData.MaxStack)
                {
                    lastBuff.stackCount++;
                    lastBuff.remainingDuration = buffData.Duration;
                    stackCount = lastBuff.stackCount;
                }
                else
                {
                    var newBuff = new ActiveBuff
                    {
                        buffData = buffData,
                        remainingDuration = buffData.Duration,
                        stackCount = 1
                    };
                    buffList.Add(newBuff);
                }
            }
            else
            {
                var newBuff = new ActiveBuff
                {
                    buffData = buffData,
                    remainingDuration = buffData.Duration,
                    stackCount = 1
                };
                buffList.Add(newBuff);
            }

            playerConfig.ApplyBuffModifier(buffType, buffData.Value);
            OnBuffApplied?.Invoke(buffType, buffData.Value, stackCount);
        }

        public void RemoveBuff(BuffType buffType)
        {
            if (!activeBuffs.ContainsKey(buffType) || activeBuffs[buffType].Count == 0)
                return;

            RemoveBuffAt(buffType, 0);
        }

        private void RemoveBuffAt(BuffType buffType, int index)
        {
            if (!activeBuffs.ContainsKey(buffType) || index >= activeBuffs[buffType].Count)
                return;

            var buff = activeBuffs[buffType][index];
            activeBuffs[buffType].RemoveAt(index);

            playerConfig.ApplyBuffModifier(buffType, -buff.buffData.Value);
            OnBuffRemoved?.Invoke(buffType, buff.buffData.Value, buff.stackCount);
        }

        public int GetActiveBuffCount(BuffType buffType)
        {
            return activeBuffs.ContainsKey(buffType) ? activeBuffs[buffType].Count : 0;
        }

        public bool HasActiveBuff(BuffType buffType)
        {
            return activeBuffs.ContainsKey(buffType) && activeBuffs[buffType].Count > 0;
        }
    }
}
