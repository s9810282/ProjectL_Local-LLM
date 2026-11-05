using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>특성 등록만 연결합니다. 대상 디버프 적용과 해제는 공격 경계가 담당합니다.</summary>
    internal sealed class IntimidationEffectRule : ICombatEffectRule
    {
        internal IntimidationEffectRule(IntimidationTraitDefinition definition, IntimidationTraitRule rule)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
        }
    }
}
