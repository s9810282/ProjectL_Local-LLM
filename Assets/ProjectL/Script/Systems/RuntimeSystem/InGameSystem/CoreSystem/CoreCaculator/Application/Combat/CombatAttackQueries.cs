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
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackAdmission);
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
            var failure = IntrinsicUnitReadiness(input, unit);
            if (failure != AttackFailureReason.None) return failure;
            return input.Characters.Any(DefersMultiCellAttack)
                ? AttackFailureReason.DeferredMultiCellUnit : AttackFailureReason.None;
        }

        internal static bool DefersMultiCellAttack(CharacterCombatFacts unit) =>
            unit.Area == CharacterRuntimeArea.Field && unit.OccupiedCells.Count != 1;

        internal static AttackFailureReason IntrinsicUnitReadiness(CombatCommandInput input, CharacterCombatFacts unit)
        {
            if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead) return AttackFailureReason.NotInCombat;
            var restriction = RestrictionFailure(unit);
            if (restriction != AttackFailureReason.None) return restriction;
            if (!UnitBehaviorRules.CanBasicAttack(input, unit)) return AttackFailureReason.BasicAttackDisabled;
            return AttackFailureReason.None;
        }

        /// <summary>
        /// 공격 제한(재구성 중·무장 해제)의 실패 이유다. 제한이 없으면 None이다. 판정은 <see cref="CombatActionAdmission.IsAttackRestricted"/> 하나이고,
        /// 여기서는 이유만 가른다. 유닛 조건 검사와 포효(대기 판정·예고, BossRoarCalculator)가 같이 쓴다.
        /// </summary>
        internal static AttackFailureReason RestrictionFailure(CharacterCombatFacts unit)
        {
            if (!CombatActionAdmission.IsAttackRestricted(unit)) return AttackFailureReason.None;
            return Domain.Traits.Rules.ReconstructionTraitRule.IsReconstructing(unit)
                ? AttackFailureReason.Reconstructing : AttackFailureReason.Disarmed;
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
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackSelection);
            AttackSelectionEvaluation result = null;
            foreach (var _ in SelectionSteps(input, actor, profile, aim, environment, walls, elevation, value => result = value)) { }
            return result;
        }

        internal static IEnumerable<string> SelectionSteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim? aim, MovementEnvironmentFacts environment, WallGeometry walls,
            AttackElevation elevation, Action<AttackSelectionEvaluation> completed)
        {
            var policy = new CombatEffectEngine().EvaluateAttackTargeting(CombatCommandEffects.Capture(actor, profile)).Policy;
            int range = SelectionRange(input, actor, profile, environment);
            yield return "SelectionPolicy";
            AttackCellSelection selected = null;
            AttackFailureReason failure = default;
            foreach (var phase in SelectionCellSteps(input, actor, profile, aim ?? input.Aim, range, policy,
                (value, reason) => { selected = value; failure = reason; })) yield return phase;
            if (failure != AttackFailureReason.None) { completed(AttackSelectionEvaluation.Failure(failure)); yield break; }
            var eligible = new List<CharacterCombatFacts>();
            foreach (var candidate in input.Characters)
            {
                if (AttackTargetEligibility.CanTargetCharacter(actor, candidate, policy)) eligible.Add(candidate);
                yield return "SelectionCharacterEligibility";
            }
            var structures = new List<StructureCombatFacts>();
            foreach (var candidate in input.State.Structures)
            {
                if (AttackTargetEligibility.CanTargetStructure(actor, candidate,
                    input.EnvironmentBindings.ObstacleEntityIds.Contains(candidate.EntityId), policy)) structures.Add(candidate);
                yield return "SelectionStructureEligibility";
            }
            KeyValuePair<EntityId, AttackTargetRole>[] targets = null;
            foreach (var phase in AttackTargetResolver.CollectSteps(selected, eligible, structures, value => targets = value))
                yield return phase;
            // 벽 형상은 공개 지형 원본을, 고저는 이 연산의 관찰 환경을 읽는다.
            if (walls == null)
                foreach (var phase in WallGeometryBuilder.BuildSteps(input.State.BoardWidth, input.State.BoardHeight,
                    input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking, value => walls = value))
                    yield return phase;
            if (elevation == null)
                foreach (var phase in AttackElevation.BuildSteps(environment ?? input.State.Environment,
                    input.EnvironmentBindings, value => elevation = value)) yield return phase;
            var primaryFailure = ValidatePrimary(input, selected, targets, policy, walls);
            if (primaryFailure != AttackFailureReason.None && (!selected.PrimaryCell.HasValue || !walls.Occupies(selected.PrimaryCell.Value)))
            {
                completed(AttackSelectionEvaluation.Failure(primaryFailure));
                yield break;
            }

            EntityId selectedTargetId = EntityId.Empty;
            var emptyFailure = profile.Method.ChoiceMode == TargetingChoiceMode.All
                ? AttackFailureReason.UnresolvedEmptyAll : AttackFailureReason.NoEligibleTargets;
            if (primaryFailure == AttackFailureReason.None)
                selectedTargetId = targets.Where(t => t.Value == AttackTargetRole.Primary)
                    .Select(t => t.Key).DefaultIfEmpty(EntityId.Empty).Single();

            // 위치 없는 지정이 실패해도 실제로 도달한 표시 칸을 남긴다.
            AttackCellSelection reachable = null;
            IReadOnlyList<AttackBlockedContact> blocked = null;
            foreach (var phase in AttackOcclusionResolver.ResolveCellSteps(actor.Anchor.Value, selected, walls, elevation,
                profile.Occlusion, (cells, contacts) => { reachable = cells; blocked = contacts; })) yield return phase;
            if (primaryFailure != AttackFailureReason.None)
            {
                completed(AttackSelectionEvaluation.Failure(primaryFailure, blocked));
                yield break;
            }
            if (!selected.PrimaryCell.HasValue && targets.Length == 0)
            {
                CellCoord[] empty = null;
                foreach (var phase in CombatAttackDelivery.DirectAffectedCellSteps(reachable, value => empty = value))
                    yield return phase;
                completed(AttackSelectionEvaluation.Failure(emptyFailure, emptyAreaCells: empty));
                yield break;
            }
            if (selected.PrimaryCell.HasValue && !reachable.PrimaryCell.HasValue)
            {
                completed(AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked));
                yield break;
            }
            foreach (var phase in AttackTargetResolver.CollectSteps(reachable, eligible, structures, value => targets = value))
                yield return phase;
            if (!selected.PrimaryCell.HasValue && targets.Length == 0)
            {
                CellCoord[] empty = null;
                foreach (var phase in CombatAttackDelivery.DirectAffectedCellSteps(reachable, value => empty = value))
                    yield return phase;
                completed(AttackSelectionEvaluation.Failure(AttackFailureReason.BlockedByWall, blocked, empty));
                yield break;
            }
            // 전달 검증은 일반 지정이 수락한 조준에만 적용한다.
            if (selected.PrimaryCell.HasValue)
            {
                var deliveryFailure = CombatAttackDelivery.ValidateAim(profile, actor.Anchor.Value, selected.PrimaryCell.Value);
                if (deliveryFailure != AttackFailureReason.None)
                {
                    completed(AttackSelectionEvaluation.Failure(deliveryFailure));
                    yield break;
                }
            }
            var resolved = new ResolvedAttackSelection(selected, reachable, targets, eligible, structures, policy, selectedTargetId);
            PullAttackEvaluation pull = null;
            foreach (var phase in PullAttackCalculator.EvaluateSteps(input, actor, profile, resolved,
                environment ?? input.State.Environment, walls, value => pull = value)) yield return phase;
            completed(pull != null && pull.FailureReason != AttackFailureReason.None
                ? AttackSelectionEvaluation.Failure(pull.FailureReason, blocked)
                : AttackSelectionEvaluation.Success(resolved, blocked, pull));
            yield return "SelectionComplete";
        }

        /// <summary>대상 점유와 무관한 지정 칸을 계산한다. 실행과 예상 위협이 같은 사거리·각도·지형 제한을 쓴다.</summary>
        internal static IEnumerable<string> SelectionCellSteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim aim, int range, AttackTargetingPolicy policy,
            Action<AttackCellSelection, AttackFailureReason> completed)
        {
            AttackCellSelection raw = null;
            AttackFailureReason failure = default;
            foreach (var phase in AttackSelectionResolver.ResolveSteps(profile.Method, range, actor.Anchor.Value, actor.Owner,
                aim, (value, reason) => { raw = value; failure = reason; })) yield return phase;
            if (failure == AttackFailureReason.None && raw.PrimaryCell is CellCoord primary)
            {
                if (!input.State.IsInside(primary)) failure = AttackFailureReason.OutOfBounds;
                else if (!CanDesignate(input, actor, primary)) failure = AttackFailureReason.HaboobDesignationBlocked;
                else if (policy.ExcludesAdjacentDirectCells && CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, primary))
                    failure = AttackFailureReason.AppointCellRejected;
            }
            if (failure != AttackFailureReason.None) { completed(null, failure); yield break; }

            var designated = new List<CellCoord>();
            var additional = new List<CellCoord>();
            for (int i = 0; i < raw.DesignatedCells.Length; i++)
            {
                var cell = raw.DesignatedCells.Span[i];
                if (input.State.IsInside(cell) && (!policy.ExcludesAdjacentDirectCells || raw.PrimaryCell.HasValue ||
                    !CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, cell))) designated.Add(cell);
                yield return "SelectionBoardCell";
            }
            for (int i = 0; i < raw.AdditionalCells.Length; i++)
            {
                var cell = raw.AdditionalCells.Span[i];
                if (input.State.IsInside(cell)) additional.Add(cell);
                yield return "SelectionBoardCell";
            }
            completed(new AttackCellSelection(raw.PrimaryCell, designated.ToArray(), additional.ToArray()), AttackFailureReason.None);
        }

        /// <summary>추적과 공격 선택이 실제 전달의 관통·벽·지정 규칙을 함께 사용합니다.</summary>
        internal static bool DeliversTo(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
            AttackAim aim, EntityId targetId, int commonDamage, MovementEnvironmentFacts environment, WallGeometry walls, AttackElevation elevation)
        {
            bool delivers = false;
            foreach (var _ in DeliveryCheckSteps(input, actor, profile, aim, targetId, commonDamage, environment, walls, elevation,
                value => delivers = value)) { }
            return delivers;
        }

        internal static IEnumerable<string> DeliveryCheckSteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim aim, EntityId targetId, int commonDamage, MovementEnvironmentFacts environment,
            WallGeometry walls, AttackElevation elevation, Action<bool> completed)
        {
            ResolvedAttackDelivery delivery = null;
            foreach (var phase in DeliverySteps(input, actor, profile, aim, commonDamage, environment, walls, elevation,
                value => delivery = value))
                yield return phase;
            if (delivery == null)
            {
                completed(false);
                yield break;
            }
            foreach (var hit in delivery.Hits)
            {
                if (hit.TargetId == targetId && hit.AppliesDamage)
                {
                    completed(true);
                    yield break;
                }
                yield return "DeliveryTargetCheck";
            }
            completed(false);
        }

        internal static IEnumerable<string> DeliverySteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, AttackAim aim, int commonDamage, MovementEnvironmentFacts environment,
            WallGeometry walls, AttackElevation elevation, Action<ResolvedAttackDelivery> completed)
        {
            AttackSelectionEvaluation evaluation = null;
            foreach (var phase in SelectionSteps(input, actor, profile, aim, environment, walls, elevation, value => evaluation = value))
                yield return phase;
            if (evaluation.FailureReason != AttackFailureReason.None) { completed(null); yield break; }
            foreach (var phase in CombatAttackDelivery.ResolveSteps(input, actor, profile, evaluation.Selection, commonDamage,
                walls, elevation, completed)) yield return phase;
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
            using var cursor = EnumerateAimsThatCanHit(input, actor, profile, targetCells, environment);
            while (cursor.MoveNext())
                if (cursor.Current is AttackAim aim) aims.Add(aim);
            return aims;
        }

        internal static IEnumerator<AttackAim?> EnumerateAimsThatCanHit(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, IReadOnlyCollection<CellCoord> targetCells, MovementEnvironmentFacts environment)
        {
            if (targetCells.Count == 0) yield break;
            var method = profile.Method;
            int range = SelectionRange(input, actor, profile, environment);
            var origin = actor.Anchor.Value;
            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                var primaries = new SortedSet<CellCoord>(AimCellOrder);
                var splash = AppointShapeCalculator.CreateSplashOffsets(method.Splash);
                foreach (var target in targetCells)
                {
                    if (profile.UsesPiercingShot)
                    {
                        if (PierceTargetCollector.TryResolveStep(origin, target, out var step))
                            for (int distance = 1; distance <= range; distance++)
                            {
                                primaries.Add(origin + new CellCoord(step.X * distance, step.Y * distance));
                                yield return null;
                            }
                    }
                    else
                    {
                        primaries.Add(target);
                        yield return null;
                        foreach (var offset in splash)
                        {
                            primaries.Add(new CellCoord(target.X - offset.X, target.Y - offset.Y));
                            yield return null;
                        }
                    }
                    yield return null;
                }
                foreach (var cell in primaries)
                {
                    var aim = AttackAim.ForCell(cell);
                    bool valid = input.State.IsInside(cell) && AttackSelectionResolver.ValidateAim(
                        method, range, origin, actor.Owner, aim) == AttackFailureReason.None;
                    yield return valid ? aim : (AttackAim?)null;
                }
                yield break;
            }

            var targets = new HashSet<CellCoord>();
            foreach (var target in targetCells)
            {
                targets.Add(target);
                yield return null;
            }
            var candidates = method.ChoiceMode == TargetingChoiceMode.All ? new[] { AttackAim.ForAll() } : DirectionAims;
            foreach (var aim in candidates)
            {
                AttackCellSelection cells = null;
                foreach (var _ in AttackSelectionResolver.ResolveSteps(method, range, origin, actor.Owner, aim,
                    (value, _) => cells = value)) yield return null;
                if (cells != null)
                {
                    bool intersects = false;
                    for (int i = 0; i < cells.DesignatedCells.Length; i++)
                    {
                        if (targets.Contains(cells.DesignatedCells.Span[i])) { intersects = true; break; }
                        yield return null;
                    }
                    for (int i = 0; !intersects && i < cells.AdditionalCells.Length; i++)
                    {
                        if (targets.Contains(cells.AdditionalCells.Span[i])) { intersects = true; break; }
                        yield return null;
                    }
                    yield return intersects ? aim : (AttackAim?)null;
                }
                else yield return null;
            }
        }

        private static readonly IComparer<CellCoord> AimCellOrder = Comparer<CellCoord>.Create((left, right) =>
        {
            int rows = left.Y.CompareTo(right.Y);
            return rows != 0 ? rows : left.X.CompareTo(right.X);
        });

        internal static IEnumerable<AttackAim?> AiRangeCandidates(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, MovementEnvironmentFacts environment)
        {
            if (profile.Method.ChoiceMode == TargetingChoiceMode.All) { yield return AttackAim.ForAll(); yield break; }
            if (profile.Method.ChoiceMode != TargetingChoiceMode.Single)
            {
                foreach (var aim in DirectionAims) yield return aim;
                yield break;
            }

            int range = SelectionRange(input, actor, profile, environment);
            var policy = new CombatEffectEngine().EvaluateAttackTargeting(CombatCommandEffects.Capture(actor, profile)).Policy;
            var origin = actor.Anchor.Value;
            var cells = new SortedSet<CellCoord>(AimCellOrder);
            foreach (var offset in AppointShapeCalculator.MaskSteps(profile.Method.Shape, range, null))
            {
                if (offset.HasValue && (profile.Method.ChoiceAngle == TargetingChoiceAngle.Unrestricted ||
                    TargetingAngleCalculator.Contains(profile.Method.ChoiceAngle, actor.Owner, CellCoord.Zero, offset.Value)))
                {
                    var cell = new CellCoord(checked(origin.X + offset.Value.X), checked(origin.Y + offset.Value.Y));
                    if (IsRangeCell(input, actor, profile, policy, cell)) cells.Add(cell);
                }
                yield return null;
            }
            foreach (var cell in cells) yield return AttackAim.ForCell(cell);
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
                .Where(c => IsRangeCell(input, actor, profile, policy, c))
                .OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
        }

        private static bool IsRangeCell(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
            AttackTargetingPolicy policy, CellCoord cell) =>
            input.State.IsInside(cell) &&
            (profile.Method.ChoiceMode != TargetingChoiceMode.Single || CanDesignate(input, actor, cell)) &&
            CombatAttackDelivery.ValidateAim(profile, actor.Anchor.Value, cell) == AttackFailureReason.None &&
            (!policy.ExcludesAdjacentDirectCells || !CannonAppointExclusionCalculator.IsForbidden(actor.OccupiedCells, cell));

        private static bool CanDesignate(CombatCommandInput input, CharacterCombatFacts actor, CellCoord primary) =>
            input.State.Gimmicks.Entries.OfType<HaboobGimmickState>().All(h =>
                HaboobTargetingRules.CanDesignate(h.Cells, actor.Anchor.Value, primary));

        internal static CombatCommandInput WithAim(CombatCommandInput input, AttackAim aim) =>
            input.WithIntent(input.Kind, aim, input.TargetCell, input.Path, input.MovementContext);
    }
}
