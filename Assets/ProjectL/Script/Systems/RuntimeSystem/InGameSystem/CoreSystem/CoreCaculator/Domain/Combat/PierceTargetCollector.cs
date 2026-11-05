using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>정렬된 한 줄 경로에서 조우와 차단 증거만 수집한다. 자격 선정과 피해 예산 계산은 호출자가 담당한다.</summary>
    internal static class PierceTargetCollector
    {
        /// <summary>조준 셀이 출발점과 가로·세로·대각선으로 정렬되면 한 칸 진행 방향을 돌려준다. 정렬 판정은 여기에만 둔다.</summary>
        internal static bool TryResolveStep(CellCoord origin, CellCoord aim, out CellCoord step)
        {
            // 좌표 뺄셈은 확장 정수로 계산해 오버플로를 막는다.
            long dx = (long)aim.X - origin.X;
            long dy = (long)aim.Y - origin.Y;
            if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0 && Math.Abs(dx) != Math.Abs(dy)))
            {
                step = default;
                return false;
            }
            step = new CellCoord(Math.Sign(dx), Math.Sign(dy));
            return true;
        }

        /// <summary>
        /// 조준 방향으로 보드 끝까지 진행한다. 사거리는 조준 칸만 제한하고 진행 거리는 제한하지 않는다.
        /// 진행은 첫 벽(이동 종점)·살아 있는 구조물·보드 끝에서만 멈춘다.
        /// 맞는 칸이 출발 칸에서 조준 방향의 줄 위라는 성질을 적 추적 조준 후보(<c>CombatAttackQueries.AimsThatCanHit</c>)가 쓴다. 바꾸면 그쪽도 고친다.
        /// </summary>
        internal static PiercingShotTrace CollectSingle(CellCoord origin, CellCoord aim,
            IReadOnlyList<CellCoord> actorCells, int width, int height,
            IReadOnlyList<CharacterCombatFacts> eligibleCharacters, IReadOnlyList<StructureCombatFacts> eligibleStructures,
            IReadOnlyList<StructureCombatFacts> physicalStructures, AttackTargetingPolicy policy, WallGeometry walls,
            AttackElevation elevation, AttackOcclusionSpec occlusion)
        {
            if (actorCells == null) throw new ArgumentNullException(nameof(actorCells));
            if (actorCells.Count == 0) throw new ArgumentException("공격자의 점유 셀이 필요합니다.", nameof(actorCells));
            if (eligibleCharacters == null) throw new ArgumentNullException(nameof(eligibleCharacters));
            if (eligibleStructures == null) throw new ArgumentNullException(nameof(eligibleStructures));
            if (physicalStructures == null) throw new ArgumentNullException(nameof(physicalStructures));
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (elevation == null) throw new ArgumentNullException(nameof(elevation));
            if (occlusion == null) throw new ArgumentNullException(nameof(occlusion));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (origin.X < 0 || origin.X >= width || origin.Y < 0 || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (aim.X < 0 || aim.X >= width || aim.Y < 0 || aim.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(aim));

            if (!TryResolveStep(origin, aim, out var step))
                throw new ArgumentException("조준 셀은 출발점과 가로, 세로 또는 대각선으로 정렬되어야 합니다.", nameof(aim));
            var traveled = new List<CellCoord>();
            var encounters = new List<(EntityId Id, int Encounter, CellCoord Cell)>();
            var blocked = new List<AttackBlockedContact>();
            var encounteredIds = new HashSet<EntityId>();
            var endpoint = origin;
            var stopReason = PiercingShotStopReason.BoardEdge;
            AttackBlockedContact blocker = null;

            // 진행 방향은 0이 아니므로 반복은 늦어도 보드 끝에서 끝난다.
            for (long distance = 1; ; distance++)
            {
                long x = origin.X + distance * step.X;
                long y = origin.Y + distance * step.Y;
                if (x < 0 || x >= width || y < 0 || y >= height) break;
                var cell = new CellCoord((int)x, (int)y);
                if (blocker == null)
                {
                    // 벽 뒤의 진단 수집을 계속해도 실제 이동과 종점은 첫 벽에서 고정한다. 고저 조건은 칸마다 판정한다(설계 D3).
                    blocker = AttackOcclusionResolver.CheckDirect(origin, cell, walls, elevation, occlusion);
                    endpoint = cell;
                    if (blocker == null) traveled.Add(cell);
                    else stopReason = PiercingShotStopReason.Wall;
                }
                // 빈 칸과 인접 제외 칸도 이동 경로에 남기며 벽 검사를 생략하지 않는다.
                if (actorCells.Contains(cell) ||
                    policy.ExcludesAdjacentDirectCells && CannonAppointExclusionCalculator.IsForbidden(actorCells, cell))
                    continue;

                if (!walls.Occupies(cell))
                {
                    var character = eligibleCharacters.SingleOrDefault(c => !c.Health.IsDead && c.OccupiedCells.Contains(cell));
                    var structure = eligibleStructures.SingleOrDefault(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
                    EntityId id = character?.EntityId ?? structure?.EntityId ?? EntityId.Empty;
                    if (!id.IsEmpty && encounteredIds.Add(id))
                    {
                        if (blocker == null) encounters.Add((id, encounters.Count, cell));
                        else blocked.Add(new AttackBlockedContact(origin, cell, AttackOcclusionPhase.Direct,
                            blocker.WallSourceIds, blocker.WallCells));
                    }
                }
                // 기존 포대 인접 제외를 통과한 살아 있는 구조물은 대상 자격과 무관하게 탐색을 끝낸다.
                if (physicalStructures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell)))
                {
                    if (blocker == null) stopReason = PiercingShotStopReason.Structure;
                    break;
                }
            }
            return new PiercingShotTrace(origin, aim, step, traveled, encounters, endpoint, stopReason, blocked);
        }
    }
}
