using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>확정된 공존 규칙의 위반이며 None은 이동이나 미정 규칙의 허용을 보증하지 않는다.</summary>
    [Flags]
    public enum EnvironmentPlacementViolation
    {
        None = 0,
        OutsideBoard = 1,
        TerrainWithObstacle = 2,
        ObstacleWithObject = 4,
        OverlappingObstacles = 8,
        WallOverlap = 16
    }

    /// <summary>한 셀의 현재 참여자를 읽은 불변 결과이며 공존 규칙의 판단은 별도로 수행한다.</summary>
    public sealed class CellEnvironmentSnapshot
    {
        public CellCoord Cell { get; }
        public bool IsInside { get; }
        public IReadOnlyList<long> TerrainSourceIds { get; }
        public IReadOnlyList<long> ObjectSourceIds { get; }
        public IReadOnlyList<EntityId> ObstacleIds { get; }
        public IReadOnlyList<EntityId> BlockingStructureIds { get; }
        public IReadOnlyList<EntityId> CharacterIds { get; }
        public bool IsWallOccupied { get; }

        public CellEnvironmentSnapshot(CellCoord cell, bool isInside,
            IReadOnlyList<long> terrainSourceIds, IReadOnlyList<long> objectSourceIds,
            IReadOnlyList<EntityId> obstacleIds, IReadOnlyList<EntityId> blockingStructureIds,
            IReadOnlyList<EntityId> characterIds, bool isWallOccupied = false)
        {
            var terrain = EnvironmentSemanticBindings.CopySourceIds(terrainSourceIds, nameof(terrainSourceIds));
            var objects = EnvironmentSemanticBindings.CopySourceIds(objectSourceIds, nameof(objectSourceIds));
            var obstacles = EnvironmentSemanticBindings.CopyEntityIds(obstacleIds, nameof(obstacleIds));
            var blockers = EnvironmentSemanticBindings.CopyEntityIds(blockingStructureIds, nameof(blockingStructureIds));
            var characters = EnvironmentSemanticBindings.CopyEntityIds(characterIds, nameof(characterIds));
            if (obstacles.Except(blockers).Any())
                throw new ArgumentException("장애물은 차단 구조물에 포함되어야 합니다.", nameof(obstacleIds));
            if (isWallOccupied && (!isInside || terrain.Length == 0))
                throw new ArgumentException("벽 점유는 보드 안 지형 원본이 있는 셀에만 존재합니다.", nameof(isWallOccupied));
            if (!isInside && (terrain.Length != 0 || objects.Length != 0 || obstacles.Length != 0 ||
                blockers.Length != 0 || characters.Length != 0))
                throw new ArgumentException("보드 밖의 셀에는 참여자가 있을 수 없습니다.");

            Cell = cell;
            IsInside = isInside;
            IsWallOccupied = isWallOccupied;
            TerrainSourceIds = Array.AsReadOnly(terrain);
            ObjectSourceIds = Array.AsReadOnly(objects);
            ObstacleIds = Array.AsReadOnly(obstacles);
            BlockingStructureIds = Array.AsReadOnly(blockers);
            CharacterIds = Array.AsReadOnly(characters);
        }
    }
}
