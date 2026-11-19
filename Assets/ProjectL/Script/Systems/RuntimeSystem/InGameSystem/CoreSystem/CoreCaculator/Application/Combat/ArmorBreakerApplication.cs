using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>숫자 타격이 확정된 실제 수신자에게 방어구 파괴를 적용합니다.</summary>
    internal static class ArmorBreakerApplication
    {
        internal static void AfterHit(CombatCommandInput input, CharacterCombatFacts actor, EntityId recipient,
            IDictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, CombatDamageOrigin origin = null)
        {
            if (!characters.TryGetValue(recipient, out var victim) || victim.Health.IsDead ||
                !new HostilityPolicy().AreHostile(actor.Owner, victim.Owner)) return;
            if (!global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(actor.Stats.TraitStatuses, TraitKey.ArmorBreaker)) return;
            foreach (var binding in CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)))
                if (binding.Rule is ArmorBreakerEffectRule rule)
                {
                    var state = rule.ApplyAfterHit(new EffectRuleInput(binding.Origin, binding.ExpectedState), victim.Debuffs);
                    characters[recipient] = victim.With(debuffs: state);
                    if (rule.ReductionPercent > 0) events.Add(new CombatEffectsEvent(new[] { new TraitActivationSnapshot(actor.EntityId, recipient,
                        TraitKey.ArmorBreaker, TraitActivationKind.ArmorBroken, rule.ReductionPercent, resultOrigin: origin) }, Array.Empty<CategoryActivationSnapshot>()));
                    CombatCommandEvents.TraitState(events, victim, characters[recipient], origin);
                }
        }
    }
}
