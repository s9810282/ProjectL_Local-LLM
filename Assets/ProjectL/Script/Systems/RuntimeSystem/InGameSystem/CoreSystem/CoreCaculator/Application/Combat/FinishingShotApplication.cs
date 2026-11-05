using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>원래 타격 대상의 현재 HP와 공격력을 비교합니다. 숫자 피해의 희생 수신자와 구분합니다.</summary>
    internal static class FinishingShotApplication
    {
        internal static void Record(CharacterCombatFacts actor, EntityId target, int health,
            IReadOnlyList<CellCoord> cells, CombatDamageOrigin origin, List<CombatEvent> events)
        {
            CombatCommandEvents.Trait(events, actor.EntityId, TraitKey.FinishingShot, TraitActivationKind.InstantDefeat,
                health, new[] { target }, cells, origin);
        }

        internal static bool Qualifies(CombatCommandInput input, CharacterCombatFacts actor, CharacterCombatFacts target,
            StructureCombatFacts structure = null)
        {
            if (target == null && structure == null || !actor.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.FinishingShot)) return false;
            var health = target?.Health ?? structure.Health;
            var owner = target?.Owner ?? structure.Owner;
            if (health.IsDead || !new HostilityPolicy().AreHostile(actor.Owner, owner)) return false;
            foreach (var binding in CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)))
                if (binding.Rule is FinishingShotEffectRule rule && rule.Qualifies(new EffectRuleInput(binding.Origin, binding.ExpectedState),
                    CombatEffectiveStatsCalculator.AttackPower(actor), health)) return true;
            return false;
        }
    }
}
