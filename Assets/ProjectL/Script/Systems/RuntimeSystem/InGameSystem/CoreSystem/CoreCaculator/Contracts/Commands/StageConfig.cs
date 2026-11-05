using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// View가 Stage를 만들 때 전달하는 맵 크기와 Reserve 슬롯 설정이다.
    /// </summary>
    public sealed class StageConfig
    {

        public int MapWidth { get; }
        public int MapHeight { get; }
        public int ReserveCapacity { get; }
        /// <summary>아군 배치 구역이다. 적은 이 구역 밖이면 어디든 놓인다. Core 판정은 이 값만 읽는다.</summary>
        public PlayerDeploymentZone PlayerZone { get; }

        /// <summary>전환 입구다. 줄 구역이면 그 줄 수, 칸 구역이면 0이다. Core 로직은 읽지 않으며 바깥이 옮기면 지운다.</summary>
        public int PlayerDeploymentDepth => PlayerZone.RowDepth ?? 0;

        /// <summary>전환 입구다. 적 배치 구역이 없어 늘 0이다. Core 로직은 읽지 않으며 바깥이 옮기면 지운다.</summary>
        public int EnemyDeploymentDepth => 0;
        public int TimelineSeed { get; }

        public ReadOnlyMemory<InitialEnemyPlacementSpec> InitialEnemyPlacements { get; }
        public ReadOnlyMemory<CellCoord> RailCells { get; }
        public ReadOnlyMemory<StageGimmickSpec> StageGimmicks { get; }

        /// <summary>
        /// </summary>
        /// <summary>
        /// Stage 시작 시 자동으로 생성될 적의 건물 배치 초기 위치
        /// </summary>
        public ReadOnlyMemory<InitialEnemyStructurePlacementSpec> InitialEnemyStructurePlacements { get; }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth,
                timelineSeed: 0,
                Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>())
        {
        }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth,
            int timelineSeed)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth,
                timelineSeed,
                initialEnemyPlacements: Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>())
        {
        }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth,
            int timelineSeed,
            CellCoord[] railCells,
            StageGimmickSpec[] stageGimmicks)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth,
                timelineSeed,
                Array.Empty<InitialEnemyPlacementSpec>(),
                railCells,
                stageGimmicks)
        {
        }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth,
            int timelineSeed,
            InitialEnemyPlacementSpec[] initialEnemyPlacements)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth,
                timelineSeed,
                initialEnemyPlacements,
                Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>())
        {
        }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth,
            int timelineSeed,
            InitialEnemyPlacementSpec[] initialEnemyPlacements,
            CellCoord[] railCells,
            StageGimmickSpec[] stageGimmicks)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth,
                timelineSeed,
                initialEnemyPlacements,
                railCells,
                stageGimmicks,
                Array.Empty<InitialEnemyStructurePlacementSpec>())
        {
        }

        /// <summary>
        /// 전환 입구다. 아군 줄 수로 줄 구역을 만든다. 적 줄 수는 받기만 하고 쓰지 않는다(적 배치 구역 없음).
        /// 새 코드는 <see cref="PlayerDeploymentZone"/>을 받는 생성자를 쓴다.
        /// </summary>
        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth,
            int timelineSeed,
            InitialEnemyPlacementSpec[] initialEnemyPlacements,
            CellCoord[] railCells,
            StageGimmickSpec[] stageGimmicks,
            InitialEnemyStructurePlacementSpec[] initialEnemyStructurePlacements)
            : this(
                mapWidth,
                mapHeight,
                reserveCapacity,
                PlayerDeploymentZone.Rows(playerDeploymentDepth),
                timelineSeed,
                initialEnemyPlacements,
                railCells,
                stageGimmicks,
                initialEnemyStructurePlacements)
        {
        }

        public StageConfig(
            int mapWidth,
            int mapHeight,
            int reserveCapacity,
            PlayerDeploymentZone playerZone,
            int timelineSeed,
            InitialEnemyPlacementSpec[] initialEnemyPlacements,
            CellCoord[] railCells,
            StageGimmickSpec[] stageGimmicks,
            InitialEnemyStructurePlacementSpec[] initialEnemyStructurePlacements)
        {
            if (mapWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapWidth));
            if (mapHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapHeight));
            if (reserveCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
            if (playerZone == null)
                throw new ArgumentNullException(nameof(playerZone));
            if (playerZone.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(playerZone), "아군 배치 구역은 1칸 이상이어야 합니다.");
            if (playerZone.FitsBoard(mapWidth, mapHeight) == false)
                throw new ArgumentOutOfRangeException(nameof(playerZone), "아군 배치 구역이 맵 범위를 벗어났습니다.");

            if (initialEnemyPlacements == null)
                throw new ArgumentNullException(nameof(initialEnemyPlacements));
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));
            if (stageGimmicks == null)
                throw new ArgumentNullException(nameof(stageGimmicks));
            if (initialEnemyStructurePlacements == null)
                throw new ArgumentNullException(nameof(initialEnemyStructurePlacements));

            InitialEnemyPlacementSpec[] copied = new InitialEnemyPlacementSpec[initialEnemyPlacements.Length];
            for (int i = 0; i < initialEnemyPlacements.Length; i++)
            {
                InitialEnemyPlacementSpec spec = initialEnemyPlacements[i];
                if (spec == null)
                    throw new ArgumentException($"{i}번째 spec가 비어있습니다.", nameof(initialEnemyPlacements));

                if (spec.Anchor.X < 0 || spec.Anchor.Y < 0 || spec.Anchor.X >= mapWidth || spec.Anchor.Y >= mapHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(initialEnemyPlacements),
                        $"{i}번째의 {spec.Anchor}값이 맵 범위를 벗어났습니다.");
                }

                copied[i] = spec;
            }

            HashSet<CellCoord> uniqueRailCells = new HashSet<CellCoord>();
            CellCoord[] copiedRailCells = new CellCoord[railCells.Length];
            for (int i = 0; i < railCells.Length; i++)
            {
                CellCoord cell = railCells[i];

                if (cell.X < 0 || cell.Y < 0 || cell.X >= mapWidth || cell.Y >= mapHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(railCells),
                        $"{i}번째의 {cell}값이 맵 범위를 벗어났습니다.");
                }

                if (uniqueRailCells.Add(cell) == false)
                {
                    throw new ArgumentException(
                        $"{i}번째 {cell}값은 중복되었습니다.",
                        nameof(railCells));
                }

                copiedRailCells[i] = cell;
            }

            StageGimmickSpec[] copiedGimmicks = new StageGimmickSpec[stageGimmicks.Length];
            for (int i = 0; i < stageGimmicks.Length; i++)
            {
                StageGimmickSpec spec = stageGimmicks[i];
                if (spec == null)
                    throw new ArgumentException($"{i}번째 gimmick이 비어있습니다.", nameof(stageGimmicks));

                copiedGimmicks[i] = spec;
            }

            InitialEnemyStructurePlacementSpec[] copiedEnemyStructurePlacements =
                new InitialEnemyStructurePlacementSpec[initialEnemyStructurePlacements.Length];
            for (int i = 0; i < initialEnemyStructurePlacements.Length; i++)
            {
                InitialEnemyStructurePlacementSpec spec = initialEnemyStructurePlacements[i];
                if (spec == null)
                {
                    throw new ArgumentException(
                        $"{i}번째 적 건물 배치 정보가 비어있습니다.",
                        nameof(initialEnemyStructurePlacements));
                }

                if (spec.Anchor.X < 0 || spec.Anchor.Y < 0 || spec.Anchor.X >= mapWidth || spec.Anchor.Y >= mapHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(initialEnemyStructurePlacements),
                        $"{i}번째 적 건물의 {spec.Anchor}값이 맵 범위를 벗어났습니다.");
                }

                copiedEnemyStructurePlacements[i] = spec;
            }

            MapWidth = mapWidth;
            MapHeight = mapHeight;
            ReserveCapacity = reserveCapacity;
            PlayerZone = playerZone;
            TimelineSeed = timelineSeed;
            InitialEnemyPlacements = copied;
            RailCells = copiedRailCells;
            StageGimmicks = copiedGimmicks;
            InitialEnemyStructurePlacements = copiedEnemyStructurePlacements;
        }
    }
}
