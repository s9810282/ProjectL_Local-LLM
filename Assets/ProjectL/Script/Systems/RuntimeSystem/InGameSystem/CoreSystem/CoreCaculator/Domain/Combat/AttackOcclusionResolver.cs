using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>정확한 선분 진입 순서로 첫 차단 벽을 선택한다. 피해와 대상 자격은 판정하지 않는다.</summary>
    internal static class AttackOcclusionResolver
    {
        internal static AttackBlockedContact CheckDirect(CellCoord attacker, CellCoord contact, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return FindBlock(attacker, contact, walls, AttackOcclusionPhase.Direct, policy.Delivery == AttackWallDelivery.IgnoreWalls);
        }

        internal static AttackBlockedContact CheckPropagation(CellCoord impact, CellCoord contact, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return FindBlock(impact, contact, walls, AttackOcclusionPhase.Propagation, policy.Propagation == AttackWallPropagation.IgnoreWalls);
        }

        /// <summary>직접 전달이 성공한 중심에서만 파급을 검사한다. 원본 마스크는 변경하지 않는다.</summary>
        internal static (AttackCellSelection Cells, IReadOnlyList<AttackBlockedContact> BlockedContacts) ResolveCells(
            CellCoord attacker, AttackCellSelection selected, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var blocked = new List<AttackBlockedContact>();
            var direct = FilterCells(attacker, selected.DesignatedCells.Span, walls, policy, false, blocked);
            CellCoord? primary = selected.PrimaryCell.HasValue && direct.Length != 0 ? selected.PrimaryCell : null;
            var additional = Array.Empty<CellCoord>();
            if (primary.HasValue)
            {
                var propagation = ResolvePropagationCells(primary.Value, selected.AdditionalCells.Span, walls, policy);
                additional = propagation.Cells;
                blocked.AddRange(propagation.BlockedContacts);
            }
            return (new AttackCellSelection(primary, direct, additional), blocked.AsReadOnly());
        }

        /// <summary>특성이 만든 파급 셀에도 같은 벽 정책을 적용한다. 생성 범위와 효과 정산은 호출자가 소유한다.</summary>
        internal static (CellCoord[] Cells, IReadOnlyList<AttackBlockedContact> BlockedContacts) ResolvePropagationCells(
            CellCoord impact, ReadOnlySpan<CellCoord> cells, WallGeometry walls, AttackOcclusionSpec policy)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var blocked = new List<AttackBlockedContact>();
            return (FilterCells(impact, cells, walls, policy, true, blocked), blocked.AsReadOnly());
        }

        private static CellCoord[] FilterCells(CellCoord origin, ReadOnlySpan<CellCoord> cells, WallGeometry walls,
            AttackOcclusionSpec policy, bool propagation, List<AttackBlockedContact> blocked)
        {
            var reachable = new List<CellCoord>();
            foreach (var cell in cells)
            {
                // 벽 무시는 선분 규칙만 바꾼다. 벽이 점유한 셀은 효과를 놓을 수 없다.
                if (walls.Occupies(cell))
                {
                    var occupying = walls.Runs.First(run => run.Cells.Contains(cell));
                    blocked.Add(new AttackBlockedContact(origin, cell,
                        propagation ? AttackOcclusionPhase.Propagation : AttackOcclusionPhase.Direct,
                        occupying.SourceIds, occupying.Cells));
                    continue;
                }
                var contact = propagation ? CheckPropagation(origin, cell, walls, policy) : CheckDirect(origin, cell, walls, policy);
                if (contact == null) reachable.Add(cell);
                else blocked.Add(contact);
            }
            return reachable.ToArray();
        }

        internal static AttackBlockedContact FindBlock(CellCoord origin, CellCoord contact, WallGeometry walls,
            AttackOcclusionPhase phase, bool ignoreWalls)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (phase != AttackOcclusionPhase.Direct && phase != AttackOcclusionPhase.Propagation)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (ignoreWalls) return null;
            WallRun nearest = null;
            var nearestEntry = SegmentParameter.One;
            foreach (var run in walls.Runs)
            {
                if (!WallSegmentClipper.IntersectsInterior(origin, contact, run, out var entry)) continue;
                int order = entry.CompareTo(nearestEntry);
                if (nearest != null && (order > 0 || order == 0 && run.ComponentKey >= nearest.ComponentKey)) continue;
                nearest = run;
                nearestEntry = entry;
            }
            return nearest == null ? null : new AttackBlockedContact(origin, contact, phase, nearest.SourceIds, nearest.Cells);
        }
    }
}
