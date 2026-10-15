using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 원본과 구조물의 의미만 구분하며 위치나 활성 상태를 복제하지 않는다.</summary>
    public sealed class EnvironmentSemanticBindings
    {
        public static EnvironmentSemanticBindings Empty { get; } =
            new(Array.Empty<long>(), Array.Empty<long>(), Array.Empty<EntityId>());
        public IReadOnlyList<long> TerrainSourceIds { get; }
        public IReadOnlyList<long> ObjectSourceIds { get; }
        public IReadOnlyList<EntityId> ObstacleEntityIds { get; }

        public EnvironmentSemanticBindings(IReadOnlyList<long> terrainSourceIds,
            IReadOnlyList<long> objectSourceIds, IReadOnlyList<EntityId> obstacleEntityIds)
        {
            var terrain = CopySourceIds(terrainSourceIds, nameof(terrainSourceIds));
            var objects = CopySourceIds(objectSourceIds, nameof(objectSourceIds));
            var obstacles = CopyEntityIds(obstacleEntityIds, nameof(obstacleEntityIds));
            if (terrain.Intersect(objects).Any())
                throw new ArgumentException("같은 원본을 지형과 오브젝트로 동시에 분류할 수 없습니다.");

            TerrainSourceIds = Array.AsReadOnly(terrain);
            ObjectSourceIds = Array.AsReadOnly(objects);
            ObstacleEntityIds = Array.AsReadOnly(obstacles);
        }

        /// <summary>분류별 ID 집합을 비교한다. 배열 나열 순서는 게임플레이 순서가 아니다.</summary>
        public bool SameValues(EnvironmentSemanticBindings other) => other != null &&
            new HashSet<long>(TerrainSourceIds).SetEquals(other.TerrainSourceIds) &&
            new HashSet<long>(ObjectSourceIds).SetEquals(other.ObjectSourceIds) &&
            new HashSet<EntityId>(ObstacleEntityIds).SetEquals(other.ObstacleEntityIds);

        public void ValidateInitialReferences(MovementEnvironmentFacts environment,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            if (structures.Any(s => s == null))
                throw new ArgumentException("구조물 사실은 null일 수 없습니다.", nameof(structures));

            var sourceIds = new HashSet<long>(environment.Tiles.Select(t => t.Id));
            var structureIds = new HashSet<EntityId>(structures.Select(s => s.EntityId));
            if (TerrainSourceIds.Any(id => !sourceIds.Contains(id)) ||
                ObjectSourceIds.Any(id => !sourceIds.Contains(id)))
                throw new ArgumentException("분류한 원본이 초기 환경에 없습니다.", nameof(environment));
            if (ObstacleEntityIds.Any(id => !structureIds.Contains(id)))
                throw new ArgumentException("분류한 장애물이 초기 구조물에 없습니다.", nameof(structures));
        }

        internal static long[] CopySourceIds(IReadOnlyList<long> ids, string parameterName)
        {
            if (ids == null) throw new ArgumentNullException(parameterName);
            var copy = ids.ToArray();
            if (copy.Any(id => id < 1) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("원본 ID는 양수이며 중복될 수 없습니다.", parameterName);
            return copy;
        }

        internal static EntityId[] CopyEntityIds(IReadOnlyList<EntityId> ids, string parameterName)
        {
            if (ids == null) throw new ArgumentNullException(parameterName);
            var copy = ids.ToArray();
            if (copy.Any(id => id.IsEmpty) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("개체 ID는 비어 있거나 중복될 수 없습니다.", parameterName);
            return copy;
        }
    }
}
