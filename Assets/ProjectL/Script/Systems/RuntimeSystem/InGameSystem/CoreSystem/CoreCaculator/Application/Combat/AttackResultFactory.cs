using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공격 정보·피격 기록·현재 투영으로 공개 공격 결과와 미리보기를 만든다(S14). 상태를 바꾸지 않는다.</summary>
    internal static class AttackResultFactory
    {
        internal static AttackOutcomeSnapshot SingleOutcome(Contracts.Ids.EntityId recipient, Contracts.Ids.EntityId original,
            int before, int after, int recovery, Contracts.Combat.CombatDamageOrigin origin, TraitActivationSnapshot reaction)
        {
            var contact = new AttackHitContactSnapshot(0, original, AttackTargetRole.Designated, 100, origin);
            return new AttackOutcomeSnapshot(new[] { new AttackHitResult(recipient, before, after, AttackTargetRole.Designated,
                100, 0, recovery, new[] { contact }) }, reaction == null ? Array.Empty<TraitActivationSnapshot>() : new[] { reaction });
        }
        internal static void AttachChild(System.Collections.Generic.List<Contracts.Combat.CombatEvent> events,
            Contracts.Combat.CombatEffectsEvent start, AttackOutcomeSnapshot outcome)
        {
            int index = events.IndexOf(start);
            if (index < 0) throw new InvalidOperationException("추가 공격 시작 사건이 없습니다.");
            events[index] = new Contracts.Combat.CombatEffectsEvent(new[] { start.Traits.Single().WithChildAttack(outcome) }, start.Categories);
        }

        internal static AttackHitResult[] CreateHits(CommandAttackLedger ledger) => ledger.HitIds.OrderBy(id => id).Select(id =>
        {
            var contacts = ledger.Contacts(id);
            var pairs = contacts.Select(c => (c.Role, c.AppliedSplashPercent)).Distinct().ToArray();
            var role = pairs.Length == 1 ? pairs[0].Role : AttackTargetRole.None;
            int percent = pairs.Length == 1 ? pairs[0].AppliedSplashPercent : 100;
            return new AttackHitResult(id, ledger.Before(id), ledger.HitHealth(id).CurrentHealth, role, percent,
                ledger.NormalizationLoss(id), ledger.Recovery(id), contacts);
        }).ToArray();

        internal static AttackResult Create(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            UnitActionUseSettlementSnapshot cost, TraitActivationSnapshot[] traitActivations, CategoryActivationSnapshot[] categoryActivations,
            out AttackPreview preview, bool reaction = false, AttackPullSnapshot pull = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            var input = plan.Input;
            var structures = plan.Selection.EligibleStructures.ToArray();
            var results = CreateHits(ledger);
            var result = AttackResult.FromFacts(plan.Actor.EntityId, plan.Selection.SelectedTargetId, plan.Direction, cost,
                plan.Delivery.AffectedCells.ToArray(),
                results, plan.RequestedCells, traitActivations: traitActivations.Concat(ledger.Reactions).ToArray(), categoryActivations: categoryActivations,
                blockedContacts: world.BlockedContacts, piercingShot: plan.Delivery.PiercingShot, attackGroup: plan.AttackGroup, reaction: reaction, pull: pull,
                knockbacks: ledger.Knockbacks);
            var targetPreviews = results.Select(hit =>
            {
                var original = input.Characters.SingleOrDefault(c => c.EntityId == hit.TargetId);
                var occupied = original?.OccupiedCells ?? structures.Single(s => s.EntityId == hit.TargetId).OccupiedCells;
                var damage = ledger.Breakdown(hit.TargetId);
                int expectedDamage = ledger.Expected(hit.TargetId);
                int directDamage = ledger.Direct(hit.TargetId);
                return new AttackTargetPreview(hit, expectedDamage, occupied.ToArray(), damage.DamageBeforeMatchup,
                    damage.MatchupNumerator, damage.MatchupDenominator,
                    damage.IncomingReduction,
                    secondaryDamage: checked(expectedDamage - directDamage));
            }).ToList();
            var movementTargets = ledger.PreHitTargets.Concat(ledger.Knockbacks.Select(k => k.TargetId)).Concat(pull == null ? Array.Empty<Contracts.Ids.EntityId>() : new[] { pull.TargetId });
            foreach (var id in movementTargets.Distinct().Where(id => results.All(hit => hit.TargetId != id)).OrderBy(id => id))
            {
                var before = input.Characters.Single(c => c.EntityId == id);
                // 넉백 보조 행도 직접 피격과 같은 시점의 체력을 사용합니다. Pull 사전 이동의 기존 경계는 유지합니다.
                var after = ledger.Knockbacks.Any(k => k.TargetId == id) ? ledger.HitHealth(id) : world.Characters[id].Health;
                int damage = ledger.Expected(id);
                targetPreviews.Add(new AttackTargetPreview(id, damage, before.Health.CurrentHealth, before.OccupiedCells.ToArray(),
                    0, 1, 1, 0, role: id == plan.Selection.SelectedTargetId ? AttackTargetRole.Primary : AttackTargetRole.Additional,
                    healthNormalizationLoss: ledger.NormalizationLoss(id), secondaryDamage: damage,
                    targetHealthAfter: after.CurrentHealth, isDirectHit: false, healthRecovery: ledger.Recovery(id)).WithContacts(Array.Empty<AttackHitContactSnapshot>()));
            }
            var actor = world.Characters[plan.Actor.EntityId];
            // 비용 없는 반응 공격(포효)은 미리보기 대상이 아니다(G13).
            preview = reaction ? default
                : AttackPreview.FromFacts(result, targetPreviews.ToArray(), actor.Stats.TraitStatuses.ToArray(), actor.Stats.Category);
            return result;
        }
    }
}
