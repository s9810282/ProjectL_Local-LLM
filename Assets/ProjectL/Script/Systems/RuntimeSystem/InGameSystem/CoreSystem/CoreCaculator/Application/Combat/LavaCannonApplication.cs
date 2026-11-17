using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class LavaCannonApplication
    {
        /// <summary>직접 접촉과 별개로 수락 시 포착한 범위에 적용합니다. 공격자가 죽어도 포착한 바인딩은 유효합니다.</summary>
        internal static void Apply(CommandAttackPlan plan, CommandAttackProjection world, IReadOnlyList<EntityId> capturedEnemyIds)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            ApplyCaptured(plan.Actor, plan.Bindings, plan.DamageKind, plan.Input.State.Turn, world.Characters, world.Events,
                new CombatDamageOrigin(plan.Actor.Anchor, plan.Actor.EntityId, plan.AttackGroup), capturedEnemyIds);
        }

        /// <summary>처치 연쇄의 단일 타격도 같은 규칙을 쓰되 원래 공격의 스플래시를 다시 실행하지 않습니다.</summary>
        internal static void ApplyCaptured(CharacterCombatFacts attacker, IReadOnlyList<EffectRuleBinding> bindings, CombatDamageKind kind,
            CombatTurnFacts turn, IDictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events,
            CombatDamageOrigin origin, IReadOnlyList<EntityId> capturedEnemyIds)
        {
            if (capturedEnemyIds == null) throw new ArgumentNullException(nameof(capturedEnemyIds));
            if (!CombatDamageKinds.IsAttack(kind)) return;
            foreach (var binding in bindings)
            {
                if (binding.Rule is not LavaCannonEffectRule rule) continue;
                foreach (var id in capturedEnemyIds)
                {
                    if (!characters.TryGetValue(id, out var recipient)) continue;
                    var statuses = rule.Apply(new EffectRuleInput(binding.Origin, binding.ExpectedState), attacker.Owner, recipient, turn);
                    if (ReferenceEquals(statuses, recipient.StatusEffects)) continue;
                    var final = recipient.WithStatusEffects(statuses);
                    characters[id] = final;
                    CombatCommandEvents.Trait(events, attacker.EntityId, TraitKey.LavaCannon,
                        TraitActivationKind.StatusApplied, 1, new[] { id }, origin: origin);
                    CombatCommandEvents.TraitState(events, recipient, final, origin);
                }
            }
        }
    }
}
