using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class HuntersMarkTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.HuntersMark;

        /// <summary>등록된 표식 보유자가 이 직접 타격의 생존 적에게 표식을 남길 수 있는지 판단합니다.</summary>
        internal static bool CanApplyMark(CharacterCombatFacts attacker, CharacterCombatFacts victim, CombatDamageKind kind)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            // 미정(설계 §5.1 R10): 반응 공격은 제외하지만, 반응에서 파생된 후속 타격은 ExtraAttack이라 여기서 통과한다.
            // 반응 유닛이 처치 연쇄·후속 공격 특성을 얻기 전에 표식을 남기거나 소모해도 되는지 정한다.
            return (kind == CombatDamageKind.OrdinaryAttack || kind == CombatDamageKind.ExtraAttack || kind == CombatDamageKind.PlayerSkillAttack) &&
                victim != null && !victim.Health.IsDead && victim.Area == CharacterRuntimeArea.Field &&
                new HostilityPolicy().AreHostile(attacker.Owner, victim.Owner);
        }

    }
}
