using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    /// <summary>
    /// 대상별 피격 경감 평가 입력입니다.
    /// 기존 AttackDamageEffectContext는 공격자 기준으로 남겨두고, 이 단계만 대상 정보를 갖습니다.
    /// </summary>
    internal readonly struct IncomingDamageEffectContext
    {
        public int IncomingDamage { get; }
        public CellCoord DefenderAnchor { get; }
        public Rotation4 DefenseFacing { get; }

        /// <summary>
        /// 피해가 들어온 기준점입니다. 직접 공격은 공격자 anchor, 중심형 AoE는 선택 중심입니다.
        /// 관통 피해는 별도의 진행 방향을 사용합니다.
        /// </summary>
        public CellCoord? ImpactOrigin { get; }
        public Rotation4? RayTravelDirection { get; }

        public IncomingDamageEffectContext(
            int incomingDamage,
            CellCoord defenderAnchor,
            Rotation4 defenseFacing,
            CellCoord? impactOrigin,
            Rotation4? rayTravelDirection = null)
        {
            if (incomingDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingDamage));
            if (Enum.IsDefined(typeof(Rotation4), defenseFacing) == false)
                throw new ArgumentOutOfRangeException(nameof(defenseFacing));
            if (rayTravelDirection.HasValue && !Enum.IsDefined(typeof(Rotation4), rayTravelDirection.Value))
                throw new ArgumentOutOfRangeException(nameof(rayTravelDirection));

            IncomingDamage = incomingDamage;
            DefenderAnchor = defenderAnchor;
            DefenseFacing = defenseFacing;
            ImpactOrigin = impactOrigin;
            RayTravelDirection = rayTravelDirection;
        }
    }
}
