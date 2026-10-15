using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>실행과 범위 조회가 같은 주체·지정·대상 조건을 사용하도록 묶는다. AP와 이동 정산은 별도다.</summary>
    internal static class CombatAttackQueries
    {
        internal static AttackFailureReason Actor(CombatCommandInput input, out CharacterCombatFacts actor)
        {
            actor = null;
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.State.Phase == CombatFlowPhase.Finished) return AttackFailureReason.StageEnded;
            if (input.State.Turn.StagePhase != StagePhase.Combat ||
                input.State.Phase != (input.ProcessingKind == CombatProcessingKind.Advance ? CombatFlowPhase.Automatic : CombatFlowPhase.PlayerInput))
                return AttackFailureReason.WrongPhase;
            if (input.ActorId != input.State.ActiveActorId) return AttackFailureReason.NotActiveUnit;
            actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null) return AttackFailureReason.EntityNotFound;
            if (actor.Owner != (input.ProcessingKind == CombatProcessingKind.Advance ? Owner.Enemy : Owner.Player))
                return AttackFailureReason.WrongPhase;
            if (actor.Area != CharacterRuntimeArea.Field || actor.Health.IsDead) return AttackFailureReason.NotInCombat;
            if (input.Characters.Any(c => c.Area == CharacterRuntimeArea.Field && c.OccupiedCells.Count != 1))
                return AttackFailureReason.DeferredMultiCellUnit;
            return AttackFailureReason.None;
        }

        internal static AttackSelectionEvaluation Selection(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim? aim = null, MovementEnvironmentFacts environment = null, WallGeometry walls = null)
        {
            var policy = new CombatEffectEngine().EvaluateAttackTargeting(CombatCommandEffects.Capture(actor, profile)).Policy;
            int range = EnvironmentTerrainRules.AppointRange(input, actor, environment);
            if (!new AttackSelectionResolver().TryResolve(profile.Method, range,
                actor.Anchor.Value, aim ?? input.Aim, out var raw, out var failure)) return AttackSelectionEvaluation.Failure(failure);
            if (raw.PrimaryCell.HasValue && !input.State.IsInside(raw.PrimaryCell.Value)) return AttackSelectionEvaluation.Failure(AttackFailureReason.OutOfBounds);
            if (policy.ExcludesAdjacentDirectCells && raw.PrimaryCell.HasValue &&
                CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, raw.PrimaryCell.Value))
                return AttackSelectionEvaluation.Failure(AttackFailureReason.AppointCellRejected);
            var selected = new AttackCellSelection(raw.PrimaryCell,
                raw.DesignatedCells.ToArray().Where(input.State.IsInside).Where(c => !policy.ExcludesAdjacentDirectCells || raw.PrimaryCell.HasValue ||
                    !CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, c)).ToArray(),
                raw.AdditionalCells.ToArray().Where(input.State.IsInside).ToArray());
            var eligible = input.Characters.Where(c => AttackTargetEligibility.CanTargetCharacter(actor, c, policy)).ToArray();
            var structures = input.State.Structures.Where(s => AttackTargetEligibility.CanTargetStructure(actor, s,
                input.EnvironmentBindings.ObstacleEntityIds.Contains(s.EntityId), policy)).ToArray();
            var targets = new AttackTargetResolver().CollectCandidates(selected, eligible, structures);
            // 벽은 공개 지형이다. 관찰자별 투영은 사거리에만 쓰고 형상에는 원본을 전달한다.
            walls ??= WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var primaryFailure = ValidatePrimary(input, selected, targets, policy, walls);
            if (primaryFailure != AttackFailureReason.None)
                return AttackSelectionEvaluation.Failure(primaryFailure,
                    selected.PrimaryCell.HasValue && walls.Occupies(selected.PrimaryCell.Value)
                        ? AttackOcclusionResolver.ResolveCells(actor.Anchor.Value, selected, walls, profile.Occlusion).BlockedContacts
                        : null);

            bool pierce = profile.Definition.Traits.ToArray().Any(t => t.TraitKey == TraitKey.PiercingFalloff);
            PierceTrace trace = null;
            AttackCellSelection reachable;
            IReadOnlyList<AttackBlockedContact> blocked;
            var emptyFailure = profile.Method.ChoiceMode == TargetingChoiceMode.All
                ? AttackFailureReason.UnresolvedEmptyAll : AttackFailureReason.NoEligibleTargets;
            if (pierce)
            {
                // 관통과 지정 방식의 결합은 CombatAttackProfile 생성 때 이미 검사했다.
                var steps = AppointShapeCalculator.CreateOrderedRaySteps(profile.Method.Shape, (aim ?? input.Aim).Direction);
                trace = PierceTargetCollector.Collect(actor.Anchor.Value, actor.OccupiedCells, steps,
                    input.State.BoardWidth, input.State.BoardHeight, range, eligible, structures, input.State.Structures,
                    policy, walls, profile.Occlusion);
                targets = trace.Encounters
                    .Select(h => h.Id).Distinct().OrderBy(id => id)
                    .Select(id => new KeyValuePair<EntityId, AttackTargetRole>(id, AttackTargetRole.Designated)).ToArray();
                blocked = trace.BlockedContacts;
                reachable = new AttackCellSelection(null, trace.ReachableCells.ToArray(), Array.Empty<CellCoord>());
                if (targets.Length == 0)
                    return AttackSelectionEvaluation.Failure(trace.HasWallBlockedEligibleTargets ? AttackFailureReason.BlockedByWall : emptyFailure, blocked);
            }
            else
            {
                if (!selected.PrimaryCell.HasValue && targets.Length == 0) return AttackSelectionEvaluation.Failure(emptyFailure);
                var occlusion = AttackOcclusionResolver.ResolveCells(actor.Anchor.Value, selected, walls, profile.Occlusion);
                reachable = occlusion.Cells;
                blocked = occlusion.BlockedContacts;
                if (selected.PrimaryCell.HasValue && !reachable.PrimaryCell.HasValue)
                    return AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked);
                targets = new AttackTargetResolver().CollectCandidates(reachable, eligible, structures);
                if (!selected.PrimaryCell.HasValue && targets.Length == 0)
                    return AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked);
            }
            return AttackSelectionEvaluation.Success(new ResolvedAttackSelection(selected, reachable, targets,
                eligible, structures, policy, trace), blocked);
        }

        private static AttackFailureReason ValidatePrimary(CombatCommandInput input, AttackCellSelection selected,
            KeyValuePair<EntityId, AttackTargetRole>[] targets, AttackTargetingPolicy policy, WallGeometry walls)
        {
            if (!selected.PrimaryCell.HasValue) return AttackFailureReason.None;
            CellCoord primary = selected.PrimaryCell.Value;
            if (walls.Occupies(primary)) return AttackFailureReason.AppointCellRejected;
            if (!policy.AllowsEmptySingle &&
                !targets.Any(t => t.Value == AttackTargetRole.Primary))
            {
                // 주변의 유효 대상이 빈 칸이나 아군 중심 지정의 자격을 대신하지 않는다.
                bool occupied = input.Characters.Any(c => c.Area == CharacterRuntimeArea.Field &&
                    !c.Health.IsDead && c.OccupiedCells.Contains(primary)) ||
                    input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(primary));
                return occupied ? AttackFailureReason.NonHostile : AttackFailureReason.InvalidPrimaryTarget;
            }
            return AttackFailureReason.None;
        }

        internal static AttackRangeResult Range(CombatCommandInput input)
        {
            var failure = Actor(input, out var actor);
            if (failure != AttackFailureReason.None) return AttackRangeResult.Failure(failure, input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var policy = engine.EvaluateAttackTargeting(bindings).Policy;
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            int effectiveRange = EnvironmentTerrainRules.AppointRange(input, actor, environment);
            var cells = new AppointRangeCalculator().CalculateRangeCells(profile.Method, effectiveRange, actor.Anchor.Value)
                .Where(input.State.IsInside).Where(c => !policy.ExcludesAdjacentDirectCells ||
                    !CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, c)).OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
            var mask = new HashSet<CellCoord>(cells);
            var ids = input.Characters.Where(c => AttackTargetEligibility.CanTargetCharacter(actor, c, policy) &&
                c.OccupiedCells.Any(mask.Contains)).Select(c => c.EntityId)
                .Concat(input.State.Structures.Where(s => AttackTargetEligibility.CanTargetStructure(actor, s,
                    input.EnvironmentBindings.ObstacleEntityIds.Contains(s.EntityId), policy) &&
                    s.OccupiedCells.Any(mask.Contains)).Select(s => s.EntityId)).Distinct().OrderBy(id => id).ToArray();
            IEnumerable<AttackAim> candidates = profile.Method.ChoiceMode == TargetingChoiceMode.Single
                ? cells.Select(AttackAim.ForCell)
                : profile.Method.ChoiceMode == TargetingChoiceMode.All ? new[] { AttackAim.ForAll() }
                : Enum.GetValues(typeof(AttackDirection)).Cast<AttackDirection>().Where(d => d != AttackDirection.None)
                    .OrderBy(d => (int)d).Select(AttackAim.ForDirection);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, input.State.Environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var aims = candidates.Where(a => Selection(input, actor, profile, a, environment, walls).FailureReason == AttackFailureReason.None)
                .Distinct().ToArray();
            int cost = engine.EvaluateAttackCost(bindings,
                new AttackCostEffectContext(actor.Stats.AttackCost)).EffectiveCost;
            var direct = AttackOcclusionResolver.ResolveCells(actor.Anchor.Value,
                new AttackCellSelection(null, cells, Array.Empty<CellCoord>()), walls, profile.Occlusion);
            return AttackRangeResult.Success(actor.EntityId, cells, ids, cost <= actor.Stats.CurrentActionCost, aims, direct.BlockedContacts);
        }

        internal static CombatCommandInput WithAim(CombatCommandInput input, AttackAim aim) =>
            new(input.Number, input.Kind, input.ActorId, aim, input.State, input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                input.TargetCell, input.Rotation, input.Battle, input.ProcessingKind, input.Path, input.MovementContext,
                input.BattleInstanceId, input.ArtifactItems, input.EnvironmentBindings, input.ObstacleContents);
    }
}
