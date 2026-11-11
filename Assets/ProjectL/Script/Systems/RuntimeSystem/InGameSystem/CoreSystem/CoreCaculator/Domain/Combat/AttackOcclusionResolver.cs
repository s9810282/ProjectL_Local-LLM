using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 정확한 선분 진입 순서로 첫 차단 벽을 선택한다. 피해와 대상 자격은 판정하지 않는다.
    /// 직격의 Wall 무시는 유닛 정책(<see cref="AttackWallDelivery.IgnoreWalls"/>) 또는 고저 조건(<see cref="AttackElevation"/>)에서 온다. 파급은 유닛 정책만 본다.
    /// </summary>
    internal static class AttackOcclusionResolver
    {
        internal static AttackBlockedContact CheckDirect(CellCoord attacker, CellCoord contact, WallGeometry walls,
            AttackElevation elevation, AttackOcclusionSpec policy)
        {
            AttackBlockedContact result = null;
            foreach (var _ in CheckDirectSteps(attacker, contact, walls, elevation, policy, value => result = value)) { }
            return result;
        }

        internal static IEnumerable<string> CheckDirectSteps(CellCoord attacker, CellCoord contact, WallGeometry walls,
            AttackElevation elevation, AttackOcclusionSpec policy, Action<AttackBlockedContact> completed)
        {
            if (elevation == null) throw new ArgumentNullException(nameof(elevation));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return FindBlockSteps(attacker, contact, walls, AttackOcclusionPhase.Direct,
                policy.Delivery == AttackWallDelivery.IgnoreWalls || elevation.BypassesWalls(attacker, contact), completed);
        }

        internal static AttackBlockedContact CheckPropagation(CellCoord impact, CellCoord contact, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return FindBlock(impact, contact, walls, AttackOcclusionPhase.Propagation, policy.Propagation == AttackWallPropagation.IgnoreWalls);
        }

        /// <summary>직접 전달이 성공한 중심에서만 파급을 검사한다. 원본 마스크는 변경하지 않는다.</summary>
        internal static (AttackCellSelection Cells, IReadOnlyList<AttackBlockedContact> BlockedContacts) ResolveCells(
            CellCoord attacker, AttackCellSelection selected, WallGeometry walls, AttackElevation elevation, AttackOcclusionSpec policy)
        {
            (AttackCellSelection, IReadOnlyList<AttackBlockedContact>) result = default;
            foreach (var _ in ResolveCellSteps(attacker, selected, walls, elevation, policy, (cells, blocked) => result = (cells, blocked))) { }
            return result;
        }

        internal static IEnumerable<string> ResolveCellSteps(CellCoord attacker, AttackCellSelection selected, WallGeometry walls,
            AttackElevation elevation, AttackOcclusionSpec policy, Action<AttackCellSelection, IReadOnlyList<AttackBlockedContact>> completed)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (elevation == null) throw new ArgumentNullException(nameof(elevation));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var blocked = new List<AttackBlockedContact>();
            var direct = new List<CellCoord>();
            for (int i = 0; i < selected.DesignatedCells.Length; i++)
            {
                var cell = selected.DesignatedCells.Span[i];
                var contact = OccupiedContact(attacker, cell, walls, AttackOcclusionPhase.Direct);
                if (contact == null)
                    foreach (var step in CheckDirectSteps(attacker, cell, walls, elevation, policy, value => contact = value))
                        yield return step;
                if (contact == null) direct.Add(cell);
                else blocked.Add(contact);
                yield return "SelectionOcclusionCell";
            }
            CellCoord? primary = selected.PrimaryCell.HasValue && direct.Count != 0 ? selected.PrimaryCell : null;
            var additional = new List<CellCoord>();
            if (primary.HasValue)
            {
                for (int i = 0; i < selected.AdditionalCells.Length; i++)
                {
                    var cell = selected.AdditionalCells.Span[i];
                    var contact = OccupiedContact(primary.Value, cell, walls, AttackOcclusionPhase.Propagation);
                    if (contact == null)
                        foreach (var step in FindBlockSteps(primary.Value, cell, walls, AttackOcclusionPhase.Propagation,
                            policy.Propagation == AttackWallPropagation.IgnoreWalls, value => contact = value)) yield return step;
                    if (contact == null) additional.Add(cell);
                    else blocked.Add(contact);
                    yield return "SelectionOcclusionCell";
                }
            }
            completed(new AttackCellSelection(primary, direct.ToArray(), additional.ToArray()), blocked.AsReadOnly());
            yield return "SelectionOcclusionComplete";
        }

        /// <summary>특성이 만든 파급 셀에도 같은 벽 정책을 적용한다. 생성 범위와 효과 정산은 호출자가 소유한다.</summary>
        internal static (CellCoord[] Cells, IReadOnlyList<AttackBlockedContact> BlockedContacts) ResolvePropagationCells(
            CellCoord impact, ReadOnlySpan<CellCoord> cells, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var blocked = new List<AttackBlockedContact>();
            return (FilterCells(impact, cells, walls, AttackOcclusionPhase.Propagation,
                cell => CheckPropagation(impact, cell, walls, policy), blocked), blocked.AsReadOnly());
        }

        /// <summary>
        /// 벽 점유 칸을 먼저 거르고, 남은 칸은 단계별 선분 검사(<paramref name="checkSegment"/>)로 거른다.
        /// 단계마다 검사가 다르다: 직격은 유닛 정책 + 고저 조건, 파급은 유닛 정책만(설계 D2).
        /// </summary>
        private static CellCoord[] FilterCells(CellCoord origin, ReadOnlySpan<CellCoord> cells, WallGeometry walls,
            AttackOcclusionPhase phase, Func<CellCoord, AttackBlockedContact> checkSegment, List<AttackBlockedContact> blocked)
        {
            var reachable = new List<CellCoord>();
            foreach (var cell in cells)
            {
                var contact = OccupiedContact(origin, cell, walls, phase) ?? checkSegment(cell);
                if (contact == null) reachable.Add(cell);
                else blocked.Add(contact);
            }
            return reachable.ToArray();
        }

        // 벽 무시는 선분 규칙만 바꾼다. 벽이 점유한 셀은 효과를 놓을 수 없다.
        private static AttackBlockedContact OccupiedContact(CellCoord origin, CellCoord cell, WallGeometry walls,
            AttackOcclusionPhase phase)
        {
            if (!walls.TryGetOccupyingRun(cell, out var occupying)) return null;
            return new AttackBlockedContact(origin, cell, phase, occupying.SourceIds, occupying.Cells);
        }

        internal static AttackBlockedContact FindBlock(CellCoord origin, CellCoord contact, WallGeometry walls,
            AttackOcclusionPhase phase, bool ignoreWalls)
        {
            AttackBlockedContact result = null;
            foreach (var _ in FindBlockSteps(origin, contact, walls, phase, ignoreWalls, value => result = value)) { }
            return result;
        }

        private static IEnumerable<string> FindBlockSteps(CellCoord origin, CellCoord contact, WallGeometry walls,
            AttackOcclusionPhase phase, bool ignoreWalls, Action<AttackBlockedContact> completed)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (phase != AttackOcclusionPhase.Direct && phase != AttackOcclusionPhase.Propagation)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (ignoreWalls || walls.Runs.Count == 0)
            {
                completed(null);
                yield break;
            }

            WallRun nearest = null;
            var nearestEntry = SegmentParameter.One;
            foreach (var run in walls.Runs)
                foreach (var part in run.Parts)
                {
                    if (WallSegmentClipper.IntersectsPart(origin, contact, part, out var entry))
                    {
                        int order = entry.CompareTo(nearestEntry);
                        if (nearest == null || order < 0 || order == 0 && run.ComponentKey < nearest.ComponentKey)
                        {
                            nearest = run;
                            nearestEntry = entry;
                        }
                    }
                    yield return "SelectionWallPart";
                }

            if (nearest == null)
            {
                completed(null);
                yield break;
            }
            completed(new AttackBlockedContact(origin, contact, phase, nearest.SourceIds, nearest.Cells));
            yield return "SelectionWallEvidence";
        }
    }
}
