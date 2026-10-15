using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>초기 환경의 지원 범위를 검사한 뒤 배치된 캐릭터에 위치 기여분을 적용한다.</summary>
    internal static class BattleEnvironmentPreparation
    {
        internal static IReadOnlyList<CharacterCombatFacts> Prepare(BattleInitialization initialization,
            IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures,
            MainCharacterPersistentState main, EnvironmentSemanticBindings preparedBindings = null,
            IReadOnlyList<ObstacleContentProfile> obstacleContents = null)
        {
            if (initialization == null) throw new ArgumentNullException(nameof(initialization));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (main == null) throw new ArgumentNullException(nameof(main));
            var stage = initialization.Stage;
            var environment = initialization.Environment;
            var bindings = preparedBindings ?? initialization.EnvironmentBindings;
            obstacleContents ??= Array.Empty<ObstacleContentProfile>();
            CellEnvironmentQuery.ValidateInitialEnvironment(stage.MapWidth, stage.MapHeight, environment);
            bindings.ValidateInitialReferences(environment, structures);

            CellEnvironmentQuery.ValidateInitialPlacement(stage.MapWidth, stage.MapHeight, environment,
                structures, characters, bindings, initialization.WallBlocking, stage.RailCells.ToArray());

            if (environment.Tiles.Any(source => bindings.TerrainSourceIds.Contains(source.Id) &&
                !EnvironmentContentSupport.IsSupportedInitialTerrain(source)))
                throw new NotSupportedException("초기 Terrain은 지원되는 위치 보정 또는 정적 지형 원본이어야 합니다.");
            if (environment.Tiles.Where(EnvironmentContentSupport.IsExclusiveTerrain)
                .Any(source => !bindings.TerrainSourceIds.Contains(source.Id)))
                throw new NotSupportedException("정적 지형 원본에는 Terrain 분류가 필요합니다.");
            if (environment.Tiles.Where(t => t.Active && bindings.TerrainSourceIds.Contains(t.Id)).GroupBy(t => t.Cell)
                .Any(group => group.Count() > 1 && group.Any(EnvironmentContentSupport.IsExclusiveTerrain)))
                throw new NotSupportedException("정적 지형과 다른 Terrain의 같은 셀 조합은 아직 지원하지 않습니다.");
            if (environment.Tiles.Any(source => bindings.ObjectSourceIds.Contains(source.Id) &&
                    !EnvironmentContentSupport.IsSupportedObject(source)) ||
                environment.Tiles.OfType<ObjectSourceFact>().Any(source => !bindings.ObjectSourceIds.Contains(source.Id)))
                throw new NotSupportedException("초기 Object는 구현된 원본 타입과 Object 분류를 함께 사용해야 합니다.");
            foreach (var cell in environment.Tiles.OfType<ObjectSourceFact>().Select(o => o.Cell).Distinct())
                _ = ObjectArrivalCalculator.OrderedAt(environment, bindings, cell);
            if (bindings.ObstacleEntityIds.Count > 0 && obstacleContents.Count == 0)
                throw new NotSupportedException("실행 프로필이 없는 Obstacle은 아직 지원하지 않습니다.");
            ObstacleContentProfile.Validate(obstacleContents, bindings, structures);

            var changes = EnvironmentPresenceReconciler.Calculate(characters, environment,
                characters.SelectMany(character => character.OccupiedCells).Distinct().ToArray(), main);
            // 현재 준비 계약은 지속 메인 HP를 변경할 수 없으므로 계산 결과를 게시하기 전에 거부한다.
            if (changes.Main != null)
                throw new NotSupportedException("현재 전투 준비 계약은 환경에 의한 메인 현재 HP 변경을 지원하지 않습니다.");
            if (changes.Characters.Count == 0) return characters;
            var finals = changes.Characters.ToDictionary(change => change.Expected.EntityId, change => change.Final);
            return Array.AsReadOnly(characters.Select(character =>
                finals.TryGetValue(character.EntityId, out var final) ? final : character).ToArray());
        }
    }
}
