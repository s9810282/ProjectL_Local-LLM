using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
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
            var admission = CombatActionAdmission.Actor(input, CombatCommandKind.Attack, out actor);
            if (admission != Contracts.Results.MovementFailureReason.None)
                return Enum.TryParse(admission.ToString(), out AttackFailureReason failure)
                    ? failure : AttackFailureReason.MapOccupancyMismatch;
            var ready = UnitReady(input, actor);
            if (ready != AttackFailureReason.None) return ready;
            return AttackFailureReason.None;
        }

        /// <summary>
        /// 공격 범위를 조회할 수 있는 공격자인지다. 자금 부족은 고를 수 있는지에만 반영하므로 조회는 허용한다.
        /// 범위 조회(<see cref="Range"/>)와 적 추적의 "여기서 칠 수 있나" 판정이 이 규칙 하나를 쓴다(적 추적 성능 설계 D2).
        /// </summary>
        internal static AttackFailureReason RangeActor(CombatCommandInput input, out CharacterCombatFacts actor)
        {
            var failure = Actor(input, out actor);
            return failure == AttackFailureReason.ActionCostInsufficient ? AttackFailureReason.None : failure;
        }

        /// <summary>
        /// 조준을 고를 수 있는 사거리다: 지형 보정 기본 사거리에 끌어오기 추가 사거리를 더한다.
        /// 지정(<see cref="Selection"/>)과 범위 칸(<see cref="RangeCells"/>)이 이 식 하나를 쓴다.
        /// </summary>
        internal static int SelectionRange(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
            MovementEnvironmentFacts environment = null) =>
            PullAttackCalculator.SelectionRange(actor, profile, EnvironmentTerrainRules.AppointRange(input, actor, environment));

        /// <summary>
        /// 차례·국면과 무관한 유닛 쪽 공격 조건이다: 필드에 살아 있음, 기본 공격 가능, 여러 칸 유닛 보류.
        /// 행동 검사(<see cref="Actor"/>)와 조회(<see cref="UnitRange"/>)가 같이 쓴다.
        /// </summary>
        private static AttackFailureReason UnitReady(CombatCommandInput input, CharacterCombatFacts unit)
        {
            if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead) return AttackFailureReason.NotInCombat;
            if (CombatActionAdmission.IsAttackRestricted(unit)) return AttackFailureReason.Disarmed;
            if (!UnitBehaviorRules.CanBasicAttack(input, unit)) return AttackFailureReason.BasicAttackDisabled;
            if (input.Characters.Any(c => c.Area == CharacterRuntimeArea.Field && c.OccupiedCells.Count != 1))
                return AttackFailureReason.DeferredMultiCellUnit;
            return AttackFailureReason.None;
        }

        /// <summary>
        /// 조준 하나를 지정·대상·가림·전달 규칙으로 판정한다.
        /// 주 칸 검사(모양·각도·보드 안·하붑·관통 정렬·대포 인접)는 <see cref="RangeCells"/>의 거르기와 하나하나 같아야 한다.
        /// 한쪽에만 조건을 더하면 적 추적이 범위 조회 없이 조준을 고르는 판정(적 추적 성능 설계 §4-2)이 지금 결과와 달라진다.
        /// </summary>
        internal static AttackSelectionEvaluation Selection(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim? aim = null, MovementEnvironmentFacts environment = null, WallGeometry walls = null,
            AttackElevation elevation = null)
        {
            var policy = new CombatEffectEngine().EvaluateAttackTargeting(CombatCommandEffects.Capture(actor, profile)).Policy;
            int range = SelectionRange(input, actor, profile, environment);
            if (!new AttackSelectionResolver().TryResolve(profile.Method, range,
                actor.Anchor.Value, actor.Owner, aim ?? input.Aim, out var raw, out var failure)) return AttackSelectionEvaluation.Failure(failure);
            if (raw.PrimaryCell.HasValue && !input.State.IsInside(raw.PrimaryCell.Value)) return AttackSelectionEvaluation.Failure(AttackFailureReason.OutOfBounds);
            if (raw.PrimaryCell.HasValue && !CanDesignate(input, actor, raw.PrimaryCell.Value))
                return AttackSelectionEvaluation.Failure(AttackFailureReason.HaboobDesignationBlocked);
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
            // 고지대는 한쪽에만 보일 수 있어 사거리처럼 이 연산의 환경(관찰자 투영)을 따른다(고지대 Wall 무시 설계 D8).
            walls ??= WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            elevation ??= AttackElevation.From(environment ?? input.State.Environment, input.EnvironmentBindings);
            var primaryFailure = ValidatePrimary(input, selected, targets, policy, walls);
            if (primaryFailure != AttackFailureReason.None)
                return AttackSelectionEvaluation.Failure(primaryFailure,
                    selected.PrimaryCell.HasValue && walls.Occupies(selected.PrimaryCell.Value)
                        ? AttackOcclusionResolver.ResolveCells(actor.Anchor.Value, selected, walls, elevation, profile.Occlusion).BlockedContacts
                        : null);

            EntityId selectedTargetId = targets.Where(t => t.Value == AttackTargetRole.Primary)
                .Select(t => t.Key).DefaultIfEmpty(EntityId.Empty).Single();
            var emptyFailure = profile.Method.ChoiceMode == TargetingChoiceMode.All
                ? AttackFailureReason.UnresolvedEmptyAll : AttackFailureReason.NoEligibleTargets;
            // 가림 계산을 빈 대상 검사 앞에 둔다. 위치 없는 지정이 대상 없이 실패해도 덮었을 칸(표시용)을 같은 식으로 남긴다.
            var occlusion = AttackOcclusionResolver.ResolveCells(actor.Anchor.Value, selected, walls, elevation, profile.Occlusion);
            var reachable = occlusion.Cells;
            var blocked = occlusion.BlockedContacts;
            if (!selected.PrimaryCell.HasValue && targets.Length == 0)
                return AttackSelectionEvaluation.Failure(emptyFailure, emptyAreaCells: CombatAttackDelivery.DirectAffectedCells(reachable));
            if (selected.PrimaryCell.HasValue && !reachable.PrimaryCell.HasValue)
                return AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked);
            targets = new AttackTargetResolver().CollectCandidates(reachable, eligible, structures);
            if (!selected.PrimaryCell.HasValue && targets.Length == 0)
                return AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked,
                    CombatAttackDelivery.DirectAffectedCells(reachable));
            // 전달 검증은 일반 규칙이 받아들인 조준에만 적용한다. 특성이 일반 실패 사유를 바꾸지 않게 한다.
            if (selected.PrimaryCell.HasValue)
            {
                var deliveryFailure = CombatAttackDelivery.ValidateAim(profile, actor.Anchor.Value, selected.PrimaryCell.Value);
                if (deliveryFailure != AttackFailureReason.None) return AttackSelectionEvaluation.Failure(deliveryFailure);
            }
            var resolved = new ResolvedAttackSelection(selected, reachable, targets, eligible, structures, policy, selectedTargetId);
            var pull = PullAttackCalculator.Evaluate(input, actor, profile, resolved, environment ?? input.State.Environment, walls);
            if (pull != null && pull.FailureReason != AttackFailureReason.None)
                return AttackSelectionEvaluation.Failure(pull.FailureReason, blocked);
            return AttackSelectionEvaluation.Success(resolved, blocked, pull);
        }

        /// <summary>추적과 공격 선택이 실제 전달의 관통·벽·지정 규칙을 함께 사용합니다.</summary>
        internal static bool DeliversTo(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
            AttackAim aim, EntityId targetId, int commonDamage, MovementEnvironmentFacts environment, WallGeometry walls, AttackElevation elevation)
        {
            var evaluation = Selection(input, actor, profile, aim, environment, walls, elevation);
            if (evaluation.FailureReason != AttackFailureReason.None) return false;
            return CombatAttackDelivery.Resolve(input, actor, profile, evaluation.Selection, commonDamage, walls, elevation)
                .Hits.Any(hit => hit.TargetId == targetId && hit.AppliesDamage);
        }

        /// <summary>방향 조준 후보와 그 순서(열거 값 순서, None 제외)다. 범위 조회와 추적 후보가 같은 순서로 돈다.</summary>
        private static readonly AttackAim[] DirectionAims = Enum.GetValues(typeof(AttackDirection)).Cast<AttackDirection>()
            .Where(d => d != AttackDirection.None).OrderBy(d => (int)d).Select(AttackAim.ForDirection).ToArray();

        /// <summary>
        /// 공격자가 지금 자리에서 그 칸들의 대상에 피해를 줄 수 있는 조준 후보다(적 추적 성능 설계 D1). 필요조건만 본다:
        /// 여기서 빠진 조준은 <see cref="DeliversTo"/>가 그 칸의 대상에 결코 참이 아니고, 실제 판정은 <see cref="DeliversTo"/>가 한다.
        /// 순서는 <see cref="Range"/>의 조준 순서와 같다(칸 = Y·X, 방향 = 열거 값).
        /// 피해가 나는 칸은 직접 전달이면 선택 칸(주 칸 + 스플래시, <c>CombatAttackDelivery.ResolveDirect</c>), 관통이면 출발 칸에서 조준 방향으로
        /// 보드 끝까지(<c>PierceTargetCollector.CollectSingle</c>)다. 새 전달 방식이나 피해 칸을 넓히는 규칙을 더하면 이 후보 규칙도 같이 고친다.
        /// </summary>
        internal static IReadOnlyList<AttackAim> AimsThatCanHit(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, IReadOnlyCollection<CellCoord> targetCells, MovementEnvironmentFacts environment)
        {
            var aims = new List<AttackAim>();
            if (targetCells.Count == 0) return aims;
            var method = profile.Method;
            int range = SelectionRange(input, actor, profile, environment);
            var origin = actor.Anchor.Value;
            var resolver = new AttackSelectionResolver();
            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                var primaries = new HashSet<CellCoord>();
                if (profile.UsesPiercingShot)
                {
                    // 관통은 조준 칸이 방향만 정한다. 대상과 같은 줄의 조준이면 어느 거리든 같은 줄을 지난다.
                    foreach (var target in targetCells)
                        if (PierceTargetCollector.TryResolveStep(origin, target, out var step))
                            for (int distance = 1; distance <= range; distance++)
                                primaries.Add(origin + new CellCoord(step.X * distance, step.Y * distance));
                }
                else
                {
                    // 선택 칸 = 주 칸 + 스플래시 칸이므로, 대상 칸에서 스플래시 칸을 뺀 자리가 주 칸 후보다.
                    var splash = AppointShapeCalculator.CreateSplashOffsets(method.Splash);
                    foreach (var target in targetCells)
                    {
                        primaries.Add(target);
                        foreach (var offset in splash)
                            primaries.Add(new CellCoord(target.X - offset.X, target.Y - offset.Y));
                    }
                }
                foreach (var cell in primaries.Where(input.State.IsInside).OrderBy(c => c.Y).ThenBy(c => c.X))
                {
                    var aim = AttackAim.ForCell(cell);
                    if (resolver.TryResolve(method, range, origin, actor.Owner, aim, out _, out _))
                        aims.Add(aim);
                }
                return aims;
            }

            var targets = new HashSet<CellCoord>(targetCells);
            var candidates = method.ChoiceMode == TargetingChoiceMode.All ? new[] { AttackAim.ForAll() } : DirectionAims;
            foreach (var aim in candidates)
            {
                if (resolver.TryResolve(method, range, origin, actor.Owner, aim, out var cells, out _) &&
                    (cells.DesignatedCells.ToArray().Any(targets.Contains) || cells.AdditionalCells.ToArray().Any(targets.Contains)))
                    aims.Add(aim);
            }
            return aims;
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
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackRange);
            // 자금 부족은 선택 가능 여부에만 반영하고 기존 범위·차단 근거는 보존한다.
            var failure = RangeActor(input, out var actor);
            if (failure != AttackFailureReason.None)
                return AttackRangeResult.Failure(failure, input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var policy = engine.EvaluateAttackTargeting(bindings).Policy;
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var cells = RangeCells(input, actor, profile, policy, environment);
            var basicCells = RangeCells(input, actor, profile, policy, environment, basicOnly: true);
            var basicMask = new HashSet<CellCoord>(basicCells);
            var mask = new HashSet<CellCoord>(cells);
            var ids = input.Characters.Where(c => AttackTargetEligibility.CanTargetCharacter(actor, c, policy) &&
                c.OccupiedCells.Any(mask.Contains)).Select(c => c.EntityId)
                .Concat(input.State.Structures.Where(s => AttackTargetEligibility.CanTargetStructure(actor, s,
                    input.EnvironmentBindings.ObstacleEntityIds.Contains(s.EntityId), policy) &&
                    s.OccupiedCells.Any(basicMask.Contains)).Select(s => s.EntityId)).Distinct().OrderBy(id => id).ToArray();
            IEnumerable<AttackAim> candidates = profile.Method.ChoiceMode == TargetingChoiceMode.Single
                ? cells.Select(AttackAim.ForCell)
                : profile.Method.ChoiceMode == TargetingChoiceMode.All ? new[] { AttackAim.ForAll() }
                : DirectionAims;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, input.State.Environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var elevation = AttackElevation.From(environment, input.EnvironmentBindings);
            var aims = candidates.Where(a => Selection(input, actor, profile, a, environment, walls, elevation).FailureReason == AttackFailureReason.None)
                .Distinct().ToArray();
            var direct = AttackOcclusionResolver.ResolveCells(actor.Anchor.Value,
                new AttackCellSelection(null, cells, Array.Empty<CellCoord>()), walls, elevation, profile.Occlusion);
            // 마무리 사격 대상: 실제 공격과 같은 판정 입구로 사거리 안 대상만 거른다(마무리 사격 대상 표시 설계 FS1, Q-FS1 A).
            var instantDefeat = ids.Where(id => FinishingShotApplication.Qualifies(input, actor,
                input.Characters.SingleOrDefault(c => c.EntityId == id),
                input.State.Structures.SingleOrDefault(s => s.EntityId == id))).ToArray();
            return AttackRangeResult.Success(actor.EntityId, cells, ids, aims, direct.BlockedContacts, basicCells, instantDefeat);
        }

        /// <summary>
        /// 필드 유닛 하나의 공격 범위 칸과 이동 칸을 조회한다(호버 공격 범위 설계 UR2, 호버 이동 범위 설계 EM1). 차례·국면·소유자는 보지 않고 유닛 조건만 본다.
        /// 지형은 플레이어 시점으로 투영한다(Q-UR2 A). 플레이어가 모르는 지형이 사거리 차이로 드러나지 않게 한다.
        /// 이동 칸은 공격 가능 여부와 따로 계산해 공격 실패 결과에도 담는다(P8). 전투 끝·유닛 없음이면 둘 다 비어 있다.
        /// </summary>
        internal static UnitAttackRangeResult UnitRange(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.State.Phase == CombatFlowPhase.Finished)
                return UnitAttackRangeResult.Failure(AttackFailureReason.StageEnded, input.ActorId);
            var unit = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (unit == null) return UnitAttackRangeResult.Failure(AttackFailureReason.EntityNotFound, input.ActorId);
            var environment = input.State.Environment.VisibleTo(Owner.Player);
            var moveCells = CombatMovementCalculator.UnitMoveCells(input, unit, environment);
            var failure = UnitReady(input, unit);
            if (failure != AttackFailureReason.None) return UnitAttackRangeResult.Failure(failure, input.ActorId, moveCells);
            var profile = CombatCommandCalculator.RequiredProfile(input, unit);
            var policy = new CombatEffectEngine().EvaluateAttackTargeting(CombatCommandEffects.Capture(unit, profile)).Policy;
            return UnitAttackRangeResult.Success(unit.EntityId, RangeCells(input, unit, profile, policy, environment), moveCells);
        }

        /// <summary>
        /// 공격 범위 칸이다: 지정 방식 모양·지형 사거리 보정·보드 안·전달 검사·대포 인접 금지, Y·X 순서.
        /// 행동 범위(<see cref="Range"/>)와 조회(<see cref="UnitRange"/>)의 유일한 칸 규칙이다.
        /// 거르기는 <see cref="Selection"/>의 주 칸 검사와 하나하나 같아야 한다(적 추적 성능 설계 §4-2).
        /// </summary>
        private static CellCoord[] RangeCells(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
            AttackTargetingPolicy policy, MovementEnvironmentFacts environment, bool basicOnly = false)
        {
            int effectiveRange = basicOnly ? EnvironmentTerrainRules.AppointRange(input, actor, environment)
                : SelectionRange(input, actor, profile, environment);
            return new AppointRangeCalculator().CalculateRangeCells(profile.Method, effectiveRange, actor.Anchor.Value, actor.Owner)
                .Where(input.State.IsInside)
                .Where(c => profile.Method.ChoiceMode != TargetingChoiceMode.Single || CanDesignate(input, actor, c))
                .Where(c => CombatAttackDelivery.ValidateAim(profile, actor.Anchor.Value, c) == AttackFailureReason.None)
                .Where(c => !policy.ExcludesAdjacentDirectCells ||
                    !CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, c)).OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
        }

        private static bool CanDesignate(CombatCommandInput input, CharacterCombatFacts actor, CellCoord primary) =>
            input.State.Gimmicks.Entries.OfType<HaboobGimmickState>().All(h =>
                HaboobTargetingRules.CanDesignate(h.Cells, actor.Anchor.Value, primary));

        internal static CombatCommandInput WithAim(CombatCommandInput input, AttackAim aim) =>
            input.WithIntent(input.Kind, aim, input.TargetCell, input.Path, input.MovementContext);
    }
}
