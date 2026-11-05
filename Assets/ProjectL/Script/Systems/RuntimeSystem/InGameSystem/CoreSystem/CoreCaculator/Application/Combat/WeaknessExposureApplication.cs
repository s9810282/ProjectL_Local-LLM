using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 직접 타격 조건으로 원래 접촉 대상에게 약점 노출을 부여하고 완전한 상태 사실을 남깁니다.</summary>
    internal static class WeaknessExposureApplication
    {
        internal static void CompleteHit(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId, CombatDamageOrigin origin) =>
            CompleteHit(plan.Actor, plan.Profile, plan.DamageKind, world.Characters, world.Events, targetId, origin);

        internal static void CompleteHit(CharacterCombatFacts attacker, CombatAttackProfile profile, CombatDamageKind kind,
            IDictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, EntityId targetId, CombatDamageOrigin origin = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (events == null) throw new ArgumentNullException(nameof(events));
            var configuration = profile.Definition.Traits.ToArray().OfType<HuntersMarkTraitSpec>().SingleOrDefault();
            if (configuration == null || !characters.TryGetValue(targetId, out var victim) ||
                !HuntersMarkTraitRule.CanApplyMark(attacker, victim, kind)) return;
            var grant = new WeaknessExposureStatus(TraitEffectOrigin.Create(attacker.EntityId, TraitKey.HuntersMark),
                configuration.BonusDamagePercent, configuration.DurationAffectedSideTurnEnds);
            var statuses = AppliedStatusRules.Apply(victim.StatusEffects, grant);
            if (ReferenceEquals(statuses, victim.StatusEffects)) return;
            var final = victim.WithStatusEffects(statuses);
            characters[targetId] = final;
            CombatCommandEvents.Trait(events, attacker.EntityId, TraitKey.HuntersMark, TraitActivationKind.StatusApplied,
                1, new[] { targetId }, victim.OccupiedCells, origin);
            CombatCommandEvents.TraitState(events, victim, final, origin);
        }
    }
}