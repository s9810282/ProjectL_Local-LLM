using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>관찰자에게 허용된 현재 영역이다. 칸은 권위 있는 중심과 크기에서 한 번 파생한다.</summary>
    public sealed class TornadoAreaFact
    {
        public StageGimmickId GimmickId { get; }
        public long SourceId { get; }
        public CellCoord Center { get; }
        public int SideLength { get; }
        public IReadOnlyList<CellCoord> Cells { get; }
        public TornadoGimmickPhase Phase { get; }
        public long NextBoundaryCount { get; }
        public CellCoord? WarningDirection { get; }
        /// <summary>경고로 확정된 이동의 도착 중심이다. 경고 중이고 활성일 때만 있으며, 갈 곳이 없으면 지금 중심이다.</summary>
        public CellCoord? Destination { get; }
        /// <summary>도착 중심의 정사각형 칸이다. 도착 중심이 없으면 비어 있다.</summary>
        public IReadOnlyList<CellCoord> DestinationCells { get; }
        public bool IsActive { get; }
        private TornadoAreaFact(StageGimmickId id, TornadoMemberState state, TornadoMemberSpec spec, int width, int height, bool active)
        {
            GimmickId = id; SourceId = state.SourceId; Center = state.Center; SideLength = spec.SideLength;
            Cells = TornadoGeometry.Footprint(Center, SideLength);
            Phase = state.Phase; NextBoundaryCount = state.NextBoundaryCount; IsActive = active;
            WarningDirection = active && state.LockedDirection != CellCoord.Zero ? state.LockedDirection : null;
            // 도착 칸은 실제 이동(TornadoGimmickRule)과 같은 경로 함수에서 얻는다. 전투가 도중에 끝나는 경우만 다르다.
            if (active && state.LockedDirection.HasValue)
            {
                var path = TornadoGeometry.Travel(Center, SideLength, spec.DiagonalDistance, width, height, state.LockedDirection.Value);
                Destination = path[path.Count - 1];
                DestinationCells = TornadoGeometry.Footprint(Destination.Value, SideLength);
            }
            else
            {
                DestinationCells = Array.Empty<CellCoord>();
            }
        }
        public bool SameValues(TornadoAreaFact other) => other != null && GimmickId == other.GimmickId &&
            SourceId == other.SourceId && Center == other.Center && SideLength == other.SideLength && Phase == other.Phase &&
            NextBoundaryCount == other.NextBoundaryCount && WarningDirection == other.WarningDirection &&
            Destination == other.Destination && IsActive == other.IsActive;

        public static IReadOnlyList<TornadoAreaFact> Read(CombatCommandState state, IReadOnlyList<StageGimmickSpec> definitions, Owner observer)
        {
            if (state == null || definitions == null) throw new ArgumentNullException();
            if (observer != Owner.Player && observer != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(observer));
            var group = state.Gimmicks.Entries.OfType<TornadoGimmickState>().SingleOrDefault();
            if (group == null) return Array.Empty<TornadoAreaFact>();
            var spec = (TornadoGimmickSpec)definitions[group.Id.Value - 1];
            bool active = state.Turn.StagePhase == StagePhase.Combat && state.Phase != CombatFlowPhase.Finished;
            return Array.AsReadOnly(spec.Members.Where(m => m.KnownTo.Contains(observer)).Select(m =>
                new TornadoAreaFact(group.Id, group.Members.Single(s => s.SourceId == m.SourceId), m,
                    state.BoardWidth, state.BoardHeight, active)).ToArray());
        }
    }
}
