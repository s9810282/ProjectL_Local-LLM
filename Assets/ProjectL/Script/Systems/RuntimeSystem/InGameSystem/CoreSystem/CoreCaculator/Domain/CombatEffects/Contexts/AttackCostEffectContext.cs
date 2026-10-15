using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct AttackCostEffectContext
    {
        public int BaseAttackCost { get; }

        public AttackCostEffectContext(int baseAttackCost)
        {
            if (baseAttackCost < 1) throw new ArgumentOutOfRangeException(nameof(baseAttackCost));

            BaseAttackCost = baseAttackCost;
        }
    }
}
