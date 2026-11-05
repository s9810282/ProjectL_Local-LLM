using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>대상 고정·행동 끝 재선택과 기존 처치 소비자의 보유 변경 제안을 연결합니다.</summary>
    internal static class HunterApplication
    {
        internal static bool HasHunters(IEnumerable<CharacterCombatFacts> characters) => characters.Any(c => c.OwnedId.HasValue && Status(c) != null);

        internal static CombatCommandState Reconcile(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> characters,
            CombatCommandState state, List<CombatEvent> events)
        {
            if (!HasHunters(characters.Values)) return state;
            var environment = new CommandEnvironmentState(state.Environment, input.EnvironmentBindings, state.EnvironmentProgress);
            Reconcile(input, characters, environment.ChooseRandom, events);
            return state.With(environmentProgress: environment.Progress);
        }

        internal static void Reconcile(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> characters,
            Func<int, int> chooseRandom, List<CombatEvent> events)
        {
            foreach (var owner in characters.Values.Where(c => c.OwnedId.HasValue && c.Owner == Owner.Player &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && Status(c) != null).OrderBy(c => c.EntityId).ToArray())
            {
                var status = Status(owner);
                var binding = Binding(input, owner);
                var rule = (HunterEffectRule)binding.Rule;
                EntityId? target = status.TargetId;
                if (status.SuccessfulRewards >= rule.MaxRewards) target = null;
                else if (!target.HasValue || !characters.TryGetValue(target.Value, out var existing) || existing.Health.IsDead || existing.Area != CharacterRuntimeArea.Field)
                {
                    var candidates = rule.Candidates(new EffectRuleInput(binding.Origin, binding.ExpectedState), owner, characters.Values);
                    target = candidates.Count == 0 ? null : candidates[candidates.Count == 1 ? 0 : chooseRandom(candidates.Count)];
                }
                if (target == status.TargetId) continue;
                characters[owner.EntityId] = WithStatus(owner, new HunterTraitStatusSnapshot(target, status.SuccessfulRewards));
                events.Add(new CombatEffectsEvent(new[] { new TraitActivationSnapshot(owner.EntityId, target, TraitKey.Hunter,
                    target.HasValue ? TraitActivationKind.HunterTargetSelected : TraitActivationKind.StatusRemoved, 0) }, Array.Empty<CategoryActivationSnapshot>()));
                CombatCommandEvents.TraitState(events, owner, characters[owner.EntityId]);
            }
        }

        internal static void TargetDied(CombatCommandInput input, CombatDamageApplication damage, CombatCharacterEvent death, EntityId? credited)
        {
            foreach (var owner in damage.Characters.Values.Where(c => c.OwnedId.HasValue && c.Owner == Owner.Player &&
                Status(c)?.TargetId == death.Final.EntityId).OrderBy(c => c.EntityId).ToArray())
            {
                var status = Status(owner);
                var binding = Binding(input, owner, damage.ProfileReads);
                var rule = (HunterEffectRule)binding.Rule;
                if (credited != owner.EntityId || status.SuccessfulRewards >= rule.MaxRewards ||
                    !new HostilityPolicy().AreHostile(owner.Owner, death.Expected.Owner)) continue;
                var expected = damage.OwnedRecord(owner.OwnedId.Value);
                var final = rule.Award(new EffectRuleInput(binding.Origin, binding.ExpectedState), expected);
                damage.ProposeOwned(new OwnedCharacterChange(expected, final));
                var updated = WithStatus(owner, new HunterTraitStatusSnapshot(status.TargetId, checked(status.SuccessfulRewards + 1)));
                damage.Characters[owner.EntityId] = CombatEffectiveStatsCalculator.ProjectTraitAttack(updated, final.HunterAttackBonus);
                int awardedAttack = final.HunterAttackBonus - expected.HunterAttackBonus;
                if (awardedAttack > 0)
                {
                    damage.Events.Add(new CombatEffectsEvent(new[] { new TraitActivationSnapshot(owner.EntityId, owner.EntityId,
                        TraitKey.Hunter, TraitActivationKind.AttackGranted, awardedAttack,
                        resultOrigin: death.DamageOrigins.FirstOrDefault(o => o.CreditedCharacterId == credited) ?? death.DamageOrigins.FirstOrDefault()) }, Array.Empty<CategoryActivationSnapshot>()));
                }
                CombatCommandEvents.TraitState(damage.Events, owner, damage.Characters[owner.EntityId], death.DamageOrigins.FirstOrDefault());
            }
        }

        internal static void FinishAction(AttackTraitScope scope, CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> characters,
            Func<int, int> chooseRandom, List<CombatEvent> events, bool canSelect)
        {
            if (scope.IsFinished) return;
            var before = characters[scope.ActorId];
            scope.Finish(characters);
            var after = characters[scope.ActorId];
            int spent = AccelerationTraitRule.Held(before.Stats) - AccelerationTraitRule.Held(after.Stats);
            if (spent > 0)
            {
                var origin = CombatCommandEvents.FindOrigin(events, scope.ActorId);
                CombatCommandEvents.Trait(events, scope.ActorId, TraitKey.Acceleration, TraitActivationKind.StatusConsumed,
                    spent, new[] { scope.ActorId }, after.OccupiedCells, origin);
                CombatCommandEvents.TraitState(events, before, after, origin);
            }
            if (canSelect && !scope.HasActiveAncestor) Reconcile(input, characters, chooseRandom, events);
        }

        internal static UnitStatsSnapshot ClearStage(UnitStatsSnapshot stats) => stats.With(traitStatuses: stats.TraitStatuses.ToArray()
            .Select(t => t is HunterTraitStatusSnapshot h && (h.TargetId.HasValue || h.SuccessfulRewards != 0) ? new HunterTraitStatusSnapshot() : t).ToArray());
        private static HunterTraitStatusSnapshot Status(CharacterCombatFacts owner) => owner.Stats.TraitStatuses.ToArray().OfType<HunterTraitStatusSnapshot>().SingleOrDefault();
        private static CharacterCombatFacts WithStatus(CharacterCombatFacts owner, HunterTraitStatusSnapshot status) => owner.With(stats: owner.Stats.With(
            traitStatuses: owner.Stats.TraitStatuses.ToArray().Select(t => t.Key == TraitKey.Hunter ? status : t).ToArray()));
        private static EffectRuleBinding Binding(CombatCommandInput input, CharacterCombatFacts owner,
            CombatProfileReadSet profileReads = null) =>
            CombatCommandEffects.Capture(owner, CombatCommandCalculator.RequiredProfile(input, owner, profileReads)).Single(b => b.Rule is HunterEffectRule);
    }
}
