using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal enum EffectKnockbackMode
    {
        AttackerOrigin = 0,
        ImpactCenter = 1,
    }

    /// <summary>
    /// 넉백이 막혀 멈출 때의 부딪힘 피해 비율(날아간 대상 최대 체력의 %)이다. 작렬 같은 넉백 변형이 넉백 기여에 실어 보낸다.
    /// </summary>
    internal readonly struct EffectKnockbackCollision
    {
        /// <summary>벽·맵 끝·고지대 턱·구조물에 부딪힐 때 날아간 대상이 받는 비율이다.</summary>
        public int WallDamagePercent { get; }
        /// <summary>유닛에 부딪힐 때 날아간 대상과 부딪힌 유닛이 각각 받는 비율이다.</summary>
        public int UnitDamagePercent { get; }

        public EffectKnockbackCollision(int wallDamagePercent, int unitDamagePercent)
        {
            if (wallDamagePercent < 0 || wallDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(wallDamagePercent));
            if (unitDamagePercent < 0 || unitDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(unitDamagePercent));
            WallDamagePercent = wallDamagePercent;
            UnitDamagePercent = unitDamagePercent;
        }
    }

    internal readonly struct EffectKnockbackContribution
    {
        public EffectOriginKey Origin { get; }
        public int DistanceCells { get; }
        public EffectKnockbackMode Mode { get; }
        /// <summary>막혀 멈출 때 부딪힘 피해를 내는 기여면 값이 있다. 일반 넉백은 비어 있다.</summary>
        public EffectKnockbackCollision? Collision { get; }

        public EffectKnockbackContribution(EffectOriginKey origin, int distanceCells,
            EffectKnockbackMode mode = EffectKnockbackMode.AttackerOrigin, EffectKnockbackCollision? collision = null)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));
            if (!Enum.IsDefined(typeof(EffectKnockbackMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            Origin = origin;
            DistanceCells = distanceCells;
            Mode = mode;
            Collision = collision;
        }
    }
}
