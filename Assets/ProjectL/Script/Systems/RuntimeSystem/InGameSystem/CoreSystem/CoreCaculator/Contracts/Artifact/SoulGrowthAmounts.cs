using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    [Serializable]
    public struct SoulGrowthAmounts
    {
        public int attackBonus;
        public int maxHealthBonus;
        public int healAmount;

        public int AttackBonus => attackBonus;
        public int MaxHealthBonus => maxHealthBonus;
        public int HealAmount => healAmount;

        public SoulGrowthAmounts(int attackBonus, int maxHealthBonus, int healAmount)
        {
            if (attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonus));
            if (maxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealthBonus));
            if (healAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(healAmount));

            this.attackBonus = attackBonus;
            this.maxHealthBonus = maxHealthBonus;
            this.healAmount = healAmount;
        }
    }
}
