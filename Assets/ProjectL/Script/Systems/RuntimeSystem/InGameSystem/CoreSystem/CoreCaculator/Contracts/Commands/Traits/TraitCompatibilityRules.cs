using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>기본·병과·보유 설정을 합친 공격 특성의 배타 조건을 검사합니다.</summary>
    internal static class TraitCompatibilityRules
    {
        internal static void Validate(IReadOnlyList<TraitSpec> traits)
        {
            if (traits == null) throw new ArgumentNullException(nameof(traits));

            TraitKey displacement = TraitKey.None;
            bool piercing = false;
            foreach (var trait in traits)
            {
                var key = trait.TraitKey;
                if (key == TraitKey.PiercingFalloff) piercing = true;
                if (key is not (TraitKey.Knockback or TraitKey.Pull or TraitKey.ImpactBolt or TraitKey.CrashImpact or TraitKey.DeadHand))
                    continue;
                if (displacement != TraitKey.None)
                    throw new ArgumentException($"공격 강제 이동 특성은 함께 쓸 수 없습니다: {displacement}, {key}.", nameof(traits));
                displacement = key;
            }

            if (piercing && displacement == TraitKey.DeadHand)
                throw new ArgumentException("관통과 죽음의 손 특성은 함께 쓸 수 없습니다.", nameof(traits));
        }
    }
}
