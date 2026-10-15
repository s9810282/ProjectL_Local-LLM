using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 사실과 의미 분류를 조합하며 셀별 런타임 상태를 보관하지 않는다.</summary>
    public static class CellEnvironmentQuery
    {
        /// <summary>점유 여부와 의미 분류에 관계없이 초기 원본의 범위와 지원 가능한 구성을 검사한다.</summary>
        public static void ValidateInitialEnvironment(int boardWidth, int boardHeight,
            MovementEnvironmentFacts environment)
        {
            if (boardWidth < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            foreach (var source in environment.Tiles)
                if (source.Cell.X < 0 || source.Cell.Y < 0 ||
                    source.Cell.X >= boardWidth || source.Cell.Y >= boardHeight)
                    throw new ArgumentOutOfRangeException(nameof(environment), "초기 환경 원본이 보드 범위를 벗어났습니다.");

            foreach (var cell in environment.Tiles.OfType<DestinationTileModifierFact>()
                .Where(source => source.Active).Select(source => source.Cell).Distinct())
                PositionTileModifierCalculator.ResolveAt(cell, environment);
        }

        public static EnvironmentPlacementViolation CheckPlacement(CellEnvironmentSnapshot cell) =>
            EnvironmentPlacementRules.Evaluate(cell);

        /// <summary>초기 벽 형상과 배치 공존 규칙을 한 곳에서 검사한다. Buffer는 이 검증을 호출만 한다.</summary>
        public static void ValidateInitialPlacement(int boardWidth, int boardHeight,
            MovementEnvironmentFacts environment, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyList<CharacterCombatFacts> characters, EnvironmentSemanticBindings bindings,
            WallBlockingSpec wallBlocking, IReadOnlyList<CellCoord> railCells)
        {
            if (railCells == null) throw new ArgumentNullException(nameof(railCells));
            var walls = WallGeometryBuilder.Build(boardWidth, boardHeight, environment, bindings, wallBlocking);
            if (railCells.Any(walls.Occupies))
                throw new InvalidOperationException("철도는 벽 셀을 통과할 수 없습니다.");
            foreach (var source in environment.Tiles)
                if (source.Active && walls.Occupies(source.Cell) &&
                    !(source is StaticTerrainFact terrain && terrain.Kind == StaticTerrainKind.Wall))
                    throw new InvalidOperationException($"벽 셀에 다른 환경 원본이 겹칩니다: {source.Id}@({source.Cell.X},{source.Cell.Y})");

            var cells = environment.Tiles.Select(source => source.Cell)
                .Concat(structures.SelectMany(structure => structure.OccupiedCells)).Distinct();
            foreach (var cell in cells)
            {
                var snapshot = ReadSpatial(boardWidth, boardHeight, environment, structures, characters, bindings, cell);
                if (CheckPlacement(snapshot) != EnvironmentPlacementViolation.None)
                    throw new InvalidOperationException("초기 환경 배치가 셀 공존 규칙을 위반합니다.");
            }
        }

        public static CellEnvironmentSnapshot Read(CombatCommandState state,
            IReadOnlyList<CharacterCombatFacts> characters,
            EnvironmentSemanticBindings bindings, CellCoord cell)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return ReadSpatial(state.BoardWidth, state.BoardHeight, state.Environment, state.Structures,
                characters, bindings, cell);
        }

        internal static CellEnvironmentSnapshot ReadSpatial(int boardWidth, int boardHeight,
            MovementEnvironmentFacts environment, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyList<CharacterCombatFacts> characters,
            EnvironmentSemanticBindings bindings, CellCoord cell)
        {
            if (boardWidth < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var entityIds = new HashSet<EntityId>();
            foreach (var structure in structures)
                if (structure == null || !entityIds.Add(structure.EntityId))
                    throw new ArgumentException("구조물 사실은 null이거나 중복된 개체일 수 없습니다.", nameof(structures));
            foreach (var character in characters)
                if (character == null || !entityIds.Add(character.EntityId))
                    throw new ArgumentException("캐릭터 사실은 null이거나 중복된 개체일 수 없습니다.", nameof(characters));

            bool isInside = cell.X >= 0 && cell.Y >= 0 && cell.X < boardWidth && cell.Y < boardHeight;
            if (!isInside)
                return new CellEnvironmentSnapshot(cell, false, Array.Empty<long>(), Array.Empty<long>(),
                    Array.Empty<EntityId>(), Array.Empty<EntityId>(), Array.Empty<EntityId>());

            var terrain = new List<long>();
            var objects = new List<long>();
            foreach (var source in environment.Tiles)
            {
                if (!source.Active || source.Cell != cell) continue;
                if (bindings.TerrainSourceIds.Contains(source.Id)) terrain.Add(source.Id);
                else if (bindings.ObjectSourceIds.Contains(source.Id)) objects.Add(source.Id);
            }

            var blockers = new List<EntityId>();
            var obstacles = new List<EntityId>();
            foreach (var structure in structures)
            {
                if (structure.Health.IsDead || !structure.OccupiedCells.Contains(cell)) continue;
                blockers.Add(structure.EntityId);
                if (bindings.ObstacleEntityIds.Contains(structure.EntityId)) obstacles.Add(structure.EntityId);
            }

            var occupants = characters.Where(c => c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(cell)).Select(c => c.EntityId).ToArray();
            // 초기 참조 검증을 반복하지 않아 소비 후 사라진 원본이 다시 나타나지 않는다.
            return new CellEnvironmentSnapshot(cell, true, terrain, objects, obstacles, blockers, occupants,
                EnvironmentTerrainRules.IsWallOccupied(environment, bindings, cell));
        }
    }
}
