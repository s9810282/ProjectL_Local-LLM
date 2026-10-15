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
        private const long LegacyNightLength = 1000;

        public int MapWidth { get; }
        public int MapHeight { get; }
        public int ReserveCapacity { get; }
        public int PlayerDeploymentDepth { get; }
        public int EnemyDeploymentDepth { get; }
        public int TimelineSeed { get; }

        public ReadOnlyMemory<InitialEnemyPlacementSpec> InitialEnemyPlacements { get; }
        public ReadOnlyMemory<CellCoord> RailCells { get; }
        public ReadOnlyMemory<StageGimmickSpec> StageGimmicks { get; }

        /// <summary>
        /// 밤이 얼마나 지속되는 지
        /// </summary>
        public long NightLength { get; }
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
                LegacyNightLength,
                Array.Empty<InitialEnemyStructurePlacementSpec>())
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
            StageGimmickSpec[] stageGimmicks,
            long nightLength,
            InitialEnemyStructurePlacementSpec[] initialEnemyStructurePlacements)
        {
            if (mapWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapWidth));
            if (mapHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapHeight));
            if (reserveCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
            if (playerDeploymentDepth <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerDeploymentDepth));
            if (enemyDeploymentDepth <= 0)
                throw new ArgumentOutOfRangeException(nameof(enemyDeploymentDepth));
            if (playerDeploymentDepth + enemyDeploymentDepth >= mapHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerDeploymentDepth),
                    "Combined deployment depths must be less than mapHeight.");
            }

            if (initialEnemyPlacements == null)
                throw new ArgumentNullException(nameof(initialEnemyPlacements));
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));
            if (stageGimmicks == null)
                throw new ArgumentNullException(nameof(stageGimmicks));
            if (nightLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(nightLength));
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
            PlayerDeploymentDepth = playerDeploymentDepth;
            EnemyDeploymentDepth = enemyDeploymentDepth;
            TimelineSeed = timelineSeed;
            InitialEnemyPlacements = copied;
            RailCells = copiedRailCells;
            StageGimmicks = copiedGimmicks;
            NightLength = nightLength;
            InitialEnemyStructurePlacements = copiedEnemyStructurePlacements;
        }
    }
}
