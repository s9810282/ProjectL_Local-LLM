using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>다른 전투 사실을 유지하며 대상의 디버프만 교체합니다.</summary>
    public static class CombatDebuffApplication
    {
        /// <summary>
        /// 한 공격의 모든 전달이 끝난 뒤 대상별 적용과 실제 공격자의 해제를 정산합니다.
        /// 수락된 일반 공격은 피해가 없는 끌어오기여도 공격 완료로 보고 공격자의 위협 효과를 끝냅니다(설계 R11).
        /// </summary>
        internal static void CompleteAttack(CombatCommandInput input, CharacterCombatFacts attackerAtStart,
            IReadOnlyCollection<EntityId> deliveredVictims, IDictionary<EntityId, CharacterCombatFacts> projected,
            List<CombatEvent> events = null, CombatDamageOrigin origin = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (attackerAtStart == null) throw new ArgumentNullException(nameof(attackerAtStart));
            if (deliveredVictims == null) throw new ArgumentNullException(nameof(deliveredVictims));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            var spec = CombatCommandCalculator.RequiredProfile(input, attackerAtStart).Definition.Traits.ToArray()
                .OfType<IntimidationTraitSpec>().SingleOrDefault();
            if (spec != null)
            {
                var hostility = new HostilityPolicy();
                var sourceOrigin = CombatCommandEffects.Capture(attackerAtStart, CombatCommandCalculator.RequiredProfile(input, attackerAtStart))
                    .Single(binding => binding.Origin.DefinitionKey == (int)TraitKey.Intimidation).Origin;
                foreach (var id in deliveredVictims.Distinct())
                {
                    if (!projected.TryGetValue(id, out var victim) || victim.Health.IsDead ||
                        !hostility.AreHostile(attackerAtStart.Owner, victim.Owner)) continue;
                    var statuses = AppliedStatusRules.Apply(victim.StatusEffects, new AttackDecreaseStatus(sourceOrigin, spec.AttackReductionPercent));
                    var updated = victim.WithStatusEffects(statuses);
                    var debuffs = updated.Debuffs;
                    if (!victim.Debuffs.SameValues(debuffs))
                    {
                        if (debuffs.Intimidation.Origin.HasValue)
                            CombatCommandEvents.Trait(events, new EntityId(debuffs.Intimidation.Origin.Value.OwnerInstanceId), TraitKey.Intimidation,
                                TraitActivationKind.StatusApplied, debuffs.Intimidation.AttackReductionPercent, new[] { id }, origin: origin);
                        projected[id] = updated;
                        CombatCommandEvents.TraitState(events, victim, projected[id], origin);
                    }
                }
            }
            if (attackerAtStart.Debuffs.Intimidation != null)
            {
                var actor = projected[attackerAtStart.EntityId];
                var statusOrigin = actor.Debuffs.Intimidation?.Origin;
                if (statusOrigin.HasValue && statusOrigin.Value.Kind == EffectOwnerKind.Trait)
                    CombatCommandEvents.Trait(events, new EntityId(statusOrigin.Value.OwnerInstanceId), (TraitKey)statusOrigin.Value.DefinitionKey,
                        TraitActivationKind.StatusConsumed, 0, new[] { actor.EntityId }, origin: origin);
                projected[actor.EntityId] = actor.WithStatusEffects(AppliedStatusRules.CompleteAttack(actor.StatusEffects));
                CombatCommandEvents.TraitState(events, actor, projected[actor.EntityId], origin);
            }
        }

        public static CharacterCombatFacts WithDebuffs(CharacterCombatFacts character, CharacterDebuffState debuffs)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (debuffs == null)
                throw new ArgumentNullException(nameof(debuffs));

            return character.With(debuffs: debuffs);
        }
    }
}
