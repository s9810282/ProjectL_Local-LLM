using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 추적 경로 찾기가 성능 작업 전 사본(<see cref="EnemyPursuitReference"/>)과 같은 결과를 내는지 본다.
    /// 경로 찾기 전체(참·거짓, 경로 칸)는 출발 칸을 보드 여러 곳으로 바꿔 비교하고(성능 계획 TP-2),
    /// "그 칸에서 칠 수 있나" 판정은 보드의 빈 칸마다 비교한다(TP-3). 기대값은 사본이 정하므로 규칙이 바뀌면 사본도 다시 정해야 한다.
    /// </summary>
    public sealed class EnemyPursuitEquivalenceTests
    {
        private const int Size = 7;
        private static readonly EntityId UnitTarget = new(2);
        private static readonly EntityId StructureTarget = new(30);
        private static readonly CellCoord DefaultTargetCell = new(5, 4);
        private static readonly CellCoord MainCell = new(0, 6);

        [TestCaseSource(nameof(Scenes))]
        public void TryFind_MatchesReference_FromEveryStart(Scene scene)
        {
            var mismatches = new List<string>();
            int compared = 0;
            foreach (var start in Starts(scene))
            {
                var input = scene.Build(start);
                bool expected = EnemyPursuitReference.TryFind(input, scene.TargetId, out var expectedPath);
                bool actual = EnemyMovementPathfinder.TryFind(input, scene.TargetId, out var actualPath);
                compared++;
                if (expected != actual || expected && !expectedPath.Cells.SequenceEqual(actualPath.Cells))
                    mismatches.Add($"출발 {start}: 기준 {Describe(expected, expectedPath)} / 실제 {Describe(actual, actualPath)}");
            }
            Assert.That(compared, Is.GreaterThan(0), "비교한 출발 칸이 없습니다.");
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        [TestCaseSource(nameof(Scenes))]
        public void CanAttackFrom_MatchesReference_OnEveryFreeCell(Scene scene)
        {
            var input = scene.Build(Starts(scene).First());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var occupied = scene.Occupied;
            var mismatches = new List<string>();
            int compared = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (occupied.Contains(cell)) continue;
                    bool expected = EnemyPursuitReference.CanAttackFrom(input, actor, cell, scene.TargetId);
                    bool actual = EnemyMovementPathfinder.CanAttackFrom(input, actor, cell, scene.TargetId);
                    compared++;
                    if (expected != actual) mismatches.Add($"칸 {cell}: 기준 {expected} / 실제 {actual}");
                }
            Assert.That(compared, Is.GreaterThan(0), "비교한 칸이 없습니다.");
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        private static IEnumerable<CellCoord> Starts(Scene scene)
        {
            var occupied = scene.Occupied;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var cell = new CellCoord(x, y);
                    // 출발 칸을 보드에 고르게 흩어 놓는다(모든 칸은 기준 구현이 느려 시험 시간이 길다).
                    if ((x + y) % 3 == 0 && !occupied.Contains(cell)) yield return cell;
                }
        }

        private static string Describe(bool found, MovementPath path) =>
            found ? string.Join("→", path.Cells.Select(c => c.ToString())) : "없음";

        private static IEnumerable<TestCaseData> Scenes()
        {
            var shapes = new[] { TargetingShape.RadialSquare, TargetingShape.CardinalLine, TargetingShape.DiagonalLine,
                TargetingShape.EightDirectionLine, TargetingShape.Fan };
            var splashes = new[] { TargetingSplash.None, TargetingSplash.Cross, TargetingSplash.Diagonal, TargetingSplash.Radial };
            foreach (var shape in shapes)
            {
                foreach (var splash in splashes)
                    yield return Case($"{shape} 칸 {splash}", new Scene(Method(shape, TargetingChoiceMode.Single, splash), range: 2));
                yield return Case($"{shape} 방향", new Scene(Method(shape, TargetingChoiceMode.Direction), range: 2));
                yield return Case($"{shape} 전체", new Scene(Method(shape, TargetingChoiceMode.All), range: 2));
            }

            var square = Method(TargetingShape.RadialSquare, TargetingChoiceMode.Single);
            var cardinal = Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single);
            yield return Case("정사각 사거리 1", new Scene(square, range: 1));
            yield return Case("정사각 사거리 3", new Scene(square, range: 3));
            yield return Case("가로세로 사거리 1", new Scene(cardinal, range: 1));
            yield return Case("가로세로 사거리 3", new Scene(cardinal, range: 3));
            yield return Case("가로세로 정면 각도", new Scene(Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single,
                angle: TargetingChoiceAngle.Front), range: 3));
            yield return Case("부채꼴 방향 정면·양옆 각도", new Scene(Method(TargetingShape.Fan, TargetingChoiceMode.Direction,
                angle: TargetingChoiceAngle.FrontAndSides), range: 2));
            yield return Case("기병 이동력", new Scene(square, range: 1, category: CategoryKey.Rider));

            var line = Method(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single);
            yield return Case("관통 무제한", new Scene(line, range: 3, piercingCap: int.MaxValue));
            yield return Case("관통 예산 1 앞 유닛", new Scene(line, range: 3, piercingCap: 1,
                blockers: new[] { new CellCoord(3, 4), new CellCoord(5, 2) }));
            yield return Case("관통 사거리 1 먼 대상", new Scene(line, range: 1, piercingCap: int.MaxValue));
            yield return Case("끌어오기 추가 사거리", new Scene(cardinal, range: 1, pullExtra: 1));
            yield return Case("막는 유닛", new Scene(square, range: 1,
                blockers: new[] { new CellCoord(4, 4), new CellCoord(5, 3), new CellCoord(4, 3) }));
            yield return Case("모래폭풍", new Scene(square, range: 2, terrain: new[] {
                Terrain(11, new CellCoord(3, 4), StaticTerrainKind.Sandstorm), Terrain(12, new CellCoord(4, 2), StaticTerrainKind.Sandstorm),
                Terrain(13, new CellCoord(2, 3), StaticTerrainKind.Sandstorm) }));
            yield return Case("고지대", new Scene(square, range: 2, terrain: new[] {
                Terrain(21, new CellCoord(4, 4), StaticTerrainKind.HighGround), Terrain(22, new CellCoord(3, 3), StaticTerrainKind.HighGround) }));
            yield return Case("지뢰 미로", new Scene(square, range: 1, maze: true));

            var radialSplash = Method(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial);
            yield return Case("보드 가장자리 대상 주변 스플래시", new Scene(radialSplash, range: 2, targetCells: new[] { new CellCoord(6, 3) }));
            yield return Case("보드 가장자리 대상 관통", new Scene(line, range: 3, piercingCap: int.MaxValue,
                targetCells: new[] { new CellCoord(6, 0) }));
            yield return Case("여러 칸 본부 대상", new Scene(square, range: 1, structureTarget: true,
                targetCells: new[] { new CellCoord(4, 4), new CellCoord(5, 4), new CellCoord(4, 5), new CellCoord(5, 5) }));
            yield return Case("여러 칸 본부 대상 십자 스플래시", new Scene(Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single,
                TargetingSplash.Cross), range: 2, structureTarget: true,
                targetCells: new[] { new CellCoord(4, 4), new CellCoord(5, 4), new CellCoord(4, 5), new CellCoord(5, 5) }));
            yield return Case("공격 사용함(공격자 조건 실패)", new Scene(square, range: 2, actionUse: UnitActionUse.Attacked));
            yield return Case("이동 사용함", new Scene(square, range: 2, actionUse: UnitActionUse.Moved));

            // 탐색 멈추기(설계 D4): 목적지가 없어 끝까지 도는 경우, 가까운 줄이 막혀 먼 줄에서만 칠 수 있는 경우
            var ring = new[] { new CellCoord(4, 3), new CellCoord(5, 3), new CellCoord(6, 3), new CellCoord(4, 4),
                new CellCoord(6, 4), new CellCoord(4, 5), new CellCoord(5, 5), new CellCoord(6, 5) };
            yield return Case("둘러싸인 대상 사거리 1(목적지 없음)", new Scene(square, range: 1, blockers: ring));
            yield return Case("둘러싸인 대상 사거리 2", new Scene(square, range: 2, blockers: ring));
            yield return Case("둘러싸인 대상 가로세로 사거리 3", new Scene(cardinal, range: 3, blockers: ring));
        }

        // {m}은 NUnit이 시험 메서드 이름으로 바꾼다. 두 시험이 같은 장면 목록을 쓰므로 이름이 겹치지 않게 한다.
        private static TestCaseData Case(string name, Scene scene) => new TestCaseData(scene).SetName($"{{m}}({name})");

        private static TargetingMethodSpec Method(TargetingShape shape, TargetingChoiceMode mode,
            TargetingSplash splash = TargetingSplash.None, TargetingChoiceAngle angle = TargetingChoiceAngle.Unrestricted) =>
            new(shape, mode, splash, splash == TargetingSplash.None ? 0 : 50, angle);

        private static StaticTerrainFact Terrain(long id, CellCoord cell, StaticTerrainKind kind) =>
            new(id, cell, kind, true, new[] { Owner.Enemy, Owner.Player });

        /// <summary>한 장면의 고정 값입니다. 행위자 출발 칸만 바꿔 입력을 만듭니다.</summary>
        public sealed class Scene
        {
            private readonly TargetingMethodSpec _method;
            private readonly int _range;
            private readonly CategoryKey _category;
            private readonly int? _piercingCap;
            private readonly int? _pullExtra;
            private readonly StaticTerrainFact[] _terrain;
            private readonly bool _maze;
            private readonly bool _structureTarget;
            private readonly UnitActionUse _actionUse;
            private readonly CellCoord[] _blockers;
            private readonly CellCoord[] _targetCells;

            internal Scene(TargetingMethodSpec method, int range, CategoryKey category = CategoryKey.Main,
                int? piercingCap = null, int? pullExtra = null, CellCoord[] blockers = null,
                StaticTerrainFact[] terrain = null, bool maze = false, CellCoord[] targetCells = null,
                bool structureTarget = false, UnitActionUse actionUse = UnitActionUse.Available)
            {
                _method = method;
                _range = range;
                _category = category;
                _piercingCap = piercingCap;
                _pullExtra = pullExtra;
                _blockers = blockers ?? Array.Empty<CellCoord>();
                _terrain = terrain ?? Array.Empty<StaticTerrainFact>();
                _maze = maze;
                _targetCells = targetCells ?? new[] { DefaultTargetCell };
                _structureTarget = structureTarget;
                _actionUse = actionUse;
            }

            internal EntityId TargetId => _structureTarget ? StructureTarget : UnitTarget;

            /// <summary>행위자를 세우지 않는 칸입니다: 다른 유닛·대상·지뢰가 있는 칸.</summary>
            internal HashSet<CellCoord> Occupied
            {
                get
                {
                    var cells = new HashSet<CellCoord>(_blockers.Concat(_targetCells).Append(MainCell));
                    if (_maze) cells.UnionWith(EnemyMovementPathTests.Maze().Tiles.Select(t => t.Cell));
                    return cells;
                }
            }

            internal CombatCommandInput Build(CellCoord start)
            {
                var source = CombatCommandTestData.Input(empty: true);
                var traits = new List<TraitStatusSnapshot>();
                var specs = new List<TraitSpec>();
                if (_piercingCap.HasValue)
                {
                    traits.Add(new PiercingFalloffTraitStatusSnapshot(30));
                    specs.Add(new PiercingFalloffTraitSpec(30, _piercingCap.Value));
                }
                if (_pullExtra.HasValue)
                {
                    traits.Add(new PullTraitStatusSnapshot(_pullExtra.Value));
                    specs.Add(new PullTraitSpec(_pullExtra.Value));
                }
                var enemy = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Demon, start, Owner.Enemy, 10),
                    appointRange: _range, category: CategorySnapshot.CreateStateless(_category), traits: traits.ToArray(),
                    actionUse: _actionUse);
                var main = CombatCommandTestData.With(source.Characters[1], position: MainCell);
                // 메인 캐릭터가 개체 10을 쓰므로 막는 유닛은 20번대로 둔다.
                var blockers = _blockers.Select((cell, i) =>
                    CombatCommandTestData.Character(20 + i, EntityKey.Unit_Akimbo, cell, Owner.Player));
                var units = new List<CharacterCombatFacts> { enemy, main };
                if (!_structureTarget)
                    units.Add(CombatCommandTestData.Character(2, EntityKey.Unit_Akimbo, _targetCells[0], Owner.Player));
                var characters = units.Concat(blockers).ToArray();
                var structures = _structureTarget
                    ? new[] { new StructureCombatFacts(StructureTarget, new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                        new[] { CellCoord.Zero }, 10, false, 0), new HealthStateSnapshot(10, 10), _targetCells[0], Rotation4.R0,
                        _targetCells, Owner.Player) }
                    : Array.Empty<StructureCombatFacts>();
                var environment = _maze ? EnemyMovementPathTests.Maze() : null;
                var state = new CombatCommandState(Size, Size, CombatFlowPhase.Automatic, true,
                    structures, Array.Empty<ThornFieldCellSnapshot>(),
                    new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                        controllerTurnSequence: 2, completedPlayerTurnCount: 1), environment);
                var profiles = source.Profiles.Select(p => p.Definition.Key == enemy.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(enemy.CharacterKey, 10, _category, specs.ToArray()), _method)
                    : p).ToArray();
                var input = new CombatCommandInput(0, CombatCommandKind.Attack, enemy.EntityId, default, state,
                    characters, profiles, source.Main,
                    characters.Where(c => c.OwnedId.HasValue).Select(c =>
                        new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                    characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, _ => 0),
                    source.Souls, source.Artifacts, battle: new CombatBattleFacts(10, 1, 0, 0,
                        Array.Empty<CharacterInstanceId>(), 0), processingKind: CombatProcessingKind.Advance);
                return _terrain.Length == 0 ? input : HighGroundMovementTests.TerrainInput(input, _terrain);
            }

            public override string ToString() => $"{_method.Shape}/{_method.ChoiceMode}/{_method.Splash}/사거리 {_range}";
        }
    }
}
