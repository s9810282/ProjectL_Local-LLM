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
    /// <summary>정렬된 광선에서 조우와 차단 증거만 수집한다. 자격 선정과 피해 예산 계산은 호출자가 담당한다.</summary>
    internal static class PierceTargetCollector
    {
        internal static PierceTrace Collect(CellCoord origin, IReadOnlyList<CellCoord> actorCells,
            IReadOnlyList<CellCoord> raySteps, int width, int height, int range,
            IReadOnlyList<CharacterCombatFacts> eligibleCharacters, IReadOnlyList<StructureCombatFacts> eligibleStructures,
            IReadOnlyList<StructureCombatFacts> physicalStructures, AttackTargetingPolicy policy, WallGeometry walls, AttackOcclusionSpec occlusion)
        {
            if (actorCells == null) throw new ArgumentNullException(nameof(actorCells));
            if (raySteps == null) throw new ArgumentNullException(nameof(raySteps));
            if (eligibleCharacters == null) throw new ArgumentNullException(nameof(eligibleCharacters));
            if (eligibleStructures == null) throw new ArgumentNullException(nameof(eligibleStructures));
            if (physicalStructures == null) throw new ArgumentNullException(nameof(physicalStructures));
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (occlusion == null) throw new ArgumentNullException(nameof(occlusion));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (range < 0) throw new ArgumentOutOfRangeException(nameof(range));
            if (origin.X < 0 || origin.X >= width || origin.Y < 0 || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (raySteps.Any(step => step.X < -1 || step.X > 1 || step.Y < -1 || step.Y > 1 || step == CellCoord.Zero) ||
                raySteps.Distinct().Count() != raySteps.Count)
                throw new ArgumentException("광선은 중복 없는 한 칸 방향이어야 합니다.", nameof(raySteps));

            var encounters = new List<(EntityId, int, int)>();
            var blocked = new List<AttackBlockedContact>();
            var reachable = new List<CellCoord>();
            for (int ray = 0; ray < raySteps.Count; ray++)
            {
                var step = raySteps[ray];
                int encounter = 0;
                AttackBlockedContact blocker = null;
                for (long distance = 1; distance <= range; distance++)
                {
                    long x = origin.X + distance * step.X;
                    long y = origin.Y + distance * step.Y;
                    if (x < 0 || x >= width || y < 0 || y >= height) break;
                    var cell = new CellCoord((int)x, (int)y);
                    // 빈 칸과 인접 제외 칸에서도 벽을 검사한다. 한 번 막힌 광선은 증거 수집만 이어 간다.
                    if (blocker == null)
                        blocker = AttackOcclusionResolver.CheckDirect(origin, cell, walls, occlusion);
                    if (policy.ExcludesAdjacentDirectCells && CannonAppointExclusionCalculator.IsForbidden(actorCells, cell)) continue;
                    if (!walls.Occupies(cell))
                    {
                        if (blocker == null) reachable.Add(cell);
                        var character = eligibleCharacters.SingleOrDefault(c => c.OccupiedCells.Contains(cell));
                        var structure = eligibleStructures.SingleOrDefault(s => s.OccupiedCells.Contains(cell));
                        if (character != null || structure != null)
                        {
                            if (blocker == null) encounters.Add((character?.EntityId ?? structure.EntityId, encounter++, ray));
                            else blocked.Add(new AttackBlockedContact(origin, cell, AttackOcclusionPhase.Direct,
                                blocker.WallSourceIds, blocker.WallCells));
                        }
                    }
                    // 기존 인접 제외를 통과한 물리 구조물은 아군이어도 타격과 숨은 대상 탐색을 모두 끝낸다.
                    if (physicalStructures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell))) break;
                }
            }
            return new PierceTrace(encounters, blocked, reachable);
        }

        internal static (EntityId Id, int Encounter, int Ray)[] ApplyBudget(PierceTrace trace, int maxTargetsPerRay)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (maxTargetsPerRay < 0) throw new ArgumentOutOfRangeException(nameof(maxTargetsPerRay));
            return trace.Encounters.Where(hit => hit.Encounter < maxTargetsPerRay).ToArray();
        }
    }
}
