using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class CombatPreparation
    {
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int ReserveCapacity { get; }
        public long NightLength { get; }
        public int TimelineSeed { get; }
        public CombatFlowPhase InitialPhase { get; }
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        public int PlayerDeploymentDepth { get; }
        public int EnemyDeploymentDepth { get; }
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> StageGimmicks { get; }
        public int? ManualDrawGoldCost { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public WallBlockingSpec WallBlocking { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        /// <summary>이 스테이지의 시작 골드(수동 드로우 전용). 스테이지 경제가 없으면 0이다. Run 재화와 별개다.</summary>
        public int InitialStageGold { get; }

        public CombatPreparation(int boardWidth, int boardHeight, int reserveCapacity,
            long nightLength, int timelineSeed, CombatFlowPhase initialPhase,
            IReadOnlyList<CharacterInstanceId> drawOrder, int drawCursor = 0,
            int playerDeploymentDepth = 0, int enemyDeploymentDepth = 0,
            CellCoord[] railCells = null, StageGimmickSpec[] stageGimmicks = null, int? manualDrawGoldCost = null,
            MovementEnvironmentFacts environment = null, EnvironmentSemanticBindings environmentBindings = null,
            IReadOnlyList<ObstacleContentProfile> obstacleContents = null, int initialStageGold = 0,
            WallBlockingSpec wallBlocking = null)
        {
            if (initialStageGold < 0)
                throw new ArgumentOutOfRangeException(nameof(initialStageGold));
            if (boardWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1)
                throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (reserveCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
            if (nightLength < 1)
                throw new ArgumentOutOfRangeException(nameof(nightLength));
            if (initialPhase != CombatFlowPhase.PlayerInput &&
                initialPhase != CombatFlowPhase.Automatic)
                throw new ArgumentOutOfRangeException(nameof(initialPhase));
            if (drawOrder == null)
                throw new ArgumentNullException(nameof(drawOrder));
            if (drawCursor < 0 || drawCursor > drawOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(drawCursor));
            if (playerDeploymentDepth < 0 || enemyDeploymentDepth < 0 ||
                (long)playerDeploymentDepth + enemyDeploymentDepth >= boardHeight)
                throw new ArgumentOutOfRangeException(nameof(playerDeploymentDepth));

            var copy = new CharacterInstanceId[drawOrder.Count];
            var ids = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < copy.Length; i++)
            {
                CharacterInstanceId id = drawOrder[i];
                if (id.IsEmpty)
                    throw new ArgumentException("드로우 순서에 빈 보유 ID를 넣을 수 없습니다.", nameof(drawOrder));
                if (ids.Add(id) == false)
                    throw new ArgumentException("드로우 순서에 같은 보유 ID를 두 번 넣을 수 없습니다.", nameof(drawOrder));

                copy[i] = id;
            }

            BoardWidth = boardWidth;
            if (manualDrawGoldCost < 0) throw new ArgumentOutOfRangeException(nameof(manualDrawGoldCost));
            ManualDrawGoldCost = manualDrawGoldCost;
            InitialStageGold = initialStageGold;
            Environment = environment ?? MovementEnvironmentFacts.Empty;
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            var obstacleCopy = (obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray();
            if (obstacleCopy.Any(p => p == null) || obstacleCopy.Select(p => p.EntityId).Distinct().Count() != obstacleCopy.Length)
                throw new ArgumentException("장애물 프로필은 null이나 중복을 포함할 수 없습니다.");
            ObstacleContents = Array.AsReadOnly(obstacleCopy);
            BoardHeight = boardHeight;
            ReserveCapacity = reserveCapacity;
            NightLength = nightLength;
            TimelineSeed = timelineSeed;
            InitialPhase = initialPhase;
            DrawOrder = Array.AsReadOnly(copy);
            DrawCursor = drawCursor;
            PlayerDeploymentDepth = playerDeploymentDepth;
            EnemyDeploymentDepth = enemyDeploymentDepth;
            RailCells = Array.AsReadOnly((CellCoord[])(railCells ?? Array.Empty<CellCoord>()).Clone());
            StageGimmicks = Array.AsReadOnly((StageGimmickSpec[])(stageGimmicks ?? Array.Empty<StageGimmickSpec>()).Clone());
        }
    }

    public sealed class BattlePreparation
    {
        public IReadOnlyList<CharacterCombatFacts> Characters { get; }
        public CombatPreparation Combat { get; }
        public IReadOnlyList<StructureCombatFacts> Structures { get; }
        public long NextEntityId { get; }
        public CombatCommandState CommandState { get; }

        public BattlePreparation(IReadOnlyList<CharacterCombatFacts> characters,
            CombatPreparation combat, IReadOnlyList<StructureCombatFacts> structures = null,
            long? nextEntityId = null, CombatCommandState commandState = null)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (characters.Count == 0)
                throw new ArgumentException("전투 준비에는 캐릭터가 한 명 이상 필요합니다.", nameof(characters));

            var copy = new CharacterCombatFacts[characters.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = characters[i] ?? throw new ArgumentException(
                    "전투 준비 캐릭터에는 null을 넣을 수 없습니다.", nameof(characters));

            Characters = Array.AsReadOnly(copy);
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));

            var structureCopy = new StructureCombatFacts[structures?.Count ?? 0];
            long maximumId = 0;
            foreach (CharacterCombatFacts character in copy)
                maximumId = Math.Max(maximumId, character.EntityId.Value);

            for (int i = 0; i < structureCopy.Length; i++)
            {
                structureCopy[i] = structures[i] ?? throw new ArgumentException("구조물은 null일 수 없습니다.");
                maximumId = Math.Max(maximumId, structureCopy[i].EntityId.Value);
            }

            Structures = Array.AsReadOnly(structureCopy);
            NextEntityId = nextEntityId ?? maximumId + 1;
            CommandState = commandState ?? new CombatCommandState(combat.BoardWidth, combat.BoardHeight,
                combat.InitialPhase, EntityId.Empty, false, Structures,
                Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>(),
                new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: copy.Where(c => c.Owner == Owner.Enemy && c.Area == CharacterRuntimeArea.Field)
                    .Select(c => new CombatEnemyOrigin(c.EntityId, c.Anchor.Value, c.Rotation.Value)).ToArray(),
                    gimmicks: combat.StageGimmicks.Select((g, index) => g is RailwayGimmickSpec r
                        ? new CombatGimmickFacts(new StageGimmickId(index + 1), r.FirstActivationAfterUnitTurns)
                        : throw new InvalidOperationException("지원하지 않는 초기 기믹 정의입니다.")).ToArray()), combat.Environment,
                wallBlocking: combat.WallBlocking);
            var expected = new CombatCommandState(combat.BoardWidth, combat.BoardHeight, combat.InitialPhase,
                CommandState.ActiveActorId, CommandState.IsNight, Structures, CommandState.ThornFields, CommandState.Timeline,
                CommandState.Turn, combat.Environment, CommandState.EnvironmentProgress, combat.WallBlocking);
            if (!expected.SameValues(CommandState))
                throw new ArgumentException("전투 준비와 명령 조회값이 다릅니다.", nameof(commandState));
        }
    }
}
