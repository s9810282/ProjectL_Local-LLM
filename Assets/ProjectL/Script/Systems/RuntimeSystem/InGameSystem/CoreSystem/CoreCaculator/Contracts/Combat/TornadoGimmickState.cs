using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum TornadoGimmickPhase
    {
        Cooldown,
        Warning
    }

    public sealed class TornadoMemberState
    {
        public long SourceId { get; }
        public CellCoord Center { get; }
        public int HorizontalSign { get; }
        public TornadoGimmickPhase Phase { get; }
        public long NextBoundaryCount { get; }
        public CellCoord? LockedDirection { get; }

        public TornadoMemberState(long sourceId, CellCoord center, int horizontalSign, TornadoGimmickPhase phase,
            long nextBoundaryCount, CellCoord? lockedDirection)
        {
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (center.X < 0 || center.Y < 0) throw new ArgumentOutOfRangeException(nameof(center));
            if (horizontalSign != -1 && horizontalSign != 1) throw new ArgumentOutOfRangeException(nameof(horizontalSign));
            if (!Enum.IsDefined(typeof(TornadoGimmickPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            if (nextBoundaryCount < 1) throw new ArgumentOutOfRangeException(nameof(nextBoundaryCount));
            if ((phase == TornadoGimmickPhase.Warning) != lockedDirection.HasValue)
                throw new ArgumentException("경고 중에만 확정된 방향이 필요합니다.", nameof(lockedDirection));
            if (lockedDirection.HasValue && lockedDirection.Value != CellCoord.Zero &&
                (lockedDirection.Value.X != horizontalSign || Math.Abs((long)lockedDirection.Value.Y) != 1))
                throw new ArgumentException("경고 방향은 가로 진행 부호와 일치하는 대각선 또는 정지여야 합니다.", nameof(lockedDirection));
            SourceId = sourceId;
            Center = center;
            HorizontalSign = horizontalSign;
            Phase = phase;
            NextBoundaryCount = nextBoundaryCount;
            LockedDirection = lockedDirection;
        }

        public bool SameValues(TornadoMemberState other) => other != null && SourceId == other.SourceId &&
            Center == other.Center && HorizontalSign == other.HorizontalSign && Phase == other.Phase &&
            NextBoundaryCount == other.NextBoundaryCount && LockedDirection == other.LockedDirection;
    }

    public sealed class TornadoGimmickState : StageGimmickState
    {
        public IReadOnlyList<TornadoMemberState> Members { get; }

        public TornadoGimmickState(StageGimmickId id, IReadOnlyList<TornadoMemberState> members)
            : this(id, CopyMembers(members))
        {
        }

        private TornadoGimmickState(StageGimmickId id, TornadoMemberState[] members)
            : base(id, StageGimmickKey.Tornado, members.Min(m => m.NextBoundaryCount))
        {
            Members = Array.AsReadOnly(members);
        }

        private static TornadoMemberState[] CopyMembers(IReadOnlyList<TornadoMemberState> members)
        {
            var copy = (members ?? throw new ArgumentNullException(nameof(members))).ToArray();
            if (copy.Length == 0 || copy.Any(m => m == null) || copy.Select(m => m.SourceId).Distinct().Count() != copy.Length)
                throw new ArgumentException("토네이도 상태는 중복 없는 원본이 하나 이상 필요합니다.", nameof(members));
            return copy.OrderBy(m => m.SourceId).ToArray();
        }

        public override bool SameValues(StageGimmickState other) => other is TornadoGimmickState state &&
            Id == state.Id && Members.Count == state.Members.Count &&
            Members.Zip(state.Members, (a, b) => a.SameValues(b)).All(equal => equal);

        internal void ValidateDefinition(TornadoGimmickSpec spec, int width, int height)
        {
            if (!Members.Select(m => m.SourceId).SequenceEqual(spec.Members.Select(m => m.SourceId)))
                throw new InvalidOperationException("토네이도 정의와 상태의 원본 목록이 다릅니다.");
            for (int i = 0; i < Members.Count; i++)
            {
                var member = Members[i];
                int side = spec.Members[i].SideLength;
                if (!TornadoGeometry.Fits(member.Center, side, width, height))
                    throw new InvalidOperationException("토네이도 정사각형이 보드 밖입니다.");
                if (!member.LockedDirection.HasValue) continue;
                var direction = member.LockedDirection.Value;
                if (direction == CellCoord.Zero ? width > side && height > side :
                    !TornadoGeometry.Fits(member.Center + direction, side, width, height))
                    throw new InvalidOperationException("토네이도 경고 방향이 정사각형의 이동 가능 영역과 다릅니다.");
            }
        }
    }
}
