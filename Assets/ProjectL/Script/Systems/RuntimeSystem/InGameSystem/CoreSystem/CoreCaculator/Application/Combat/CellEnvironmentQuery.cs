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

            if (environment.Tiles.Where(source => source.Active && source is not MovingPlatformFact &&
                    EnvironmentContentSupport.IsSupportedInitialTerrain(source))
                .GroupBy(source => source.Cell).Any(group => group.Count() > 1 && group.OfType<StaticTerrainFact>()
                    .Any(terrain => terrain.Kind is StaticTerrainKind.WeakGround or StaticTerrainKind.Cliff or StaticTerrainKind.FilledCliff)))
                throw new InvalidOperationException("약한 지반과 낭떠러지는 다른 지형과 같은 셀에 중복 작성할 수 없습니다.");

            foreach (var cell in environment.Tiles.OfType<DestinationTileModifierFact>()
                .Where(source => source.Active).Select(source => source.Cell).Distinct())
                PositionTileModifierCalculator.ResolveAt(cell, environment);
        }

        public static EnvironmentPlacementViolation CheckPlacement(CellEnvironmentSnapshot cell) =>
            EnvironmentPlacementRules.Evaluate(cell);

        /// <summary>게시할 최종 점유와 플랫폼의 중간 이동이 기믹 차단 칸을 침범하지 않는지 검사한다.</summary>
        public static void ValidateGimmickOccupancy(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> characters)
        {
            var final = transition.FinalState.Gimmicks;
            if (GimmickOccupancyRules.BlockingCells(final).Count == 0) return;
            foreach (var character in characters)
                if (!character.Health.IsDead && character.Area == CharacterRuntimeArea.Field &&
                    GimmickOccupancyRules.Overlaps(final, character.OccupiedCells, null))
                    throw new InvalidOperationException("캐릭터가 기믹 차단 칸을 점유할 수 없습니다.");
            foreach (var structure in transition.FinalState.Structures)
                if (!structure.Health.IsDead && GimmickOccupancyRules.Overlaps(final, structure.OccupiedCells, null))
                    throw new InvalidOperationException("구조물이 기믹 차단 칸을 점유할 수 없습니다.");
            // 플랫폼은 진영 시작 직전에만 이동하므로 이 묶음 안에서 사슬 머리는 바뀌지 않는다.
            foreach (var step in transition.Events.OfType<CombatPlatformStepEvent>())
                if (step.StepKind == PlatformStepKind.Translation &&
                    GimmickOccupancyRules.Overlaps(transition.Input.State.Gimmicks, step.Final.OccupiedCells, null))
                    throw new InvalidOperationException("플랫폼이 기믹 차단 칸으로 이동할 수 없습니다.");
        }

        /// <summary>초기 벽 형상과 배치 공존 규칙을 한 곳에서 검사한다. Buffer는 이 검증을 호출만 한다.</summary>
        public static void ValidateInitialPlacement(int boardWidth, int boardHeight,
            MovementEnvironmentFacts environment, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyList<CharacterCombatFacts> characters, EnvironmentSemanticBindings bindings,
            WallBlockingSpec wallBlocking, IReadOnlyList<CellCoord> railCells, StageGimmickStateSet gimmicks = null)
        {
            if (railCells == null) throw new ArgumentNullException(nameof(railCells));
            if (gimmicks != null)
            {
                foreach (var cell in GimmickOccupancyRules.BlockingCells(gimmicks))
                    if (cell.X < 0 || cell.Y < 0 || cell.X >= boardWidth || cell.Y >= boardHeight ||
                        EnvironmentTerrainRules.BlocksVoluntaryEntry(environment, bindings, cell) ||
                        structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell)) ||
                        characters.Any(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.OccupiedCells.Contains(cell)))
                        throw new InvalidOperationException("기믹 차단 칸과 초기 지형·개체 배치가 겹칩니다.");
            }
            var platformCells = ValidateInitialPlatforms(boardWidth, boardHeight, environment, bindings);
            var walls = WallGeometryBuilder.Build(boardWidth, boardHeight, environment, bindings, wallBlocking);
            if (railCells.Any(walls.Occupies))
                throw new InvalidOperationException("철도는 벽 셀을 통과할 수 없습니다.");
            foreach (var source in environment.Tiles)
                if (source.Active && walls.Occupies(source.Cell) &&
                    !(source is StaticTerrainFact terrain && terrain.Kind == StaticTerrainKind.Wall))
                    throw new InvalidOperationException($"벽 셀에 다른 환경 원본이 겹칩니다: {source.Id}@({source.Cell.X},{source.Cell.Y})");

            var cells = environment.Tiles.Select(source => source.Cell)
                .Concat(structures.SelectMany(structure => structure.OccupiedCells)).Distinct();
            var cliffCells = BoardSurfaceRules.BlockedSurfaceCellSteps(environment, bindings, includeWalls: false)
                .Where(cell => cell.HasValue).Select(cell => cell.Value).ToHashSet();
            var bossCells = platformCells.Count == 0 ? null : characters.Where(character =>
                character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead &&
                character.Stats.Category.Key == CategoryKey.Boss).SelectMany(character => character.OccupiedCells).ToHashSet();
            foreach (var cell in cells)
            {
                var snapshot = ReadSpatial(boardWidth, boardHeight, environment, structures, characters, bindings, cell);
                if (cliffCells.Contains(cell) && (snapshot.CharacterIds.Count > 0 || snapshot.BlockingStructureIds.Count > 0))
                    throw new InvalidOperationException("낭떠러지 위에 살아 있는 캐릭터나 구조물을 초기 배치할 수 없습니다.");
                if (platformCells.Contains(cell) && (snapshot.BlockingStructureIds.Count > 0 || bossCells.Contains(cell)))
                    throw new InvalidOperationException("플랫폼 위에 보스나 구조물을 초기 배치할 수 없습니다.");
                if (CheckPlacement(snapshot) != EnvironmentPlacementViolation.None)
                    throw new InvalidOperationException("초기 환경 배치가 셀 공존 규칙을 위반합니다.");
            }
        }

        private static HashSet<CellCoord> ValidateInitialPlatforms(int width, int height,
            MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings)
        {
            var occupied = new HashSet<CellCoord>();
            foreach (var platform in environment.Tiles.OfType<MovingPlatformFact>())
            {
                var spec = platform.Spec;
                if (!bindings.TerrainSourceIds.Contains(platform.Id) || platform.KnownTo.Count != 2 ||
                    platform.Cell != spec.InitialAnchor || platform.TravelSign != spec.InitialSign)
                    throw new InvalidOperationException($"플랫폼 {platform.Id}의 초기 위치·진행 부호·지형 분류·공개 범위가 맞지 않습니다.");
                // 직선 경로의 양 끝에서 점유 모양이 들어가면 그 사이 기준 칸의 모양도 보드 안에 들어간다.
                if (!spec.FootprintAt(spec.RouteStart).All(IsInside) || !spec.FootprintAt(spec.RouteEnd).All(IsInside))
                    throw new InvalidOperationException($"플랫폼 {platform.Id}의 경로 또는 점유 모양이 보드 밖에 있습니다.");
                foreach (var cell in platform.OccupiedCells)
                {
                    if (!BoardSurfaceRules.IsPlatformRouteSurface(environment, bindings, cell) ||
                        environment.Tiles.Any(source => source.Active && source.Cell == cell &&
                            source is not MovingPlatformFact && bindings.TerrainSourceIds.Contains(source.Id) &&
                            !(source is StaticTerrainFact terrain && terrain.Kind == StaticTerrainKind.Cliff)))
                        throw new InvalidOperationException($"플랫폼 {platform.Id}의 초기 점유 셀에는 낭떠러지 바닥 한 종류만 허용합니다.");
                    if (!occupied.Add(cell))
                        throw new InvalidOperationException($"플랫폼 {platform.Id}의 초기 점유 셀이 다른 플랫폼과 겹칩니다.");
                }
            }
            return occupied;

            bool IsInside(CellCoord cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
        }

        public static CellEnvironmentSnapshot Read(CombatCommandState state,
            IReadOnlyList<CharacterCombatFacts> characters,
            EnvironmentSemanticBindings bindings, CellCoord cell)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return ReadSpatial(state.BoardWidth, state.BoardHeight, state.Environment, state.Structures,
                characters, bindings, cell, state.Gimmicks);
        }

        internal static CellEnvironmentSnapshot ReadSpatial(int boardWidth, int boardHeight,
            MovementEnvironmentFacts environment, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyList<CharacterCombatFacts> characters,
            EnvironmentSemanticBindings bindings, CellCoord cell, StageGimmickStateSet gimmicks = null)
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
                if (!source.Active || (source is MovingPlatformFact platform ? !platform.Occupies(cell) : source.Cell != cell)) continue;
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
                EnvironmentTerrainRules.IsWallOccupied(environment, bindings, cell),
                gimmicks != null && GimmickOccupancyRules.Blocks(gimmicks, cell));
        }
    }
}
