using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>끌어오기 규칙의 결정을 기존 공격 지정·이동 경계에 연결합니다. 영속 상태는 소유하지 않습니다.</summary>
    internal static class PullAttackCalculator
    {
        internal static int SelectionRange(CharacterCombatFacts actor, CombatAttackProfile profile, int basicRange,
            bool includePreHitMovement = true)
        {
            var configuration = Configuration(actor, profile);
            if (basicRange < 0) throw new ArgumentOutOfRangeException(nameof(basicRange));
            return configuration == null || basicRange == 0 || !includePreHitMovement
                ? basicRange
                : PullTraitRule.SelectionRange(basicRange, configuration.ExtraRangeCells);
        }

        private static PullTraitSpec Configuration(CharacterCombatFacts actor, CombatAttackProfile profile)
        {
            var configuration = profile.Definition.Traits.ToArray().OfType<PullTraitSpec>().SingleOrDefault();
            var status = actor.Stats.TraitStatuses.ToArray().SingleOrDefault(t => t.Key == TraitKey.Pull);
            if (configuration == null)
            {
                if (status != null) throw new InvalidOperationException("끌어오기 상태에 대응하는 정의가 없습니다.");
                return null;
            }
            if (status is not PullTraitStatusSnapshot pull || pull.ExtraRangeCells != configuration.ExtraRangeCells)
                throw new InvalidOperationException("끌어오기 정의와 공개 상태의 추가 사거리가 다릅니다.");
            PullTraitRule.ValidateMethod(profile.Method);
            return configuration;
        }

        internal static IEnumerable<string> EvaluateSteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, ResolvedAttackSelection selection, MovementEnvironmentFacts environment, WallGeometry walls,
            Action<PullAttackEvaluation> completed)
        {
            if (Configuration(actor, profile) == null) yield break;
            int basicRange = EnvironmentTerrainRules.AppointRange(input, actor, environment);
            if (basicRange == 0)
            {
                completed(PullAttackEvaluation.Failure(AttackFailureReason.InvalidPrimaryTarget));
                yield break;
            }
            var contact = selection.Cells.PrimaryCell.Value;
            var destination = PullTraitRule.Destination(actor.Anchor.Value, contact, basicRange);
            var target = selection.EligibleCharacters.SingleOrDefault(c => c.EntityId == selection.SelectedTargetId);
            // 구조물에는 기본 사거리의 기존 공격만 허용하며 이동 경로를 만들지 않습니다.
            if (target == null)
            {
                if (destination != contact) completed(PullAttackEvaluation.Failure(AttackFailureReason.OutOfBounds));
                yield break;
            }
            bool damage = PullTraitRule.DealsAttackDamage(actor.Owner, target.Owner);
            if (damage && selection.Policy.ExcludesAdjacentDirectCells &&
                CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, destination))
            {
                completed(PullAttackEvaluation.Failure(AttackFailureReason.AppointCellRejected));
                yield break;
            }
            if (destination == contact)
            {
                completed(damage ? new PullAttackEvaluation(AttackFailureReason.None, target.EntityId, contact, contact,
                    Array.Empty<CellCoord>(), true) : PullAttackEvaluation.Failure(AttackFailureReason.PullNoMovement));
                yield break;
            }
            if (UnitBehaviorRules.IsForcedMovementImmune(input, target))
            {
                completed(PullAttackEvaluation.Failure(AttackFailureReason.PullImmune));
                yield break;
            }
            var step = new CellCoord(Math.Sign(destination.X - contact.X), Math.Sign(destination.Y - contact.Y));
            var path = new List<CellCoord>();
            for (var current = contact; current != destination;)
            {
                var next = current + step;
                // 선택 단계는 공격 시작 환경으로 판정하므로 시작 바인딩이 그 짝이다(실행 중 새 지형이 생기기 전).
                if (!AttackForcedMovement.CanEnterUnit(input, environment, input.EnvironmentBindings, input.Characters,
                    input.State.Structures, target.EntityId, current, next) || walls.Occupies(next))
                {
                    completed(PullAttackEvaluation.Failure(AttackFailureReason.PullPathBlocked));
                    yield break;
                }
                path.Add(next);
                current = next;
                yield return "SelectionPullPathCell";
            }
            completed(new PullAttackEvaluation(AttackFailureReason.None, target.EntityId, contact, destination, path, damage));
            yield return "SelectionPullPathComplete";
        }

        internal static IEnumerable<string> ExecuteSteps(PullAttackEvaluation evaluation,
            CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger, MovementProjection arrival,
            Action<bool, AttackPullSnapshot> completed)
        {
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            if (evaluation.FailureReason != AttackFailureReason.None)
                throw new InvalidOperationException("거절된 끌어오기를 실행할 수 없습니다.");
            if (!evaluation.RequiresPull)
            {
                completed(evaluation.DeliverDirectDamage, null);
                yield break;
            }
            var step = evaluation.Path[0] - evaluation.StartAnchor;
            int movementEventIndex = world.Events.Count;
            ForcedDisplacementPath path = null;
            foreach (var phase in AttackForcedMovement.ExecuteUnitSteps(plan, world, ledger, arrival, evaluation.TargetId, step,
                evaluation.Path.Count, deferLastObjects: false, publishMovedEvents: false, completed: value => path = value))
                yield return phase;
            AttackPullSnapshot movement = null;
            if (path.Steps.Count > 0)
            {
                movement = new AttackPullSnapshot(evaluation.TargetId, evaluation.StartAnchor,
                    path.Steps[path.Steps.Count - 1], path.Steps);
                world.Events.Insert(movementEventIndex, new CombatPullEvent(movement));
                var occurrences = new List<CombatEvent>();
                CombatCommandEvents.Trait(occurrences, plan.Actor.EntityId, Contracts.Common.TraitKey.Pull,
                    Contracts.Common.TraitActivationKind.PullApplied, path.Steps.Count, new[] { evaluation.TargetId },
                    path.Steps.ToArray(), new CombatDamageOrigin(plan.Actor.Anchor, plan.Actor.EntityId, plan.AttackGroup));
                world.Events.InsertRange(movementEventIndex, occurrences);
                yield return "PullMovementEvidence";
            }
            var target = world.Characters[evaluation.TargetId];
            if (!target.Health.IsDead && !arrival.Terminal && path.Steps.Count > 0 &&
                path.Stop != ForcedDisplacementStop.Interrupted && target.Anchor == path.Steps[path.Steps.Count - 1])
            {
                var targetId = target.EntityId;
                foreach (var phase in arrival.ResolveForcedArrivalSteps(targetId, loss => ledger.AddNormalizationLoss(targetId, loss)))
                    yield return phase;
            }
            target = world.Characters[evaluation.TargetId];
            ledger.CapturePreHitDamage(world.Damage.CaptureAppliedDamage());
            completed(evaluation.DeliverDirectDamage && path.Stop == ForcedDisplacementStop.Completed &&
                !arrival.Terminal && !world.Characters[plan.Actor.EntityId].Health.IsDead &&
                !target.Health.IsDead && target.Anchor == evaluation.DestinationAnchor, movement);
            yield return "PullResult";
        }

    }
}
