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

    /// <summary>강제 이동이 다음 칸에 들어가지 못한 이유입니다. 작렬 같은 넉백 변형의 부딪힘 종류이기도 합니다.</summary>
    public enum AttackKnockbackCollisionKind
    {
        /// <summary>맵 끝, 벽 지형, 낮은 칸에서 고지대로 들어가려는 턱입니다.</summary>
        Wall = 1,
        /// <summary>살아 있는 구조물(장애물·화약통·본부 등)입니다.</summary>
        Structure = 2,
        /// <summary>살아 있는 필드 캐릭터입니다.</summary>
        Character = 3,
    }

    /// <summary>
    /// 넉백이 막혀 멈춘 곳에서 부딪힘 피해가 난 사실입니다. 부딪힘 피해를 내는 넉백(작렬)일 때만 넉백 요약에 붙습니다.
    /// 피해량은 체력 사건과 발동 사건(<see cref="TraitActivationKind.CrashImpactCollided"/>)이 나타냅니다.
    /// </summary>
    public sealed class AttackKnockbackCollision
    {
        public AttackKnockbackCollisionKind Kind { get; }
        /// <summary>막은 칸입니다. 맵 밖일 수 있습니다.</summary>
        public CellCoord Cell { get; }
        /// <summary>부딪힌 구조물·캐릭터입니다. 벽이면 비어 있습니다.</summary>
        public EntityId? EntityId { get; }

        public AttackKnockbackCollision(AttackKnockbackCollisionKind kind, CellCoord cell, EntityId? entityId)
        {
            if (!Enum.IsDefined(typeof(AttackKnockbackCollisionKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == AttackKnockbackCollisionKind.Wall ? entityId.HasValue : !entityId.HasValue || entityId.Value.IsEmpty)
                throw new ArgumentException("벽 부딪힘은 개체가 없고, 구조물·캐릭터 부딪힘은 개체가 있어야 합니다.", nameof(entityId));
            Kind = kind;
            Cell = cell;
            EntityId = entityId;
        }
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
        /// <summary>막혀 멈춘 곳에서 부딪힘 피해가 났으면 그 사실입니다. 일반 넉백·완주·면역·중단이면 비어 있습니다.</summary>
        public AttackKnockbackCollision Collision { get; }

        public AttackKnockbackSnapshot(EntityId targetId, AttackKnockbackTargetKind targetKind, CellCoord startAnchor,
            CellCoord step, int requestedDistance, IReadOnlyList<CellCoord> path, AttackKnockbackStop stop,
            AttackKnockbackCollision collision = null)
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
            if (collision != null)
            {
                var blocked = (copy.Length == 0 ? startAnchor : copy[copy.Length - 1]) + step;
                if (stop != AttackKnockbackStop.Blocked || collision.Cell != blocked || targetKind != AttackKnockbackTargetKind.Character)
                    throw new ArgumentException("부딪힘은 막혀 멈춘 캐릭터 넉백의 다음 칸에서만 납니다.", nameof(collision));
            }
            TargetId = targetId;
            TargetKind = targetKind;
            StartAnchor = startAnchor;
            Step = step;
            RequestedDistance = requestedDistance;
            Path = Array.AsReadOnly(copy);
            FinalAnchor = copy.Length == 0 ? startAnchor : copy[copy.Length - 1];
            Stop = stop;
            Collision = collision;
        }
    }
}
