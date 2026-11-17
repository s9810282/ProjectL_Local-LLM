using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>발리스타 충격 볼트와 가시 폭탄이 함께 쓰는 중심 바깥 밀기 방향이다(설계 §6.3).</summary>
    internal static class ImpactCenterGeometry
    {
        /// <summary>
        /// 살아 있는 필드 유닛의 중심 바깥 방향만 정한다. 진영은 보지 않는다(아군도 민다, 2026-10-27 사용자 결정).
        /// 누가 맞는지는 공격 대상 자격이 정하고, 면역·경로·이동 적용은 기존 실행기가 맡는다.
        /// </summary>
        internal static bool TryResolveStep(CharacterCombatFacts target, CellCoord center, out CellCoord step)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            step = default;
            if (target.Health.IsDead || target.Area != CharacterRuntimeArea.Field || target.OccupiedCells.Contains(center))
                return false;

            step = ForcedDisplacementGeometry.ResolveBranchStep(center, target.Anchor.Value);
            return true;
        }
    }
}
