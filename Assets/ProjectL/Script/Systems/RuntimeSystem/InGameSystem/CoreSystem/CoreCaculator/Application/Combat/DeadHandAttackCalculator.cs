using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>쓸기 대상 포착과 행별 이동을 소유하며 기존 강제 이동·도착 정산을 사용합니다.</summary>
    internal static class DeadHandAttackCalculator
    {
        internal static DeadHandAttackEvaluation Prepare(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, ResolvedAttackSelection selection, EffectAttackSweepContribution contribution,
            MovementEnvironmentFacts environment = null, AttackAim? aim = null, AttackCellSelection sweepCells = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (contribution.Origin.OwnerInstanceId != actor.EntityId.Value)
                throw new ArgumentException("쓸기 기여의 소유자가 공격자와 다릅니다.", nameof(contribution));
            var ids = new HashSet<Contracts.Ids.EntityId>(selection.Targets.Select(t => t.Key));
            var targets = selection.EligibleCharacters.Where(c => ids.Contains(c.EntityId) &&
                new HostilityPolicy().AreHostile(actor.Owner, c.Owner)).Select(c => c.EntityId);
            var chosen = sweepCells ?? selection.Cells;
            // 도착 경계는 모양의 끝이다. 하붑의 지정 금지는 주 칸 지정만 막으며 경계를 줄이지 않는다(방향 지정과 같음, PD-4).
            var area = profile.Method.ChoiceMode == TargetingChoiceMode.Single
                ? CombatAttackQueries.BasicShapeCells(input, actor, profile, environment ?? input.State.Environment)
                : chosen.DesignatedCells.ToArray();
            return new DeadHandAttackEvaluation(input, contribution,
                AttackSweepGeometry.ResolveFacing(aim ?? input.Aim, actor.Anchor.Value, actor.Rotation.Value),
                AttackSweepGeometry.ResolveSide(input.SweepSide), area.Concat(chosen.AdditionalCells.ToArray()), targets);
        }

        /// <summary>살아 있는 최초 대상의 현재 사실만 넘깁니다. 이동한 위치에서 범위·가림을 다시 검사하지 않습니다.</summary>
        internal static ResolvedAttackSelection AfterMovement(ResolvedAttackSelection original, CommandAttackProjection world)
        {
            var targets = original.Targets.Where(t => world.Characters.TryGetValue(t.Key, out var current) &&
                !current.Health.IsDead && current.Area == CharacterRuntimeArea.Field).ToArray();
            return new ResolvedAttackSelection(original.Cells, original.ReachableCells, targets,
                targets.Select(t => world.Characters[t.Key]).ToArray(), Array.Empty<StructureCombatFacts>(), original.Policy,
                original.SelectedTargetId);
        }

        // 충돌과 면역은 정밀 계산이 판단한다. 평가에서는 보드 안에서 닿을 수 있는 도착 칸을 넉넉히 잡는다.
        internal static IEnumerable<int> PotentialEntrySteps(CombatCommandInput input, DeadHandAttackEvaluation evaluation,
            ISet<CellCoord> reach, ICollection<CharacterCombatFacts> movers)
        {
            evaluation.RequireSource(input);
            var step = AttackSweepGeometry.Step(evaluation.Facing, evaluation.Side);
            foreach (var mover in input.Characters)
            {
                yield return 0;
                if (!evaluation.TargetIds.Contains(mover.EntityId)) continue;
                movers.Add(mover);
                var start = mover.Anchor.Value;
                var destination = AttackSweepGeometry.Destination(evaluation.AreaCells, start, step);
                for (var entry = start + step; input.State.IsInside(entry); entry += step)
                {
                    reach.Add(entry);
                    if (entry == destination) break;
                    yield return 0;
                }
            }
        }

        internal static IEnumerable<string> ExecuteSteps(DeadHandAttackEvaluation evaluation, CommandAttackPlan plan,
            CommandAttackProjection world, CommandAttackLedger ledger, MovementProjection arrival, Action<AttackSweepSnapshot> completed)
        {
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            evaluation.RequireSource(plan.Input);
            var step = AttackSweepGeometry.Step(evaluation.Facing, evaluation.Side);
            var rowAxis = new CellCoord(-step.Y, step.X);
            var original = plan.Input.Characters.ToDictionary(c => c.EntityId);
            var ordered = evaluation.TargetIds.OrderBy(id => AttackSweepGeometry.Project(original[id].Anchor.Value, rowAxis))
                .ThenByDescending(id => AttackSweepGeometry.Project(original[id].Anchor.Value, step)).ThenBy(id => id).ToArray();
            int headerIndex = world.Events.Count;
            var outcomes = new List<AttackSweepTargetSnapshot>();
            bool interrupted = false;
            yield return "SweepOrder";
            foreach (var id in ordered)
            {
                var start = original[id].Anchor.Value;
                var endpoint = AttackSweepGeometry.Destination(evaluation.AreaCells, start, step);
                var target = world.Characters[id];
                interrupted |= arrival.Terminal || world.Characters[plan.Actor.EntityId].Health.IsDead ||
                    world.Characters[plan.Actor.EntityId].Area != CharacterRuntimeArea.Field;
                if (interrupted)
                {
                    outcomes.Add(new AttackSweepTargetSnapshot(id, start, endpoint, Array.Empty<CellCoord>(), target.Anchor,
                        AttackSweepStop.NotProcessed));
                    yield return "SweepNotProcessed";
                    continue;
                }
                if (target.Health.IsDead || target.Area != CharacterRuntimeArea.Field || target.Anchor != start)
                {
                    outcomes.Add(new AttackSweepTargetSnapshot(id, start, endpoint, Array.Empty<CellCoord>(), target.Anchor,
                        AttackSweepStop.Interrupted));
                    yield return "SweepTargetInterrupted";
                    continue;
                }
                if (KnockbackImmunityCheck.IsImmune(plan.Input, target, out _))
                {
                    outcomes.Add(new AttackSweepTargetSnapshot(id, start, endpoint, Array.Empty<CellCoord>(), start, AttackSweepStop.Immune));
                    yield return "SweepImmune";
                    continue;
                }
                int distance = checked((int)(Math.Abs((long)endpoint.X - start.X) + Math.Abs((long)endpoint.Y - start.Y)));
                ForcedDisplacementPath path = null;
                foreach (var phase in AttackForcedMovement.ExecuteUnitSteps(plan, world, ledger, arrival, id, step, distance,
                    deferLastObjects: false, publishMovedEvents: true, completed: value => path = value,
                    movementCause: new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.DeadHand)))
                    yield return phase;
                target = world.Characters[id];
                if (path.Steps.Count > 0 && path.Stop != ForcedDisplacementStop.Interrupted && !target.Health.IsDead &&
                    !arrival.Terminal && target.Anchor == path.Steps[path.Steps.Count - 1])
                    foreach (var phase in arrival.ResolveForcedArrivalSteps(id, loss => ledger.AddNormalizationLoss(id, loss)))
                        yield return phase;
                target = world.Characters[id];
                var stop = path.Stop == ForcedDisplacementStop.Completed ? AttackSweepStop.Completed
                    : path.Stop == ForcedDisplacementStop.Blocked ? AttackSweepStop.Blocked : AttackSweepStop.Interrupted;
                if (target.Health.IsDead || arrival.Terminal || target.Anchor != (path.Steps.Count == 0 ? start : path.Steps[path.Steps.Count - 1]))
                    stop = AttackSweepStop.Interrupted;
                outcomes.Add(new AttackSweepTargetSnapshot(id, start, endpoint, path.Steps, target.Anchor, stop));
                yield return "SweepTargetComplete";
            }
            interrupted |= arrival.Terminal || world.Characters[plan.Actor.EntityId].Health.IsDead ||
                world.Characters[plan.Actor.EntityId].Area != CharacterRuntimeArea.Field;
            ledger.CapturePreHitDamage(world.Damage.CaptureAppliedDamage());
            var result = new AttackSweepSnapshot(evaluation.Contribution.Origin, plan.Actor.EntityId, evaluation.Facing, evaluation.Side,
                evaluation.AreaCells, evaluation.TargetIds, outcomes, interrupted);
            world.Events.Insert(headerIndex, new CombatSweepEvent(result, plan.AttackGroup));
            completed(result);
            yield return "SweepComplete";
        }
    }
}
