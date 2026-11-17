using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
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
        public int TimelineSeed { get; }
        public CombatFlowPhase InitialPhase { get; }
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        /// <summary>아군 배치 구역이다. 빈 구역도 허용한다(1칸 이상 검사는 StageConfig가 한다).</summary>
        public PlayerDeploymentZone PlayerZone { get; }
        /// <summary>전환 입구다. 줄 구역이면 그 줄 수, 칸 구역이면 0이다. Core 로직은 읽지 않는다.</summary>
        public int PlayerDeploymentDepth => PlayerZone.RowDepth ?? 0;
        /// <summary>전환 입구다. 적 배치 구역이 없어 늘 0이다. Core 로직은 읽지 않는다.</summary>
        public int EnemyDeploymentDepth => 0;
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> StageGimmicks { get; }
        public MovementEnvironmentFacts Environment { get; }
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public WallBlockingSpec WallBlocking { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        /// <summary>전투 동안 바뀌지 않는 유닛별 특성 설정값이다(설명 수치 공개 설계 SP3).</summary>
        public BattleTraitSettings TraitSettings { get; }
        public NexusReinforcementSpec NexusReinforcements { get; }

        /// <summary>
        /// 전환 입구다. 아군 줄 수로 줄 구역을 만든다. 적 줄 수는 받기만 하고 쓰지 않는다.
        /// 새 코드는 <see cref="PlayerDeploymentZone"/>을 받는 생성자를 쓴다.
        /// </summary>
        public CombatPreparation(int boardWidth, int boardHeight, int reserveCapacity,
            int timelineSeed, CombatFlowPhase initialPhase,
            IReadOnlyList<CharacterInstanceId> drawOrder, BattleTraitSettings traitSettings, int drawCursor = 0,
            int playerDeploymentDepth = 0, int enemyDeploymentDepth = 0,
            CellCoord[] railCells = null, StageGimmickSpec[] stageGimmicks = null,
            MovementEnvironmentFacts environment = null, EnvironmentSemanticBindings environmentBindings = null,
            IReadOnlyList<ObstacleContentProfile> obstacleContents = null,
            WallBlockingSpec wallBlocking = null, NexusReinforcementSpec nexusReinforcements = null)
            : this(boardWidth, boardHeight, reserveCapacity, timelineSeed, initialPhase, drawOrder, drawCursor,
                PlayerDeploymentZone.Rows(playerDeploymentDepth), traitSettings, railCells, stageGimmicks, environment, environmentBindings,
                obstacleContents, wallBlocking, nexusReinforcements)
        {
        }

        /// <remarks>
        /// 줄 수를 받는 옛 생성자는 drawCursor 뒤가 모두 선택 인자다. 이 생성자는 drawCursor와 구역을 필수로 받아
        /// 인자를 생략한 기존 호출이 두 생성자 사이에서 모호해지지 않게 한다.
        /// </remarks>
        public CombatPreparation(int boardWidth, int boardHeight, int reserveCapacity,
            int timelineSeed, CombatFlowPhase initialPhase,
            IReadOnlyList<CharacterInstanceId> drawOrder, int drawCursor, PlayerDeploymentZone playerZone, BattleTraitSettings traitSettings,
            CellCoord[] railCells = null, StageGimmickSpec[] stageGimmicks = null,
            MovementEnvironmentFacts environment = null, EnvironmentSemanticBindings environmentBindings = null,
            IReadOnlyList<ObstacleContentProfile> obstacleContents = null,
            WallBlockingSpec wallBlocking = null, NexusReinforcementSpec nexusReinforcements = null)
        {
            if (boardWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1)
                throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (reserveCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
            if (initialPhase != CombatFlowPhase.PlayerInput &&
                initialPhase != CombatFlowPhase.Automatic)
                throw new ArgumentOutOfRangeException(nameof(initialPhase));
            if (drawOrder == null)
                throw new ArgumentNullException(nameof(drawOrder));
            if (drawCursor < 0 || drawCursor > drawOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(drawCursor));
            if (playerZone == null)
                throw new ArgumentNullException(nameof(playerZone));
            if (playerZone.FitsBoard(boardWidth, boardHeight) == false)
                throw new ArgumentOutOfRangeException(nameof(playerZone));
            TraitSettings = traitSettings ?? throw new ArgumentNullException(nameof(traitSettings));
            NexusReinforcements = nexusReinforcements;

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
            Environment = environment ?? MovementEnvironmentFacts.Empty;
            EnvironmentBindings = environmentBindings ?? EnvironmentSemanticBindings.Empty;
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            var obstacleCopy = (obstacleContents ?? Array.Empty<ObstacleContentProfile>()).ToArray();
            if (obstacleCopy.Any(p => p == null) || obstacleCopy.Select(p => p.EntityId).Distinct().Count() != obstacleCopy.Length)
                throw new ArgumentException("장애물 프로필은 null이나 중복을 포함할 수 없습니다.");
            ObstacleContents = Array.AsReadOnly(obstacleCopy);
            BoardHeight = boardHeight;
            ReserveCapacity = reserveCapacity;
            TimelineSeed = timelineSeed;
            InitialPhase = initialPhase;
            DrawOrder = Array.AsReadOnly(copy);
            DrawCursor = drawCursor;
            PlayerZone = playerZone;
            RailCells = Array.AsReadOnly((CellCoord[])(railCells ?? Array.Empty<CellCoord>()).Clone());
            StageGimmicks = Array.AsReadOnly((StageGimmickSpec[])(stageGimmicks ?? Array.Empty<StageGimmickSpec>()).Clone());
        }
    }

    public sealed class BattlePreparation
    {
        public RunDeckSnapshot Membership { get; }
        public long RosterRevision { get; }
        public IReadOnlyList<CharacterCombatFacts> Characters { get; }
        public CombatPreparation Combat { get; }
        public IReadOnlyList<StructureCombatFacts> Structures { get; }
        /// <summary>초기 명령 상태다. 다음 EntityId의 원본은 CommandState.Allocation 한 곳이다.</summary>
        public CombatCommandState CommandState { get; }

        /// <param name="environmentProgress">
        /// 준비가 정한 초기 환경 진행값이다(최초 무작위 배치 뒤 난수 포함). 생략하면 환경에서 계산한 기본값이다.
        /// 명령 상태를 함께 넘기면 그 상태의 진행값과 같아야 한다.
        /// </param>
        public BattlePreparation(IReadOnlyList<CharacterCombatFacts> characters,
            CombatPreparation combat, IReadOnlyList<StructureCombatFacts> structures = null,
            long? nextEntityId = null, CombatCommandState commandState = null, BattleObjective objective = null,
            EnvironmentProgressFacts environmentProgress = null, RunDeckSnapshot membership = null, long rosterRevision = 0,
            PlayerSkillBattleState playerSkill = null)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (characters.Count == 0)
                throw new ArgumentException("전투 준비에는 캐릭터가 한 명 이상 필요합니다.", nameof(characters));

            var copy = new CharacterCombatFacts[characters.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = characters[i] ?? throw new ArgumentException(
                    "전투 준비 캐릭터에는 null을 넣을 수 없습니다.", nameof(characters));

            Membership = membership;
            RosterRevision = rosterRevision;
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
            // 초기 할당은 준비가 정한다(운영 경로는 commandState를 넘기지 않는다).
            // 직접 넘긴 상태는 그대로 저장한다. 할당이 없으면 그 전투는 개체를 만들 수 없고, 할당이 있으면 준비 값과 같아야 한다.
            var computed = new CombatAllocationFacts(nextEntityId ?? maximumId + 1, CombatAllocationFacts.InitialSummonRandomState);
            if (commandState != null && commandState.Allocation.IsAvailable && !commandState.Allocation.SameValues(computed))
                throw new ArgumentException("전투 준비의 초기 할당과 명령 상태의 할당이 다릅니다.", nameof(commandState));
            var allocation = commandState?.Allocation ?? computed;
            if (commandState != null && environmentProgress != null && !commandState.EnvironmentProgress.SameValues(environmentProgress))
                throw new ArgumentException("전투 준비의 초기 환경 진행값과 명령 상태의 진행값이 다릅니다.", nameof(commandState));
            var reinforcements = InitialReinforcements(combat, Structures);
            playerSkill ??= commandState?.PlayerSkill;
            if (playerSkill != null &&
                (playerSkill.LastUsedControllerTurnSequence != 0 || playerSkill.RemainingUses != playerSkill.Definition.StageUseLimit ||
                 (membership != null && membership.MainId != playerSkill.MainInstanceId) ||
                 !copy.Any(c => c.EntityId == playerSkill.ActorId && c.OwnedId == playerSkill.MainInstanceId &&
                     c.Owner == Owner.Player && c.OwnershipRole == CharacterOwnershipRole.Main &&
                     c.CharacterKey == playerSkill.Definition.MainCharacterKey)))
                throw new ArgumentException("초기 스킬 상태는 준비한 메인과 최대 사용량에 일치해야 합니다.", nameof(playerSkill));
            CommandState = commandState ?? new CombatCommandState(combat.BoardWidth, combat.BoardHeight, combat.InitialPhase, Structures,
                new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: copy.Where(c => c.Owner == Owner.Enemy && c.Area == CharacterRuntimeArea.Field)
                    .Select(c => new CombatEnemyOrigin(c.EntityId, c.Anchor.Value, c.Rotation.Value)).ToArray()),
                combat.Environment, environmentProgress: environmentProgress, wallBlocking: combat.WallBlocking,
                objective: objective, allocation: allocation, itemRandomState: unchecked((uint)combat.TimelineSeed),
                reinforcements: reinforcements, playerSkill: playerSkill);
            var expected = new CombatCommandState(combat.BoardWidth, combat.BoardHeight, combat.InitialPhase, Structures,
                CommandState.Turn, combat.Environment, CommandState.EnvironmentProgress, combat.WallBlocking,
                objective, allocation, CommandState.ItemRandomState, gimmicks: CommandState.Gimmicks,
                reinforcements: reinforcements, playerSkill: playerSkill, thornBombs: ThornBombStateSet.Empty);
            if (!expected.SameValues(CommandState))
                throw new ArgumentException("전투 준비와 명령 조회값이 다릅니다.", nameof(commandState));
        }

        private static NexusReinforcementState InitialReinforcements(CombatPreparation combat,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            if (combat.NexusReinforcements == null)
                return NexusReinforcementState.Disabled;

            var nexuses = structures.Where(s => s.Owner == Owner.Enemy && s.Definition.Key == EntityKey.Structure_Headquarters &&
                !s.Health.IsDead).ToArray();
            if (nexuses.Length != 1)
                throw new ArgumentException("넥서스 증원은 살아 있는 적 본부 하나에 연결해야 합니다.", nameof(structures));

            return new NexusReinforcementState(nexuses[0].EntityId, unchecked((uint)combat.TimelineSeed ^ 0x4E585350u),
                Array.Empty<NexusReinforcementRecord>());
        }
    }
}
