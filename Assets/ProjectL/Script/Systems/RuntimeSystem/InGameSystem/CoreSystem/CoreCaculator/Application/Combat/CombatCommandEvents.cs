using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class CombatCommandEvents
    {
        internal static void Health(List<CombatEvent> events, CharacterCombatFacts before, CharacterCombatFacts after,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null)
        {
            if (before.Health.CurrentHealth != after.Health.CurrentHealth || before.Health.MaxHealth != after.Health.MaxHealth)
                events.Add(new CombatCharacterEvent(after.Health.IsDead ? CombatEventKind.Died : CombatEventKind.HealthChanged,
                    before, after, damageOrigins));
        }
        internal static void Effects(List<CombatEvent> events, CombatEffectPlan plan)
        {
            var traits = new List<TraitActivationSnapshot>(); var categories = new List<CategoryActivationSnapshot>();
            CombatCommandEffects.AppendActivations(plan, traits, categories);
            if (traits.Count + categories.Count != 0) events.Add(new CombatEffectsEvent(traits, categories));
        }
    }
}
