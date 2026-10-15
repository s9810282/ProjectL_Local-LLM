using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct AttackSucceededEffectContext
    {
        public IReadOnlyList<EffectOriginKey> PositiveCostContributors { get; }
        public Rotation4 AttackDirection { get; }
        public bool HasAttackDirection { get; }

        public AttackSucceededEffectContext(EffectOriginKey[] positiveCostContributors)
            : this(positiveCostContributors, Rotation4.R0, false)
        {
        }

        public AttackSucceededEffectContext(
            EffectOriginKey[] positiveCostContributors,
            Rotation4 attackDirection)
            : this(positiveCostContributors, attackDirection, true)
        {
        }

        private AttackSucceededEffectContext(
            EffectOriginKey[] positiveCostContributors,
            Rotation4 attackDirection,
            bool hasAttackDirection)
        {
            if (positiveCostContributors == null)
                throw new ArgumentNullException(nameof(positiveCostContributors));

            var copied = (EffectOriginKey[])positiveCostContributors.Clone();
            Array.Sort(copied);

            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectOriginKey origin in copied)
            {
                if (origin.IsValid == false)
                    throw new ArgumentException("Invalid contributor origin", nameof(positiveCostContributors));
                if (seen.Add(origin) == false)
                    throw new ArgumentException("Duplicate contributor origin", nameof(positiveCostContributors));
            }

            PositiveCostContributors = Array.AsReadOnly(copied);
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));
            AttackDirection = attackDirection;
            HasAttackDirection = hasAttackDirection;
        }
    }
}
