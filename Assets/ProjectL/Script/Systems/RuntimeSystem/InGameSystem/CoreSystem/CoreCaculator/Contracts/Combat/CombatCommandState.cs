using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>명령에서 소비하는 전투 사실이다. 원본은 기존 CombatStateBuffer에 둔다.</summary>
    public sealed class CombatCommandState
    {
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public CombatFlowPhase Phase { get; }
        public EntityId ActiveActorId { get; }
        public bool IsNight { get; }
        public IReadOnlyList<StructureCombatFacts> Structures { get; }
        public IReadOnlyList<ThornFieldCellSnapshot> ThornFields { get; }
        public IReadOnlyList<CombatTimelineEntrySnapshot> Timeline { get; }
        public CombatTurnFacts Turn { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentProgressFacts EnvironmentProgress { get; }
        public WallBlockingSpec WallBlocking { get; }

        public CombatCommandState(int boardWidth, int boardHeight, CombatFlowPhase phase,
            EntityId activeActorId, bool isNight, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyList<ThornFieldCellSnapshot> thornFields, IReadOnlyList<CombatTimelineEntrySnapshot> timeline,
            CombatTurnFacts turn = null, MovementEnvironmentFacts environment = null, EnvironmentProgressFacts environmentProgress = null,
            WallBlockingSpec wallBlocking = null)
        {
            if (boardWidth < 1 || boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (!Enum.IsDefined(typeof(CombatFlowPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            Phase = phase;
            ActiveActorId = activeActorId;
            IsNight = isNight;
            Structures = Array.AsReadOnly((structures ?? throw new ArgumentNullException(nameof(structures))).ToArray());
            ThornFields = Array.AsReadOnly((thornFields ?? throw new ArgumentNullException(nameof(thornFields))).ToArray());
            Timeline = Array.AsReadOnly((timeline ?? throw new ArgumentNullException(nameof(timeline))).ToArray());
            Environment = environment ?? MovementEnvironmentFacts.Empty;
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            EnvironmentProgress = environmentProgress ?? EnvironmentProgressFacts.Initial(Environment);
            if (Environment.Tiles.Any(o => o.Id >= EnvironmentProgress.NextSourceId))
                throw new ArgumentException("다음 환경 식별자는 비활성 원본을 포함한 기존 식별자보다 커야 합니다.", nameof(environmentProgress));
            if (Environment.Tiles.OfType<ObjectSourceFact>().Any(o => o.EntryOrdinal >= EnvironmentProgress.NextObjectEntryOrdinal))
                throw new ArgumentException("다음 Object 순서는 비활성 원본을 포함한 기존 순서보다 커야 합니다.", nameof(environmentProgress));
            Turn = turn ?? new CombatTurnFacts(activeActorId.IsEmpty && phase == CombatFlowPhase.PlayerInput
                ? StagePhase.Deployment : StagePhase.Combat);
            if (Structures.Any(s => s == null)) throw new ArgumentException("구조물 사실은 null일 수 없습니다.");
        }

        public bool IsInside(CellCoord cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < BoardWidth && cell.Y < BoardHeight;

        public bool SameValues(CombatCommandState other)
        {
            if (other == null || BoardWidth != other.BoardWidth || BoardHeight != other.BoardHeight ||
                Phase != other.Phase || ActiveActorId != other.ActiveActorId || IsNight != other.IsNight ||
                Structures.Count != other.Structures.Count || !ThornFields.SequenceEqual(other.ThornFields) ||
                !Timeline.SequenceEqual(other.Timeline) || !Turn.SameValues(other.Turn) ||
                !Environment.SameValues(other.Environment) || !EnvironmentProgress.SameValues(other.EnvironmentProgress) ||
                WallBlocking.CornerCutTicks != other.WallBlocking.CornerCutTicks) return false;
            for (int i = 0; i < Structures.Count; i++)
            {
                var a = Structures[i]; var b = other.Structures[i];
                if (a.EntityId != b.EntityId || a.Definition != b.Definition || a.Owner != b.Owner ||
                    !a.Health.Equals(b.Health) || a.Anchor != b.Anchor || a.Rotation != b.Rotation ||
                    !a.OccupiedCells.SequenceEqual(b.OccupiedCells)) return false;
            }
            return true;
        }
    }
}
