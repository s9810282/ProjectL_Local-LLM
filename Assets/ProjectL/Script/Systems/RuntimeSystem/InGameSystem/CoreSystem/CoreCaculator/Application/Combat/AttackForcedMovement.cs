using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공격의 유닛 강제 이동에서 충돌·정규화·칸별 지형 처리를 공유합니다. 방향·면역·공격 순서는 호출자가 소유합니다.</summary>
    internal static class AttackForcedMovement
    {
        internal static bool CanEnterUnit(CombatCommandInput input, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, IEnumerable<CharacterCombatFacts> characters,
            IEnumerable<StructureCombatFacts> structures, EntityId id, CellCoord from, CellCoord to,
            IReadOnlyCollection<CellCoord> transientReservations = null) =>
            FindBlocker(input, environment, bindings, characters, structures, id, from, to, transientReservations).IsNone;

        /// <summary>
        /// 유닛 강제 이동이 <paramref name="to"/>에 들어가지 못하는 이유를 돌려준다. 강제 이동 진입 규칙의 유일한 원본이다.
        /// 맵 밖·벽 지형·낮은 칸에서 고지대로 가는 턱은 벽, 살아 있는 필드 캐릭터·구조물은 그 개체다. 다칸 대상도 앵커 칸만 본다.
        /// <paramref name="environment"/>와 <paramref name="bindings"/>는 같은 시점의 짝이어야 한다. 같은 공격 안에서 새로 생긴
        /// 지형(기둥 잔해 벽 등)은 새 ID라 현재 바인딩에만 있으므로, 실행 중에는 도착 투영의 현재 환경과 바인딩을 함께 넘긴다.
        /// </summary>
        internal static ForcedMovementBlocker FindBlocker(CombatCommandInput input, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, IEnumerable<CharacterCombatFacts> characters,
            IEnumerable<StructureCombatFacts> structures, EntityId id, CellCoord from, CellCoord to,
            IReadOnlyCollection<CellCoord> transientReservations = null)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (!input.State.IsInside(to) || !EnvironmentTerrainRules.CanKnockback(environment, bindings, from, to))
                return ForcedMovementBlocker.Wall;
            // 기믹 점유는 앵커뿐 아니라 이동할 전체 모양을 검사한다. 기존 지형 판정 범위는 유지한다.
            if (GimmickOccupancyRules.Blocks(input.State.Gimmicks, to, transientReservations)) return ForcedMovementBlocker.Wall;
            var mover = characters.FirstOrDefault(c => c.EntityId == id);
            if (mover?.Anchor is CellCoord anchor)
            {
                var offset = to - anchor;
                foreach (var occupied in mover.OccupiedCells)
                    if (GimmickOccupancyRules.Blocks(input.State.Gimmicks, occupied + offset, transientReservations))
                        return ForcedMovementBlocker.Wall;
            }
            var character = characters.Where(c => c.EntityId != id && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(to)).OrderBy(c => c.EntityId).FirstOrDefault();
            if (character != null) return ForcedMovementBlocker.Character(character.EntityId);
            var structure = structures.Where(s => !s.Health.IsDead && s.OccupiedCells.Contains(to))
                .OrderBy(s => s.EntityId).FirstOrDefault();
            return structure != null ? ForcedMovementBlocker.Structure(structure.EntityId) : ForcedMovementBlocker.None;
        }

        /// <summary>넉백은 마지막 오브젝트 도착을 정산 뒤로 미루며, 사전 강제 이동은 즉시 도착까지 처리합니다.</summary>
        internal static IEnumerable<string> ExecuteUnitSteps(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, MovementProjection arrival, EntityId id, CellCoord step, int distance,
            bool deferLastObjects, bool publishMovedEvents, Action<ForcedDisplacementPath> completed,
            CombatMovementCause movementCause = null)
        {
            var request = new ForcedMovementRequest(id, step, distance,
                movementCause ?? new CombatMovementCause(CombatMovementAgency.Forced), deferLastObjects, publishMovedEvents);
            foreach (var phase in ForcedMovementExecution.ExecuteSteps(arrival, request, plan.Rules, result =>
            {
                ledger.AddNormalizationLoss(id, result.NormalizationHealthLoss);
                completed(result.Path);
            })) yield return phase;
        }
    }
}
