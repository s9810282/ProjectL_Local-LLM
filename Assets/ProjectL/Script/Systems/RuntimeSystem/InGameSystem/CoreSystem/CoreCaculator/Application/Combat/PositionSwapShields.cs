using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class PositionSwapShields
    {
        internal static int Amount(CombatCommandInput input, CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) =>
            CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor, profileReads))
                .Select(b => b.Rule).OfType<PositionSwapEffectRule>().Single().ShieldAmount;

        internal static CharacterCombatFacts Add(CharacterCombatFacts recipient, CharacterCombatFacts source, int amount)
        {
            var origin = TraitEffectOrigin.Create(source.EntityId, TraitKey.PositionSwap);
            var prior = recipient.StatusEffects.Entries.OfType<ShieldStatus>().SingleOrDefault(s => s.Origin == origin);
            var grant = new ShieldStatus(origin, checked((prior?.RemainingBudget ?? 0) + amount), true, 0, source.Owner);
            return recipient.WithStatusEffects(ShieldStatusRules.Restore(recipient.StatusEffects, origin, grant));
        }

        // 이동 결과의 지급 근거는 실제 교대 쌍이다. 피해 흡수·사망·회수로 이후 잔량이 줄어들 수 있다.
        internal static void ValidateMove(CombatCommandTransition move, IReadOnlyList<CharacterCombatFacts> published)
        {
            if (!move.Accepted || move.Input.Kind != CombatCommandKind.Move) return;
            var pair = move.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Moved && e.MovementCause?.TraitKey == TraitKey.PositionSwap).ToArray();
            var required = new Dictionary<EntityId, CharacterCombatFacts>();
            var granted = new HashSet<EntityId>();
            CharacterCombatFacts actor = null;
            int amount = 0;
            if (pair.Length > 0)
            {
                if (move.Input.Path == null || pair.Length != 2 || pair[0].MovementCause.Agency != CombatMovementAgency.Voluntary ||
                    pair[1].MovementCause.Agency != CombatMovementAgency.Forced || pair[0].Final.EntityId != move.Input.ActorId ||
                    pair[0].Final.EntityId == pair[1].Final.EntityId || pair[0].Final.Owner != pair[1].Final.Owner ||
                    pair[0].Final.Anchor != move.Input.Path.Endpoint || pair[1].Expected.Anchor != pair[0].Final.Anchor ||
                    pair[1].Final.Anchor != move.Input.Path.Origin)
                    throw new InvalidOperationException("교대 실드의 양쪽 이동 근거가 올바르지 않습니다.");
                actor = pair[0].Final;
                amount = Amount(move.Input, actor);
                if (amount > 0)
                {
                    foreach (var e in pair) required.Add(e.Final.EntityId, e.Final);
                }
            }
            foreach (var e in move.Events.OfType<CombatTraitStateEvent>())
            {
                if (!IncreasesSwapShield(e.Expected, e.Final)) continue;
                if (!required.TryGetValue(e.Final.EntityId, out var recipient) || !granted.Add(recipient.EntityId) ||
                    !recipient.SameValues(e.Expected) || !Add(recipient, actor, amount).SameValues(e.Final))
                    throw new InvalidOperationException("교대 실드 지급 대상 또는 지급량이 실제 교대와 다릅니다.");
            }
            if (granted.Count != required.Count)
                throw new InvalidOperationException("교대 실드의 지급 근거가 누락됐습니다.");
            if (granted.Count > 0)
            {
                var origin = TraitEffectOrigin.Create(actor.EntityId, TraitKey.PositionSwap);
                foreach (var id in granted)
                {
                    var last = move.Events.Select(e => e is CombatCharacterEvent c ? c.Final :
                        (e as CombatTraitStateEvent)?.Final).Last(c => c?.EntityId == id);
                    var recorded = last.StatusEffects.Entries.OfType<ShieldStatus>().SingleOrDefault(s => s.Origin == origin);
                    var actual = published.Single(c => c.EntityId == id).StatusEffects.Entries.OfType<ShieldStatus>()
                        .SingleOrDefault(s => s.Origin == origin);
                    if (recorded == null ? actual != null : !recorded.SameValues(actual))
                        throw new InvalidOperationException("교대 실드의 마지막 사건과 게시할 잔량이 다릅니다.");
                }
            }
            foreach (var change in move.Characters.Characters)
            {
                if (!IncreasesSwapShield(change.Expected, change.Final)) continue;
                if (!required.TryGetValue(change.Final.EntityId, out var recipient) ||
                    IncreasesSwapShield(Add(recipient, actor, amount), change.Final))
                    throw new InvalidOperationException("게시할 실드 잔량이 해당 교대의 지급 범위를 벗어났습니다.");
            }
        }

        private static bool IncreasesSwapShield(CharacterCombatFacts before, CharacterCombatFacts after) =>
            after.StatusEffects.Entries.OfType<ShieldStatus>().Any(s => s.Origin.Value.Kind == EffectOwnerKind.Trait &&
                s.Origin.Value.DefinitionKey == (int)TraitKey.PositionSwap && s.RemainingBudget >
                (before.StatusEffects.Entries.OfType<ShieldStatus>().SingleOrDefault(prior => prior.Origin == s.Origin)?.RemainingBudget ?? 0));
    }
}
