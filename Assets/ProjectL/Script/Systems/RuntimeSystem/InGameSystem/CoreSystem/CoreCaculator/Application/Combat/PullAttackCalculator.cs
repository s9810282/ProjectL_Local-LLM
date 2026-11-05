using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
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
        internal static int SelectionRange(CharacterCombatFacts actor, CombatAttackProfile profile, int basicRange)
        {
            var configuration = Configuration(actor, profile);
            return configuration == null ? basicRange : PullTraitRule.SelectionRange(basicRange, configuration.ExtraRangeCells);
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

        internal static PullAttackEvaluation Evaluate(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, ResolvedAttackSelection selection, MovementEnvironmentFacts environment, WallGeometry walls)
        {
            if (Configuration(actor, profile) == null) return null;
            int basicRange = EnvironmentTerrainRules.AppointRange(input, actor, environment);
            var contact = selection.Cells.PrimaryCell.Value;
            var destination = PullTraitRule.Destination(actor.Anchor.Value, contact, basicRange);
            var target = selection.EligibleCharacters.SingleOrDefault(c => c.EntityId == selection.SelectedTargetId);
            // 구조물에는 기본 사거리의 기존 공격만 허용하며 이동 경로를 만들지 않습니다.
            if (target == null)
                return destination == contact ? null : PullAttackEvaluation.Failure(AttackFailureReason.OutOfBounds);
            bool damage = PullTraitRule.DealsAttackDamage(actor.Owner, target.Owner);
            if (damage && selection.Policy.ExcludesAdjacentDirectCells &&
                CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, destination))
                return PullAttackEvaluation.Failure(AttackFailureReason.AppointCellRejected);
            if (destination == contact)
                return damage ? new PullAttackEvaluation(AttackFailureReason.None, target.EntityId, contact, contact,
                    Array.Empty<CellCoord>(), true) : PullAttackEvaluation.Failure(AttackFailureReason.PullNoMovement);
            if (UnitBehaviorRules.IsForcedMovementImmune(input, target))
                return PullAttackEvaluation.Failure(AttackFailureReason.PullImmune);
            var step = new CellCoord(Math.Sign(destination.X - contact.X), Math.Sign(destination.Y - contact.Y));
            var path = new List<CellCoord>();
            for (var current = contact; current != destination;)
            {
                var next = current + step;
                if (!AttackForcedMovement.CanEnterUnit(input, environment, input.Characters, input.State.Structures,
                    target.EntityId, current, next) || walls.Occupies(next))
                    return PullAttackEvaluation.Failure(AttackFailureReason.PullPathBlocked);
                path.Add(next);
                current = next;
            }
            return new PullAttackEvaluation(AttackFailureReason.None, target.EntityId, contact, destination, path, damage);
        }

        internal static (bool CanDeliverDamage, AttackPullSnapshot Movement) Execute(PullAttackEvaluation evaluation,
            CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger, MovementProjection arrival)
        {
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            if (evaluation.FailureReason != AttackFailureReason.None)
                throw new InvalidOperationException("거절된 끌어오기를 실행할 수 없습니다.");
            if (!evaluation.RequiresPull) return (evaluation.DeliverDirectDamage, null);
            var step = evaluation.Path[0] - evaluation.StartAnchor;
            int movementEventIndex = world.Events.Count;
            var path = AttackForcedMovement.ExecuteUnit(plan, world, ledger, arrival, evaluation.TargetId, step,
                evaluation.Path.Count, deferLastObjects: false, publishMovedEvents: false);
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
            }
            var target = world.Characters[evaluation.TargetId];
            if (!target.Health.IsDead && !arrival.Terminal && path.Steps.Count > 0 &&
                path.Stop != ForcedDisplacementStop.Interrupted && target.Anchor == path.Steps[path.Steps.Count - 1])
                ledger.AddNormalizationLoss(target.EntityId, arrival.ResolveForcedArrival(target.EntityId));
            target = world.Characters[evaluation.TargetId];
            ledger.CapturePreHitDamage(world.Damage.CaptureAppliedDamage());
            return (evaluation.DeliverDirectDamage && path.Stop == ForcedDisplacementStop.Completed &&
                !arrival.Terminal && !world.Characters[plan.Actor.EntityId].Health.IsDead &&
                !target.Health.IsDead && target.Anchor == evaluation.DestinationAnchor, movement);
        }

        /// <summary>선택한 개체를 새 도착 위치로 다시 전달하되 최초 명령의 기대 상태와 비용은 유지합니다.</summary>
        internal static CommandAttackPlan RebuildDelivery(CommandAttackPlan originalPlan, CommandAttackProjection world,
            MovementProjection arrival, EntityId targetId)
        {
            var target = world.Characters[targetId];
            var current = CombatAttackQueries.WithAim(arrival.CurrentInput(), AttackAim.ForCell(target.Anchor.Value));
            var actor = world.Characters[originalPlan.Actor.EntityId];
            var environment = arrival.Environment;
            // 벽은 공개 지형이라 형상은 원본에서 만든 최초 계획의 것을 그대로 쓴다. 공개 미리보기의 환경은 관찰자 투영이라
            // 모든 지형이 한쪽에만 공개되어 벽 형상 검사를 통과하지 못한다(CombatAttackQueries.Selection과 같은 규칙).
            var walls = originalPlan.Walls;
            var elevation = AttackElevation.From(environment, current.EnvironmentBindings);
            var evaluation = CombatAttackQueries.Selection(current, actor, originalPlan.Profile,
                environment: environment, walls: walls, elevation: elevation);
            if (evaluation.FailureReason != AttackFailureReason.None || evaluation.Selection.SelectedTargetId != targetId)
                throw new InvalidOperationException("끌어오기 후 전달 대상의 식별자가 달라졌습니다.");
            int damage = CombatAttackCalculator.CommonAttackDamage(current, actor, originalPlan.Profile, world.Events, originalPlan.TraitScope);
            var delivery = CombatAttackDelivery.Resolve(current, originalPlan.TraitScope?.ForDamage(actor) ?? actor, originalPlan.Profile, evaluation.Selection,
                damage, walls, elevation);
            world.BlockedContacts.AddRange(delivery.BlockedContacts);
            return new CommandAttackPlan(originalPlan.Input,originalPlan.Rules,originalPlan.Profile,                originalPlan.OriginalActor,actor,environment,walls,evaluation.Selection,delivery,                originalPlan.Engine,CombatCommandEffects.Capture(actor, originalPlan.Profile),originalPlan.Pipeline,                damage,originalPlan.Direction,originalPlan.PublicPreview,originalPlan.DamageKind,                originalPlan.AttackGroup,originalPlan.Cause,originalPlan.Aim,originalPlan.RequestedCells,originalPlan.TraitScope);
        }
    }
}
