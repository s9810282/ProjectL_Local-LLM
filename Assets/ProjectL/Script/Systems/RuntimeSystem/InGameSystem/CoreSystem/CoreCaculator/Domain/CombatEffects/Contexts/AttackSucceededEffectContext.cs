using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct AttackSucceededEffectContext
    {
        public Rotation4 AttackDirection { get; }
        public bool HasAttackDirection { get; }


        public AttackSucceededEffectContext(
            Rotation4 attackDirection)
            : this(attackDirection,true)
        {
        }

        private AttackSucceededEffectContext(
            Rotation4 attackDirection,            bool hasAttackDirection)
        {
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));
            AttackDirection = attackDirection;
            HasAttackDirection = hasAttackDirection;
        }
    }
}
