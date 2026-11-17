using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
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
        public MovementUndoState MovementUndo { get; }
        public StageGimmickStateSet Gimmicks { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public CombatFlowPhase Phase { get; }
        /// <summary>전투 진입 뒤인지다. 턴 국면에서 계산하며 따로 저장하지 않는다(옛 밤 판정을 대신함, 밤 삭제 설계 N2).</summary>
        public bool InCombat => Turn.StagePhase == StagePhase.Combat;
        public uint ItemRandomState { get; }
        public CombatRewardFacts Rewards { get; }
        public IReadOnlyList<StructureCombatFacts> Structures { get; }
        public ThornBombStateSet ThornBombs { get; }
        public CombatTurnFacts Turn { get; }
        public long LastPlatformBoundaryCount { get; }
        public TraitTurnFacts TraitTurn { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentProgressFacts EnvironmentProgress { get; }
        public WallBlockingSpec WallBlocking { get; }
        /// <summary>전투 준비 때 결합한 승리 목표다. 전투 동안 바뀌지 않으며 Buffer가 변경을 거절한다.</summary>
        public BattleObjective Objective { get; }
        /// <summary>새 개체 ID와 소환 난수 진행값이다. 원본은 여기 한 곳이며 Buffer가 연속성을 검사한다.</summary>
        public CombatAllocationFacts Allocation { get; }
        public NexusReinforcementState Reinforcements { get; }
        public PlayerSkillBattleState PlayerSkill { get; }

        /// <param name="objective">생략하면 본부 목표다. 운영 생성 지점은 원본 목표를 그대로 넘긴다.</param>
        /// <param name="allocation">생략하면 할당 불가 상태다. 운영 생성 지점은 원본 진행값을 그대로 넘긴다.</param>
        public CombatCommandState(int boardWidth, int boardHeight, CombatFlowPhase phase, IReadOnlyList<StructureCombatFacts> structures,
            CombatTurnFacts turn = null, MovementEnvironmentFacts environment = null, EnvironmentProgressFacts environmentProgress = null,
            WallBlockingSpec wallBlocking = null, BattleObjective objective = null, CombatAllocationFacts allocation = null,
            uint itemRandomState = 0, CombatRewardFacts rewards = null, TraitTurnFacts traitTurn = null,
            MovementUndoState movementUndo = null, StageGimmickStateSet gimmicks = null,
            NexusReinforcementState reinforcements = null, PlayerSkillBattleState playerSkill = null, long lastPlatformBoundaryCount = 0,
            ThornBombStateSet thornBombs = null)
        {
            Objective = objective ?? BattleObjective.EnemyHeadquarters;
            Allocation = allocation ?? CombatAllocationFacts.Unavailable;
            Reinforcements = reinforcements ?? NexusReinforcementState.Disabled;
            PlayerSkill = playerSkill;
            if (boardWidth < 1 || boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (!Enum.IsDefined(typeof(CombatFlowPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            Phase = phase;
            ItemRandomState = itemRandomState;
            Rewards = rewards ?? CombatRewardFacts.Empty;
            Structures = Array.AsReadOnly((structures ?? throw new ArgumentNullException(nameof(structures))).ToArray());
            Environment = environment ?? MovementEnvironmentFacts.Empty;
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            EnvironmentProgress = environmentProgress ?? EnvironmentProgressFacts.Initial(Environment);
            if (Environment.Tiles.Any(o => o.Id >= EnvironmentProgress.NextSourceId))
                throw new ArgumentException("다음 환경 식별자는 비활성 원본을 포함한 기존 식별자보다 커야 합니다.", nameof(environmentProgress));
            if (Environment.Tiles.OfType<ObjectSourceFact>().Any(o => o.EntryOrdinal >= EnvironmentProgress.NextObjectEntryOrdinal))
                throw new ArgumentException("다음 Object 순서는 비활성 원본을 포함한 기존 순서보다 커야 합니다.", nameof(environmentProgress));
            Turn = turn ?? new CombatTurnFacts(StagePhase.Deployment);
            if (lastPlatformBoundaryCount < 0 || lastPlatformBoundaryCount > Turn.CompletedControllerTurnCount)
                throw new ArgumentOutOfRangeException(nameof(lastPlatformBoundaryCount));
            LastPlatformBoundaryCount = lastPlatformBoundaryCount;
            TraitTurn = traitTurn ?? TraitTurnFacts.Empty;
            MovementUndo = movementUndo ?? MovementUndoState.Empty;
            Gimmicks = gimmicks ?? StageGimmickStateSet.Empty;
            ThornBombs = thornBombs ?? ThornBombStateSet.Empty;
            var expectedPhase = Turn.StagePhase == StagePhase.Deployment || Turn.ControllerPhase == CombatControllerPhase.Player
                ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic;
            if (phase != CombatFlowPhase.Finished && phase != CombatFlowPhase.None && phase != expectedPhase)
                throw new ArgumentException("전투 국면과 제어자 단계가 일치해야 합니다.", nameof(phase));
            if (Structures.Any(s => s == null)) throw new ArgumentException("구조물 사실은 null일 수 없습니다.");
        }

        /// <summary>지정한 사실만 바꾸고 나머지 불변 문맥은 보존한다.</summary>
        public CombatCommandState With(CombatFlowPhase? phase = null, IReadOnlyList<StructureCombatFacts> structures = null,
            CombatTurnFacts turn = null, MovementEnvironmentFacts environment = null, EnvironmentProgressFacts environmentProgress = null,
            CombatAllocationFacts allocation = null, uint? itemRandomState = null, CombatRewardFacts rewards = null,
            TraitTurnFacts traitTurn = null, MovementUndoState movementUndo = null, StageGimmickStateSet gimmicks = null,
            NexusReinforcementState reinforcements = null, PlayerSkillBattleState playerSkill = null, long? lastPlatformBoundaryCount = null,
            ThornBombStateSet thornBombs = null) =>
            new CombatCommandState(BoardWidth, BoardHeight, phase ?? Phase, structures ?? Structures, turn ?? Turn,
                environment ?? Environment, environmentProgress ?? EnvironmentProgress, WallBlocking, Objective,
                allocation ?? Allocation, itemRandomState ?? ItemRandomState, rewards ?? Rewards, traitTurn ?? TraitTurn,
                movementUndo ?? MovementUndo, gimmicks ?? Gimmicks,
                reinforcements ?? Reinforcements, playerSkill ?? PlayerSkill, lastPlatformBoundaryCount ?? LastPlatformBoundaryCount,
                thornBombs ?? ThornBombs);

        public bool IsInside(CellCoord cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < BoardWidth && cell.Y < BoardHeight;

        public bool SameValues(CombatCommandState other) => SameGameplayValues(other) && MovementUndo.SameValues(other.MovementUndo);

        public bool SameGameplayValues(CombatCommandState other)
        {
            if (other == null || BoardWidth != other.BoardWidth || BoardHeight != other.BoardHeight ||
                Phase != other.Phase ||
                ItemRandomState != other.ItemRandomState || LastPlatformBoundaryCount != other.LastPlatformBoundaryCount ||
                Structures.Count != other.Structures.Count ||
                !ThornBombs.SameValues(other.ThornBombs) || !Turn.SameValues(other.Turn) || !TraitTurn.SameValues(other.TraitTurn) ||
                !Environment.SameValues(other.Environment) || !EnvironmentProgress.SameValues(other.EnvironmentProgress) ||
                WallBlocking.CornerCutTicks != other.WallBlocking.CornerCutTicks || !Objective.SameValues(other.Objective) ||
                !Gimmicks.SameValues(other.Gimmicks) || !Allocation.SameValues(other.Allocation) ||
                !Reinforcements.SameValues(other.Reinforcements) || !Rewards.SameValues(other.Rewards) ||
                !(PlayerSkill?.SameValues(other.PlayerSkill) ?? other.PlayerSkill == null)) return false;
            for (int i = 0; i < Structures.Count; i++)
            {
                if (!Structures[i].SameValues(other.Structures[i])) return false;
            }
            return true;
        }
    }
}
