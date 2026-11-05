using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>개체 지정 대상에 접촉하면, 실제로 피해를 받은 생존 적(희생이 대신 맞으면 수호자)에게 제한을 갱신합니다.</summary>
    internal static class TraitRestrictionApplication
    {
        /// <param name="targetId">접촉한 원래 대상. 지정 대상인지 판정한다.</param>
        /// <param name="recipientId">실제로 피해를 받은 유닛. 희생이 대신 맞으면 수호자이고, 제한은 이 유닛이 받는다.</param>
        internal static void AfterContact(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId, EntityId recipientId,
            CombatDamageOrigin origin = null)
        {
            if (plan.Selection.SelectedTargetId.IsEmpty || plan.Selection.SelectedTargetId != targetId ||
                !world.Characters.TryGetValue(recipientId, out var victim) || victim.Health.IsDead ||
                !new HostilityPolicy().AreHostile(plan.Actor.Owner, victim.Owner)) return;
            if (!plan.Actor.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Disarm || t.Key == TraitKey.AnkleBreaker)) return;
            var current = victim.Debuffs;
            var statuses = victim.StatusEffects;
            foreach (var binding in plan.Bindings)
            {
                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                if (binding.Rule is DisarmEffectRule attackRule) statuses = AppliedStatusRules.Apply(statuses, attackRule.Evaluate(input).ToStatus());
                if (binding.Rule is AnkleBreakerEffectRule moveRule) statuses = AppliedStatusRules.Apply(statuses, moveRule.Evaluate(input).ToStatus());
            }
            var updated = victim.WithStatusEffects(statuses);
            var debuffs = updated.Debuffs;
            var disarm = debuffs.Disarm;
            var ankleBreaker = debuffs.AnkleBreaker;
            if (!current.SameValues(debuffs))
            {
                if (disarm != null && (current.Disarm == null || !current.Disarm.SameValues(disarm)))
                    CombatCommandEvents.Trait(world.Events, new EntityId(disarm.Origin.OwnerInstanceId), TraitKey.Disarm,
                        TraitActivationKind.StatusApplied, disarm.RemainingAffectedSideTurnEnds, new[] { recipientId }, origin: origin);
                if (ankleBreaker != null && (current.AnkleBreaker == null || !current.AnkleBreaker.SameValues(ankleBreaker)))
                    CombatCommandEvents.Trait(world.Events, new EntityId(ankleBreaker.Origin.OwnerInstanceId), TraitKey.AnkleBreaker,
                        TraitActivationKind.StatusApplied, ankleBreaker.RemainingAffectedSideTurnEnds, new[] { recipientId }, origin: origin);
                world.Characters[recipientId] = updated;
                CombatCommandEvents.TraitState(world.Events, victim, world.Characters[recipientId], origin);
            }
        }
    }
}
