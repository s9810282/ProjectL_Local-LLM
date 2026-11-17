using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
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
        internal static Contracts.Combat.CombatEffectsEvent AttachChild(System.Collections.Generic.List<Contracts.Combat.CombatEvent> events,
            Contracts.Combat.CombatEffectsEvent start, AttackOutcomeSnapshot outcome)
        {
            int index = events.IndexOf(start);
            if (index < 0) throw new InvalidOperationException("추가 공격 시작 사건이 없습니다.");
            var occurrence = new Contracts.Combat.CombatEffectsEvent(new[] { start.Traits.Single().WithChildAttack(outcome) }, start.Categories);
            events[index] = occurrence;
            return occurrence;
        }

        internal static AttackHitResult[] CreateHits(CommandAttackLedger ledger) =>
            ledger.HitIds.OrderBy(id => id).Select(id => CreateHit(ledger, id)).ToArray();

        /// <summary>같은 피격 장부를 스킬 사건으로 봉인합니다. 일반 행동 정산이나 반응 공격 예외를 만들지 않습니다.</summary>
        internal static PlayerSkillAttackResult CreatePlayerSkill(CommandAttackPlan plan, CommandAttackLedger ledger,
            int attackOrdinal, IReadOnlyList<TraitActivationSnapshot> traits = null,
            IReadOnlyList<CategoryActivationSnapshot> categories = null, IReadOnlyList<CellCoord> projectilePath = null,
            IReadOnlyDictionary<int, CellCoord> incomingDirections = null, IReadOnlyDictionary<int, CellCoord> impactCells = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            var hits = CreateHits(ledger);
            var breakdowns = hits.ToDictionary(hit => hit.TargetId, hit =>
            {
                var value = ledger.Breakdown(hit.TargetId);
                return new PlayerSkillDamageBreakdown(value.DamageBeforeMatchup, value.MatchupNumerator, value.MatchupDenominator,
                    value.DamageAfterMatchup, value.DamageAfterPhysicalDefense, value.IncomingReduction, value.FinalDamage);
            });
            return new PlayerSkillAttackResult(plan.Actor.EntityId, attackOrdinal, plan.AttackGroup,
                projectilePath ?? plan.Delivery.AffectedCells, hits, ledger.Knockbacks,
                (traits ?? Array.Empty<TraitActivationSnapshot>()).Concat(ledger.Reactions).ToArray(), categories,
                projectilePath, incomingDirections, breakdowns, impactCells);
        }

        private static AttackHitResult CreateHit(CommandAttackLedger ledger, Contracts.Ids.EntityId id)
        {
            var contacts = ledger.Contacts(id);
            var pairs = contacts.Select(c => (c.Role, c.AppliedSplashPercent)).Distinct().ToArray();
            var role = pairs.Length == 1 ? pairs[0].Role : AttackTargetRole.None;
            int percent = pairs.Length == 1 ? pairs[0].AppliedSplashPercent : 100;
            return new AttackHitResult(id, ledger.Before(id), ledger.HitHealth(id).CurrentHealth, role, percent,
                ledger.NormalizationLoss(id), ledger.Recovery(id), contacts, ledger.Reconstructed(id), ledger.IsObstacle(id));
        }

        internal static AttackResult Create(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            UnitActionUseSettlementSnapshot cost, TraitActivationSnapshot[] traitActivations, CategoryActivationSnapshot[] categoryActivations,
            out AttackPreview preview, bool reaction = false, AttackPullSnapshot pull = null, AttackSweepSnapshot sweep = null)
        {
            using var work = BeginCreate(plan, world, ledger, cost, traitActivations, categoryActivations, reaction, pull, sweep);
            while (!work.IsComplete) work.Step();
            preview = work.Preview;
            return work.Result;
        }

        internal static ResultWork BeginCreate(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            UnitActionUseSettlementSnapshot cost, TraitActivationSnapshot[] traitActivations, CategoryActivationSnapshot[] categoryActivations,
            bool reaction = false, AttackPullSnapshot pull = null, AttackSweepSnapshot sweep = null) =>
            new ResultWork(plan, world, ledger, cost, traitActivations, categoryActivations, reaction, pull, sweep);

        /// <summary>대상별 결과를 순서대로 조립하고 공격 결과와 미리보기를 함께 봉인한다.</summary>
        internal sealed class ResultWork : IDisposable
        {
            private IEnumerator<int> _steps;
            private AttackResult _result;
            private AttackPreview _preview;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal bool IsComplete
            {
                get
                {
                    RequireActive();
                    return _complete;
                }
            }
            internal long WorkVersion
            {
                get
                {
                    RequireActive();
                    return _version;
                }
            }
            internal AttackResult Result
            {
                get
                {
                    RequireComplete();
                    return _result;
                }
            }
            internal AttackPreview Preview
            {
                get
                {
                    RequireComplete();
                    return _preview;
                }
            }

            internal ResultWork(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
                UnitActionUseSettlementSnapshot cost, TraitActivationSnapshot[] traitActivations, CategoryActivationSnapshot[] categoryActivations,
                bool reaction, AttackPullSnapshot pull, AttackSweepSnapshot sweep)
            {
                if (plan == null) throw new ArgumentNullException(nameof(plan));
                if (world == null) throw new ArgumentNullException(nameof(world));
                if (ledger == null) throw new ArgumentNullException(nameof(ledger));
                _steps = Build(plan, world, ledger, cost, traitActivations, categoryActivations, reaction, pull, sweep).GetEnumerator();
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                using var sample = CoreProfiling.Sample(CoreProfileSection.AttackResultConstruction);
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                    }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = default;
                    _preview = default;
                    throw;
                }
            }

            private IEnumerable<int> Build(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
                UnitActionUseSettlementSnapshot cost, TraitActivationSnapshot[] traitActivations, CategoryActivationSnapshot[] categoryActivations,
                bool reaction, AttackPullSnapshot pull, AttackSweepSnapshot sweep)
            {
                var input = plan.Input;
                var structures = plan.Selection.EligibleStructures.ToArray();
                yield return 0;
                var orderedIds = new SortedSet<Contracts.Ids.EntityId>();
                foreach (var id in ledger.HitIds)
                {
                    orderedIds.Add(id);
                    yield return 0;
                }
                var results = new List<AttackHitResult>();
                foreach (var id in orderedIds)
                {
                    results.Add(CreateHit(ledger, id));
                    yield return 0;
                }
                var result = AttackResult.FromFacts(plan.Actor.EntityId, plan.Selection.SelectedTargetId, plan.Direction, cost,
                    plan.Delivery.AffectedCells.ToArray(), results.ToArray(), plan.RequestedCells,
                    traitActivations: traitActivations.Concat(ledger.Reactions).ToArray(), categoryActivations: categoryActivations,
                    blockedContacts: world.BlockedContacts, piercingShot: plan.Delivery.PiercingShot, attackGroup: plan.AttackGroup,
                    reaction: reaction, pull: pull, knockbacks: ledger.Knockbacks, sweep: sweep);
                yield return 0;
                var targetPreviews = new List<AttackTargetPreview>();
                foreach (var hit in results)
                {
                    var original = input.Characters.SingleOrDefault(c => c.EntityId == hit.TargetId);
                    var occupied = original?.OccupiedCells ?? structures.Single(s => s.EntityId == hit.TargetId).OccupiedCells;
                    var damage = ledger.Breakdown(hit.TargetId);
                    int expectedDamage = ledger.Expected(hit.TargetId);
                    int directDamage = ledger.Direct(hit.TargetId);
                    targetPreviews.Add(new AttackTargetPreview(hit, expectedDamage, occupied.ToArray(), damage.DamageBeforeMatchup,
                        damage.MatchupNumerator, damage.MatchupDenominator, damage.IncomingReduction,
                        secondaryDamage: checked(expectedDamage - directDamage)));
                    yield return 0;
                }
                var movementTargets = new SortedSet<Contracts.Ids.EntityId>();
                foreach (var id in ledger.PreHitTargets.Concat(ledger.Knockbacks.Select(k => k.TargetId)).Concat(ledger.CollisionVictims)
                    .Concat(pull == null ? Array.Empty<Contracts.Ids.EntityId>() : new[] { pull.TargetId })
                    .Concat(sweep?.CapturedTargetIds ?? Array.Empty<Contracts.Ids.EntityId>()))
                {
                    movementTargets.Add(id);
                    yield return 0;
                }
                foreach (var id in movementTargets)
                {
                    yield return 0;
                    if (results.Any(hit => hit.TargetId == id)) continue;
                    var before = input.Characters.Single(c => c.EntityId == id);
                    // 넉백·작렬 부딪힘 보조 행의 체력도 직접 피격 시점에 고정하며, Pull 사전 이동 경계는 유지한다.
                    var after = ledger.Knockbacks.Any(k => k.TargetId == id) || ledger.CollisionVictims.Contains(id)
                        ? ledger.HitHealth(id) : world.Characters[id].Health;
                    int damage = ledger.Expected(id);
                    targetPreviews.Add(new AttackTargetPreview(id, damage, before.Health.CurrentHealth, before.OccupiedCells.ToArray(),
                        0, 1, 1, 0, role: id == plan.Selection.SelectedTargetId ? AttackTargetRole.Primary : AttackTargetRole.Additional,
                        healthNormalizationLoss: ledger.NormalizationLoss(id), secondaryDamage: damage,
                        targetHealthAfter: after.CurrentHealth, isDirectHit: false, healthRecovery: ledger.Recovery(id),
                        targetReconstructs: ledger.Reconstructed(id))
                        .WithContacts(Array.Empty<AttackHitContactSnapshot>()));
                }
                var actor = world.Characters[plan.Actor.EntityId];
                // 실제 반응 공격(포효)은 미리보기 값이 필요 없다. 공개 미리보기로 돌린 반응(포효 예고)만 만든다(포효 예고 계획 TP-3).
                if (!reaction || plan.PublicPreview)
                {
                    var pillarFalls = plan.PublicPreview
                        ? world.Events.OfType<CombatPillarFallEvent>().Select(fall => new PillarFallPreview(fall, world.Events)).ToArray()
                        : null;
                    _preview = AttackPreview.FromFacts(result, targetPreviews.ToArray(), actor.Stats.TraitStatuses.ToArray(),
                        actor.Stats.Category, pillarFalls);
                }
                _result = result;
                yield return 0;
            }

            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(ResultWork));
                if (_faulted) throw new InvalidOperationException("실패한 공격 결과 조립은 다시 진행할 수 없습니다.");
            }

            private void RequireComplete()
            {
                RequireActive();
                if (!_complete) throw new InvalidOperationException("공격 결과 조립이 끝나지 않았습니다.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = default;
                _preview = default;
            }
        }
    }
}
