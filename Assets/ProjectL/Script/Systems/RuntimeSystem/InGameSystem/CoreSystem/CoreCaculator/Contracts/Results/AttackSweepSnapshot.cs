using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>공격 피해에 앞서 확정한 쓸기의 표시 근거입니다.</summary>
    public sealed class AttackSweepSnapshot
    {
        public EffectOriginKey Origin { get; }
        public EntityId ActorId { get; }
        public Rotation4 Facing { get; }
        public AttackSweepSide Side { get; }
        /// <summary>명목 목적지 계산에 사용한 전체 공격 면적입니다. 보드 밖 칸을 포함할 수 있습니다.</summary>
        public IReadOnlyList<CellCoord> AreaCells { get; }
        public IReadOnlyList<EntityId> CapturedTargetIds { get; }
        public IReadOnlyList<AttackSweepTargetSnapshot> Targets { get; }
        public bool Interrupted { get; }

        public AttackSweepSnapshot(EffectOriginKey origin, EntityId actorId, Rotation4 facing, AttackSweepSide side,
            IReadOnlyList<CellCoord> areaCells, IReadOnlyList<EntityId> capturedTargetIds,
            IReadOnlyList<AttackSweepTargetSnapshot> targets, bool interrupted)
        {
            if (actorId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(actorId));
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId != actorId.Value ||
                origin.DefinitionKey != (int)TraitKey.DeadHand)
                throw new ArgumentException("죽음의 손 출처와 공격자가 일치해야 합니다.", nameof(origin));
            if (!Enum.IsDefined(typeof(Rotation4), facing)) throw new ArgumentOutOfRangeException(nameof(facing));
            AttackSweepSideValidation.Validate(side);

            var area = (areaCells ?? throw new ArgumentNullException(nameof(areaCells))).ToArray();
            var captured = (capturedTargetIds ?? throw new ArgumentNullException(nameof(capturedTargetIds))).ToArray();
            var outcomes = (targets ?? throw new ArgumentNullException(nameof(targets))).ToArray();
            var areaSet = new HashSet<CellCoord>(area);
            if (areaSet.Count != area.Length)
                throw new ArgumentException("쓸기 범위에 같은 칸을 두 번 넣을 수 없습니다.", nameof(areaCells));
            var remaining = new HashSet<EntityId>(captured);
            if (remaining.Count != captured.Length || remaining.Contains(EntityId.Empty) || remaining.Contains(actorId))
                throw new ArgumentException("쓸기 대상으로 빈 ID, 공격자 또는 중복 대상을 포착할 수 없습니다.", nameof(capturedTargetIds));

            var forward = facing switch
            {
                Rotation4.R0 => CellCoord.Up,
                Rotation4.R90 => CellCoord.Right,
                Rotation4.R180 => CellCoord.Down,
                _ => CellCoord.Left
            };
            // 같은 줄은 목적지에 가까운 대상부터 처리합니다.
            var lateral = side == AttackSweepSide.Right
                ? new CellCoord(forward.Y, -forward.X)
                : new CellCoord(-forward.Y, forward.X);
            var rowEdges = new Dictionary<long, long>();
            foreach (var cell in area)
            {
                long row = Project(cell, forward);
                long position = Project(cell, lateral);
                if (!rowEdges.TryGetValue(row, out var edge) || position > edge)
                    rowEdges[row] = position;
            }
            var lastInRow = new Dictionary<long, long>();
            bool stopped = false;
            foreach (var target in outcomes)
            {
                if (target == null || !remaining.Remove(target.TargetId) || !areaSet.Contains(target.StartAnchor))
                    throw new ArgumentException("포착한 범위와 대상마다 결과 하나가 필요합니다.", nameof(targets));
                long row = Project(target.StartAnchor, forward);
                long position = Project(target.StartAnchor, lateral);
                if (row != Project(target.IntendedEndpoint, forward) ||
                    Project(target.IntendedEndpoint, lateral) != rowEdges[row] + 1 ||
                    lastInRow.TryGetValue(row, out var previous) && previous <= position)
                    throw new ArgumentException("같은 줄의 대상과 목적지는 쓸기 방향 및 처리 순서를 따라야 합니다.", nameof(targets));
                lastInRow[row] = position;
                if (target.Stop == AttackSweepStop.NotProcessed)
                {
                    if (!interrupted) throw new ArgumentException("미처리 대상은 중단된 쓸기에만 기록합니다.", nameof(targets));
                    stopped = true;
                }
                else if (stopped)
                {
                    throw new ArgumentException("미처리 대상 뒤에 처리 결과를 넣을 수 없습니다.", nameof(targets));
                }
            }
            if (remaining.Count != 0)
                throw new ArgumentException("포착한 모든 대상의 결과가 필요합니다.", nameof(targets));

            Origin = origin;
            ActorId = actorId;
            Facing = facing;
            Side = side;
            AreaCells = Array.AsReadOnly(area);
            CapturedTargetIds = Array.AsReadOnly(captured);
            Targets = Array.AsReadOnly(outcomes);
            Interrupted = interrupted;
        }

        internal void ValidateActor(EntityId attackerId)
        {
            if (ActorId != attackerId)
                throw new ArgumentException("쓸기 기록의 공격자가 공격 결과와 다릅니다.", "sweep");
        }

        private static long Project(CellCoord cell, CellCoord axis) =>
            (long)cell.X * axis.X + (long)cell.Y * axis.Y;
    }
}
