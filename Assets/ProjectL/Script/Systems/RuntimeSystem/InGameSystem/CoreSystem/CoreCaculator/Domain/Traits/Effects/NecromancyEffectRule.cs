using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>
    /// 사령술은 공격·이동 효과 훅에 참여하지 않는다. 소환 실행은 보스 턴 계산이 정의를 읽어 처리한다.
    /// 등록 계약을 맞추기 위한 수동 규칙이며 효과 기여를 만들지 않는다.
    /// </summary>
    internal sealed class NecromancyEffectRule : ICombatEffectRule
    {
        internal NecromancyEffectRule(NecromancyTraitDefinition definition, NecromancyTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
        }
    }
}
