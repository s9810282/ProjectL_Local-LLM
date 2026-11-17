using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class ImpactBoltTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.ImpactBolt;

        /// <summary>
        /// 살아 있는 필드 유닛의 중심 바깥 방향만 정한다. 진영은 보지 않는다(아군도 민다, 2026-10-27 사용자 결정).
        /// 누가 맞는지는 공격 대상 자격이 정하고, 면역·경로·이동 적용은 기존 실행기가 맡는다.
        /// </summary>
        internal static bool TryResolveStep(CharacterCombatFacts target, CellCoord center, out CellCoord step)
        {
            return ImpactCenterGeometry.TryResolveStep(target, center, out step);
        }
    }
}
