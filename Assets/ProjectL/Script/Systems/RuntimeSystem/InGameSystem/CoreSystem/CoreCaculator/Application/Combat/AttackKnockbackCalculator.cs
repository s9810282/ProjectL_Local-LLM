using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 공격의 밀치기(S8)와 그 강제 도착(S12)을 처리한다. 모든 직접 피해와 사망을 고정한 뒤 ID 순서로 거리 N만큼 한 칸씩 밀고,
    /// 지나가는 칸의 지형·가시밭·오브젝트를 즉시 정산한다. 요청 거리 N번째 칸의 오브젝트 도착은 비용 정산과 피격 체력 고정 뒤에 따로 처리한다.
    /// </summary>
    internal readonly struct ResolvedAttackDisplacement
    {
        internal EntityId TargetId { get; }
        internal int CompletedSteps { get; }
        internal ResolvedAttackDisplacement(EntityId targetId, int completedSteps)
        { TargetId = targetId; CompletedSteps = completedSteps; }
    }

    internal static class AttackKnockbackCalculator
    {
        internal static IReadOnlyList<ResolvedAttackDisplacement> Resolve(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, MovementProjection arrival)
        {
            var arrivals = new List<ResolvedAttackDisplacement>();
            foreach (var _ in ResolveSteps(plan, world, ledger, arrival, arrivals)) { }
            return arrivals;
        }

        /// <summary>
        /// 밀치기를 처리하고 살아서 도착한 대상을 기록한다. 오브젝트 도착은 <see cref="ResolveArrivalsSteps"/>가 처리한다.
        /// 밀려고 한 대상마다 넉백 요약을 <paramref name="ledger"/>에 남긴다(설계 2026-10-27 §4.3).
        /// </summary>
        internal static IEnumerable<string> ResolveSteps(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, MovementProjection arrival, List<ResolvedAttackDisplacement> forcedArrivals)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
            var input = plan.Input;
            var actor = plan.Actor;
            var projected = world.Characters;
            var knockback = plan.Engine.EvaluateKnockback(plan.Bindings);
            int distance = knockback.Contributions.Count == 0 ? 0 : knockback.Contributions.Max(c => c.DistanceCells);
            if (distance == 0) yield break;
            // 가장 긴 거리의 기여가 여럿이면 부딪힘 값이 있는 기여(작렬)를 출처로 삼아 View가 작렬 연출을 고를 수 있게 한다.
            var source = knockback.Contributions.Where(contribution => contribution.DistanceCells == distance)
                .OrderByDescending(contribution => contribution.Collision.HasValue).First();
            var movementCause = new CombatMovementCause(CombatMovementAgency.Forced,
                source.Origin.Kind == EffectOwnerKind.Trait ? (TraitKey?)source.Origin.DefinitionKey : null);
            int activationIndex = world.Events.Count;
            var outcomes = new List<TraitActivationSnapshot>();
            void Outcome(EntityId id, EntityId owner, TraitKey key, TraitActivationKind kind, int magnitude, IReadOnlyList<CellCoord> cells)
            {
                var delivered = plan.Delivery.Hits.FirstOrDefault(hit => hit.TargetId == id);
                var physical = delivered.TargetId.IsEmpty ? actor.Anchor : delivered.ImpactOrigin;
                var resultOrigin = new CombatDamageOrigin(physical, actor.EntityId, plan.AttackGroup);
                outcomes.Add(new TraitActivationSnapshot(owner, id, key, kind, magnitude, targetCells: cells, resultOrigin: resultOrigin));
            }
            bool impactCenter = knockback.Contributions[0].Mode == EffectKnockbackMode.ImpactCenter;
            var origin = actor.Anchor.Value;
            IEnumerable<EntityId> candidates = ledger.Roles.Keys;
            if (impactCenter)
            {
                // 벽 등으로 전달 자체가 없으면 중심도 요구하지 않는다. 피해량과 무관하게 직접 전달된 대상만 후보로 삼는다.
                if (plan.Delivery.Hits.Count == 0) yield break;
                origin = plan.Selection.ReachableCells.PrimaryCell ??
                    throw new InvalidOperationException("충격 볼트가 전달된 공격에는 충돌 중심이 필요합니다.");
                candidates = plan.Delivery.Hits.Select(hit => hit.TargetId);
            }
            // 희생이 대신 맞았으면 수호자를 민다. 수호자(Heavy)는 기본 특성 굳건한 의지로 막으므로 보통은 아무도 밀리지 않는다.
            candidates = ledger.ActualRecipients(candidates);
            foreach (var id in candidates.Distinct().OrderBy(id => id).ToArray())
            {
                // 앞선 대상의 이동 중 목표가 달성되거나 메인이 죽으면 이후 대상의 이동·지형 피해도 취소한다.
                if (arrival.Terminal) break;
                yield return "KnockbackCandidate";
                if (!projected.TryGetValue(id, out var victim))
                {
                    if (impactCenter) continue;
                    (ForcedDisplacementPath Path, CellCoord Start, CellCoord Step)? obstacle = null;
                    foreach (var phase in KnockbackObstacleSteps(arrival, id, actor.Anchor.Value, distance, movementCause,
                        (path, start, step) => obstacle = (path, start, step))) yield return phase;
                    if (obstacle == null) continue;
                    Record(ledger, id, AttackKnockbackTargetKind.Structure, obstacle.Value.Start, obstacle.Value.Step, obstacle.Value.Path);
                    int steps = obstacle.Value.Path.Steps.Count;
                    if (source.Origin.Kind == EffectOwnerKind.Trait)
                        Outcome(id, new EntityId(source.Origin.OwnerInstanceId), (TraitKey)source.Origin.DefinitionKey,
                            TraitActivationKind.KnockbackApplied, steps, new[] { obstacle.Value.Start }.Concat(obstacle.Value.Path.Steps).Distinct().ToArray());
                    if (steps > 0) forcedArrivals.Add(new ResolvedAttackDisplacement(id, steps));
                    continue;
                }
                if (victim.Health.IsDead)
                {
                    // 직접 피해로 죽은 사망 특성 보유자는 시체를 옮기지 않고 넉백 끝 칸만 사망 위치로 정한다(적 전용 특성 설계 D5·§6.3).
                    if (!plan.PublicPreview) DisplaceCorpse(plan, world, arrival, id, origin, impactCenter, distance);
                    continue;
                }
                CellCoord step = default;
                if (impactCenter)
                {
                    if (!ImpactBoltTraitRule.TryResolveStep(victim, origin, out step)) continue;
                }
                else
                {
                    // 면역 기록에도 방향이 필요해 면역 검사 앞에서 구한다. 순수 계산이라 결과는 같다.
                    step = ForcedDisplacementGeometry.ResolveBranchStep(origin, victim.Anchor.Value);
                }
                var start = victim.Anchor.Value;
                // 대상의 공격 넉백 면역만 판정한다. 피해는 이미 적용됐으며 다른 강제 이동 규칙은 바꾸지 않는다.
                if (KnockbackImmunityCheck.IsImmune(input, victim, out bool preventedBySteadfastWill))
                {
                    if (preventedBySteadfastWill)
                        Outcome(id, victim.EntityId, TraitKey.SteadfastWill, TraitActivationKind.KnockbackPrevented, distance, victim.OccupiedCells);
                    ledger.RecordKnockback(new AttackKnockbackSnapshot(id, AttackKnockbackTargetKind.Character, start, step,
                        distance, Array.Empty<CellCoord>(), AttackKnockbackStop.Immune));
                    continue;
                }
                ForcedDisplacementPath path = null;
                foreach (var phase in AttackForcedMovement.ExecuteUnitSteps(plan, world, ledger, arrival, id, step, distance,
                    deferLastObjects: true, publishMovedEvents: true, completed: value => path = value, movementCause: movementCause))
                    yield return phase;
                var collision = CrashImpactCollision.Find(plan, world, arrival, knockback, id, start, step, path);
                Record(ledger, id, AttackKnockbackTargetKind.Character, start, step, path, collision);
                if (source.Origin.Kind == EffectOwnerKind.Trait)
                    Outcome(id, new EntityId(source.Origin.OwnerInstanceId), (TraitKey)source.Origin.DefinitionKey,
                        TraitActivationKind.KnockbackApplied, path.Steps.Count, new[] { start }.Concat(path.Steps).Distinct().ToArray());
                // 막혀 멈춘 곳의 부딪힘 피해(작렬). 부딪힘 값이 없거나 막히지 않았으면 아무것도 하지 않는다.
                foreach (var phase in CrashImpactCollision.ResolveSteps(plan, world, ledger, arrival, knockback, id, collision))
                    yield return phase;
                // 종료 여부는 S12(ResolveArrivals)가 다시 확인한다.
                if (path.Steps.Count != 0 && path.Stop != ForcedDisplacementStop.Interrupted &&
                    !projected[id].Health.IsDead && projected[id].Anchor == path.Steps[path.Steps.Count - 1])
                    forcedArrivals.Add(new ResolvedAttackDisplacement(id, path.Steps.Count));
            }
            foreach (var group in outcomes.GroupBy(value => new { value.SourceUnitId, value.TraitKey, value.TraitActivationKind,
                value.ResultOrigin.PhysicalCell, value.ResultOrigin.CreditedCharacterId, value.ResultOrigin.AttackGroup }))
            {
                var first = group.First();
                var magnitudes = group.Select(value => value.Magnitude).Distinct().ToArray();
                var occurrence = new TraitActivationSnapshot(first.SourceUnitId, null, first.TraitKey, first.TraitActivationKind,
                    magnitudes.Length == 1 ? magnitudes[0] : 0, group.SelectMany(value => value.TargetIds).Distinct().OrderBy(id => id).ToArray(),
                    group.SelectMany(value => value.TargetCells).Distinct().ToArray(), first.ResultOrigin);
                world.Events.Insert(activationIndex++, new CombatEffectsEvent(new[] { occurrence }, Array.Empty<CategoryActivationSnapshot>()));
                yield return "KnockbackEvidence";
            }
        }

        /// <summary>살아서 밀려난 대상의 도착 칸 오브젝트와 강제 도착을 ID 순서로 처리한다(S12). 전투가 끝나면 멈춘다.</summary>
        internal static IEnumerable<string> ResolveArrivalsSteps(CommandAttackPlan plan, CommandAttackProjection world, MovementProjection arrival,
            IReadOnlyList<ResolvedAttackDisplacement> forcedArrivals)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
            if (forcedArrivals == null) throw new ArgumentNullException(nameof(forcedArrivals));
            foreach (var displacement in forcedArrivals.OrderBy(d => d.TargetId))
            {
                var id = displacement.TargetId;
                if (arrival.Terminal) break;
                yield return "KnockbackArrivalCandidate";
                if (world.Characters.TryGetValue(id, out var character))
                {
                    if (character.Health.IsDead) continue;
                    foreach (var phase in MovementEnvironmentCalculator.ResolveObjectsAtSteps(arrival, id, character.Anchor.Value, plan.Rules))
                        yield return phase;
                    foreach (var phase in arrival.ResolveForcedArrivalSteps(id, _ => { })) yield return phase;
                }
            }
        }

        /// <summary>
        /// 사망 특성 보유자의 시체가 밀려날 경로를 계산만 한다. 죽은 캐릭터는 위치가 비어 있어 실제로 옮기거나 이동 사건·넉백 장부를 남기지 않는다.
        /// 진입은 산 대상과 같은 판정이고 칸 효과(낙사·가시·지뢰 등)는 정산하지 않는다. 면역이면 그 자리다.
        /// 끝 칸을 사망 위치로 덮어써 영혼과 사망 특성이 그 칸을 쓰고, View를 위해 시작 → 끝 칸 목록을 발동 사건으로 남긴다.
        /// </summary>
        private static void DisplaceCorpse(CommandAttackPlan plan, CommandAttackProjection world, MovementProjection arrival,
            EntityId id, CellCoord origin, bool impactCenter, int distance)
        {
            var input = plan.Input;
            var corpse = CommandDeathSettlement.PreDeathFact(input, world.Events, id);
            if (corpse.Stats.TraitStatuses.Length == 0 || !corpse.Anchor.HasValue) return;
            var profile = CombatCommandCalculator.RequiredProfile(input, corpse);
            var deathTraits = plan.Engine.EvaluateDeathTraits(CombatCommandEffects.Capture(corpse, profile));
            if (!deathTraits.HasContribution) return;
            var start = world.DeathAnchors.TryGetValue(id, out var anchor) ? anchor : corpse.Anchor.Value;
            CellCoord step;
            if (impactCenter)
            {
                if (!ImpactBoltTraitRule.TryResolveStep(corpse, origin, out step)) return;
            }
            else step = ForcedDisplacementGeometry.ResolveBranchStep(origin, start);
            if (KnockbackImmunityCheck.IsImmune(input, corpse, out _)) return;
            var cells = new List<CellCoord> { start };
            var current = start;
            for (int i = 0; i < distance; i++)
            {
                var next = current + step;
                if (!AttackForcedMovement.CanEnterUnit(input, arrival.Environment, world.Characters.Values, world.Structures.Values,
                    id, current, next)) break;
                cells.Add(next);
                current = next;
            }
            if (cells.Count == 1) return;
            world.DeathAnchors[id] = current;
            var key = deathTraits.SelfDestructPercent > 0 ? TraitKey.SelfDestruct : TraitKey.Fear;
            CombatCommandEvents.Trait(world.Events, id, key, TraitActivationKind.CorpseDisplaced, cells.Count - 1, cells: cells);
        }

        /// <summary>내부 멈춤 이유를 공개 넉백 요약으로 바꾸는 유일한 곳이다.</summary>
        private static void Record(CommandAttackLedger ledger, EntityId id, AttackKnockbackTargetKind kind, CellCoord start,
            CellCoord step, ForcedDisplacementPath path, AttackKnockbackCollision collision = null)
        {
            var stop = path.Stop switch
            {
                ForcedDisplacementStop.Completed => AttackKnockbackStop.Completed,
                ForcedDisplacementStop.Blocked => AttackKnockbackStop.Blocked,
                ForcedDisplacementStop.Interrupted => AttackKnockbackStop.Interrupted,
                _ => throw new InvalidOperationException("알 수 없는 강제 이동 멈춤 이유입니다: " + path.Stop),
            };
            ledger.RecordKnockback(new AttackKnockbackSnapshot(id, kind, start, step, path.RequestedDistance, path.Steps, stop, collision));
        }

        /// <summary>밀 수 있는 장애물이면 경로와 시작 칸·방향을, 밀 대상이 아니면 null을 돌려준다.</summary>
        private static IEnumerable<string> KnockbackObstacleSteps(MovementProjection projection,
            EntityId id, CellCoord origin, int distance, CombatMovementCause movementCause,
            Action<ForcedDisplacementPath, CellCoord, CellCoord> completed)
        {
            var before = projection.Structure(id);
            if (before == null || before.Health.IsDead || !projection.Bindings.ObstacleEntityIds.Contains(id)) yield break;
            var step = ForcedDisplacementGeometry.ResolveBranchStep(origin, before.Anchor);
            // 다칸 장애물은 footprint 전체를 한 칸씩 옮기며 매 칸 같은 검사를 한다. 장애물에는 유닛용 오브젝트 효과를 적용하지 않는다.
            foreach (var phase in ForcedDisplacementPathCalculator.ExecuteSteps(before.Anchor, step, distance, CanEnter, Enter,
                path => completed(path, before.Anchor, step))) yield return phase;

            IEnumerable<string> CanEnter(CellCoord from, CellCoord to, Action<bool> allowed)
            {
                var structure = projection.Structure(id);
                var offset = to - from;
                var cells = structure.OccupiedCells.Select(c => c + offset).ToArray();
                using var observation = projection.BeginCurrentInput();
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "KnockbackObstacleObservation";
                }
                var current = observation.Result;
                allowed(cells.All(c => current.State.IsInside(c)) &&
                    structure.OccupiedCells.All(c => EnvironmentTerrainRules.CanKnockback(projection.Environment, projection.Bindings, c, c + offset)) &&
                    !current.Characters.Any(c => !c.Health.IsDead && c.OccupiedCells.Intersect(cells).Any()) &&
                    !current.State.Structures.Any(s => s.EntityId != id && !s.Health.IsDead && s.OccupiedCells.Intersect(cells).Any()));
                yield return "KnockbackObstacleAdmission";
            }

            IEnumerable<string> Enter(CellCoord from, CellCoord to, bool last, Action<bool> continued)
            {
                var structure = projection.Structure(id);
                var offset = to - from;
                var cells = structure.OccupiedCells.Select(c => c + offset).ToArray();
                projection.MergeStructure(new StructureCombatFacts(id, structure.Definition, structure.Health, to,
                    structure.Rotation, cells, structure.Owner), movementCause);
                yield return "KnockbackObstacleRelocation";
                foreach (var cell in cells.OrderBy(c => c.Y).ThenBy(c => c.X))
                {
                    if (projection.ResolveForcedTerrainEntry(id, cell)) yield break;
                    yield return "KnockbackObstacleTerrain";
                }
                continued(true);
            }
        }

    }
}
