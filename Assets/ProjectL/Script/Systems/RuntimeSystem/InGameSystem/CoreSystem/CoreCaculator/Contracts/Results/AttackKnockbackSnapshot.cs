using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>넉백이 대상에서 어떻게 끝났는지입니다.</summary>
    public enum AttackKnockbackStop
    {
        /// <summary>요청 거리를 모두 이동했습니다.</summary>
        Completed = 1,
        /// <summary>다음 칸이 막혀 그 앞에서 멈췄습니다. 첫 칸부터 막히면 경로가 비어 있습니다.</summary>
        Blocked = 2,
        /// <summary>
        /// 넉백 진행이 중단됐습니다. 대상 사망을 뜻하지 않습니다. 칸 효과로 전투가 끝나면 살아 있는 대상도 이 값이 됩니다.
        /// 사망은 대상 미리보기의 TargetKilled로 봅니다.
        /// </summary>
        Interrupted = 3,
        /// <summary>넉백 면역이라 이동하지 않았습니다.</summary>
        Immune = 4,
    }

    public enum AttackKnockbackTargetKind
    {
        Character = 1,
        Structure = 2,
    }

    /// <summary>
    /// 이 공격의 넉백 요약입니다. 칸 단위 재생 원본은 Moved 사건입니다(설계 2026-10-27 §5.3).
    /// 다칸 대상의 최종 점유 칸은 현재 점유 칸에 FinalAnchor - StartAnchor를 더해 얻습니다.
    /// </summary>
    public sealed class AttackKnockbackSnapshot
    {
        public EntityId TargetId { get; }
        public AttackKnockbackTargetKind TargetKind { get; }
        public CellCoord StartAnchor { get; }
        /// <summary>한 칸 방향입니다. 각 성분은 -1·0·1이며 0벡터가 아닙니다.</summary>
        public CellCoord Step { get; }
        public int RequestedDistance { get; }
        /// <summary>실제로 들어간 칸입니다. 비어 있을 수 있습니다.</summary>
        public IReadOnlyList<CellCoord> Path { get; }
        /// <summary>경로의 마지막 칸이며, 경로가 비었으면 시작 칸입니다.</summary>
        public CellCoord FinalAnchor { get; }
        public AttackKnockbackStop Stop { get; }

        public AttackKnockbackSnapshot(EntityId targetId, AttackKnockbackTargetKind targetKind, CellCoord startAnchor,
            CellCoord step, int requestedDistance, IReadOnlyList<CellCoord> path, AttackKnockbackStop stop)
        {
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            if (!Enum.IsDefined(typeof(AttackKnockbackTargetKind), targetKind)) throw new ArgumentOutOfRangeException(nameof(targetKind));
            if (!Enum.IsDefined(typeof(AttackKnockbackStop), stop)) throw new ArgumentOutOfRangeException(nameof(stop));
            if (Math.Abs(step.X) > 1 || Math.Abs(step.Y) > 1 || step == CellCoord.Zero)
                throw new ArgumentOutOfRangeException(nameof(step), "넉백 방향은 0이 아닌 한 칸 방향이어야 합니다.");
            if (requestedDistance < 1) throw new ArgumentOutOfRangeException(nameof(requestedDistance));
            var copy = (path ?? throw new ArgumentNullException(nameof(path))).ToArray();
            var expected = startAnchor;
            foreach (var cell in copy)
            {
                expected += step;
                if (cell != expected)
                    throw new ArgumentException("넉백 경로는 시작 칸에서 방향대로 한 칸씩 이어져야 합니다.", nameof(path));
            }
            bool countMatchesStop = stop switch
            {
                AttackKnockbackStop.Completed => copy.Length == requestedDistance,
                AttackKnockbackStop.Blocked => copy.Length < requestedDistance,
                AttackKnockbackStop.Interrupted => copy.Length >= 1 && copy.Length <= requestedDistance,
                _ => copy.Length == 0,
            };
            if (!countMatchesStop)
                throw new ArgumentException("넉백 경로의 칸 수가 멈춘 이유와 맞지 않습니다.", nameof(path));
            TargetId = targetId;
            TargetKind = targetKind;
            StartAnchor = startAnchor;
            Step = step;
            RequestedDistance = requestedDistance;
            Path = Array.AsReadOnly(copy);
            FinalAnchor = copy.Length == 0 ? startAnchor : copy[copy.Length - 1];
            Stop = stop;
        }
    }
}
