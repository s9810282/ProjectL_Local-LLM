using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 캐릭터 넉백 면역 판정의 원본입니다. 명시적 강제 이동 면역·카테고리 정책·굳건한 의지를 함께 봅니다.
    /// 공격 넉백, 사망 특성 보유자 시체 밀기, 재구성 부활 넉백이 이 함수 하나를 부릅니다.
    /// </summary>
    internal static class KnockbackImmunityCheck
    {
        /// <param name="preventedBySteadfastWill">굳건한 의지 때문에만 막혔으면 참입니다. 호출자가 막힘 발동 사건을 남길 때 씁니다.</param>
        internal static bool IsImmune(CombatCommandInput input, CharacterCombatFacts victim, out bool preventedBySteadfastWill)
        {
            bool forcedImmune = UnitBehaviorRules.IsForcedMovementImmune(input, victim);
            bool traitImmune = global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(victim.Stats.TraitStatuses, TraitKey.SteadfastWill) &&
                CombatCommandEffects.Capture(victim, CombatCommandCalculator.RequiredProfile(input, victim))
                    .Any(binding => binding.Rule is SteadfastWillEffectRule rule &&
                        rule.GrantsKnockbackImmunity(new EffectRuleInput(binding.Origin, binding.ExpectedState), victim.EntityId));
            bool immune = KnockbackImmunityPolicy.IsImmuneWithTrait(victim.Stats.Category.Key, forcedImmune, traitImmune);
            preventedBySteadfastWill = immune && traitImmune && !KnockbackImmunityPolicy.IsImmune(victim.Stats.Category.Key, forcedImmune);
            return immune;
        }
    }
}
