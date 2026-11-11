using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi
{
    public enum EnemyDecisionKind
    {
        Attack,
        Move,
        EndEnemyPhase
    }

    /// <summary>같은 입력에서 실제 정산에 넘길 행동 하나를 보관한다.</summary>
    public sealed class EnemyDecision : IEquatable<EnemyDecision>
    {
        private static readonly EnemyDecision End = new EnemyDecision(EnemyDecisionKind.EndEnemyPhase, default, default, null, default);

        public EnemyDecisionKind Kind { get; }
        public EntityId ActorId { get; }
        public AttackAim Aim { get; }
        public MovementPath Path { get; }
        public EntityId TauntSourceId { get; }

        private EnemyDecision(EnemyDecisionKind kind, EntityId actorId, AttackAim aim, MovementPath path, EntityId tauntSourceId)
        {
            Kind = kind;
            ActorId = actorId;
            Aim = aim;
            Path = path;
            TauntSourceId = tauntSourceId;
        }

        public static EnemyDecision Attack(EntityId actorId, AttackAim aim, EntityId tauntSourceId)
        {
            RequireActor(actorId);
            if (aim.Kind == AttackAimKind.None)
                throw new ArgumentException("공격 조준이 필요합니다.", nameof(aim));
            return new EnemyDecision(EnemyDecisionKind.Attack, actorId, aim, null, tauntSourceId);
        }

        public static EnemyDecision Move(EntityId actorId, MovementPath path, EntityId tauntSourceId)
        {
            RequireActor(actorId);
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.StepCount == 0) throw new ArgumentException("이동에는 한 걸음 이상이 필요합니다.", nameof(path));
            var visited = new HashSet<CellCoord> { path.Origin };
            for (int i = 1; i < path.Cells.Count; i++)
                if (!IsCardinalStep(path.Cells[i - 1], path.Cells[i]) || !visited.Add(path.Cells[i]))
                    throw new ArgumentException("이동 경로는 중복 없는 상하좌우 걸음으로 이루어져야 합니다.", nameof(path));
            return new EnemyDecision(EnemyDecisionKind.Move, actorId, default, path, tauntSourceId);
        }

        public static EnemyDecision EndEnemyPhase() => End;

        public bool Equals(EnemyDecision other)
        {
            if (other == null || Kind != other.Kind || ActorId != other.ActorId || TauntSourceId != other.TauntSourceId)
                return false;
            if (Kind == EnemyDecisionKind.Attack)
                return Aim.Kind == other.Aim.Kind && Aim.Cell == other.Aim.Cell && Aim.Direction == other.Aim.Direction;
            if (Kind != EnemyDecisionKind.Move) return true;
            if (Path.Cells.Count != other.Path.Cells.Count) return false;
            for (int i = 0; i < Path.Cells.Count; i++)
                if (Path.Cells[i] != other.Path.Cells[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is EnemyDecision other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Kind);
            hash.Add(ActorId);
            hash.Add(TauntSourceId);
            hash.Add(Aim.Kind);
            hash.Add(Aim.Cell);
            hash.Add(Aim.Direction);
            if (Path != null)
                foreach (var cell in Path.Cells) hash.Add(cell);
            return hash.ToHashCode();
        }

        private static bool IsCardinalStep(CellCoord from, CellCoord to) =>
            Math.Abs((long)to.X - from.X) + Math.Abs((long)to.Y - from.Y) == 1;

        private static void RequireActor(EntityId actorId)
        {
            if (actorId.IsEmpty) throw new ArgumentException("행위자 ID가 필요합니다.", nameof(actorId));
        }
    }
}
