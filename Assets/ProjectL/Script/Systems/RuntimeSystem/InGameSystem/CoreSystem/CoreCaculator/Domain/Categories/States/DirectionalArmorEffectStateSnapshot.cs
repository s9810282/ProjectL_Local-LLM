using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    /// <summary>
    /// 공통 CombatEffects에 전달하는 불변 Heavy 방어 상태입니다.
    /// </summary>
    public sealed class DirectionalArmorEffectStateSnapshot : IEffectStateSnapshot
    {
        public Rotation4 DefenseFacing { get; }
        public bool MovedThisTurn { get; }

        public DirectionalArmorEffectStateSnapshot(Rotation4 defenseFacing, bool movedThisTurn)
        {
            if (Enum.IsDefined(typeof(Rotation4), defenseFacing) == false)
                throw new ArgumentOutOfRangeException(nameof(defenseFacing));

            DefenseFacing = defenseFacing;
            MovedThisTurn = movedThisTurn;
        }

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is DirectionalArmorEffectStateSnapshot snapshot &&
                   snapshot.DefenseFacing == DefenseFacing &&
                   snapshot.MovedThisTurn == MovedThisTurn;
        }
    }
}
