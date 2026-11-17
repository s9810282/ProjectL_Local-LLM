using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>전투 준비의 최초 무작위 토네이도 배치(후보·순서·난수·실패·진행값)를 검사한다.</summary>
    public sealed class InitialTornadoPlacementTests
    {
        private static readonly Owner[] Both = { Owner.Player, Owner.Enemy };

        [Test]
        public void RandomTornado_UsesLegalCandidatesAndSeparateDeterministicRandomness()
        {
            var initialization = MixedBoard(7);
            var prepared = Prepare(initialization);
            var storm = prepared.CommandState.Gimmicks.Entries.OfType<TornadoGimmickState>().Single();
            Assert.That(storm.Members.Single().SourceId, Is.EqualTo(301));
            Assert.That(storm.Members.Single().Center, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(prepared.Combat.Environment.Tiles.OfType<TornadoSourceFact>(), Is.Empty);
            Assert.That(prepared.Combat.EnvironmentBindings.TerrainSourceIds.Contains(301), Is.False);
            Assert.That(storm.NextBoundaryCount, Is.EqualTo(2));
            Assert.That(prepared.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(302));
            Assert.That(Prepare(initialization).CommandState.Gimmicks.SameValues(prepared.CommandState.Gimmicks), Is.True);
            Assert.That(prepared.CommandState.EnvironmentProgress.RandomState, Is.Not.EqualTo(EnvironmentRandom.InitialState(7)));
            Assert.That(prepared.CommandState.ItemRandomState, Is.EqualTo(7u));
        }

        [Test]
        public void MultipleRandomSources_UseIdOrderAndDistinctCells()
        {
            var one = new InitialRandomTornadoPlacementSpec(301, Both);
            var two = new InitialRandomTornadoPlacementSpec(302, Both);
            var first = Prepare(Initialization(Stage(11), specs: new[] { two, one }));
            var second = Prepare(Initialization(Stage(11), specs: new[] { one, two }));
            var members = first.CommandState.Gimmicks.Entries.OfType<TornadoGimmickState>().Single().Members;
            Assert.That(members.Select(m => m.SourceId), Is.EqualTo(new long[] { 301, 302 }));
            Assert.That(members.Select(m => m.Center).Distinct().Single(), Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(first.CommandState.Gimmicks.SameValues(second.CommandState.Gimmicks), Is.True);
            Assert.That(first.CommandState.EnvironmentProgress.SameValues(second.CommandState.EnvironmentProgress), Is.True);
        }

        [Test]
        public void ValidStormCanCoverStructuresWithoutMutatingDefinition()
        {
            var initialization = Initialization(Stage(1), obstacles: BarrelsExcept(CellCoord.Zero), specs: new[] {
                new InitialRandomTornadoPlacementSpec(301, Both), new InitialRandomTornadoPlacementSpec(302, Both) });
            var prepared = Prepare(initialization);
            Assert.That(prepared.CommandState.Gimmicks.Entries.OfType<TornadoGimmickState>().Single().Members.Count, Is.EqualTo(2));
            Assert.That(initialization.Environment.Tiles, Is.Empty);
        }

        [Test]
        public void CoincidentEdgesUseSeededSideChoice()
        {
            var initialization = Initialization(Stage(1), specs: new[] { new InitialRandomTornadoPlacementSpec(301, Both) });
            var prepared = Prepare(initialization);
            Assert.That(prepared.CommandState.Gimmicks.Entries.OfType<TornadoGimmickState>().Single().Members.Single().Center, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(prepared.CommandState.EnvironmentProgress.RandomState, Is.Not.EqualTo(EnvironmentRandom.InitialState(1)));
        }

        [Test]
        public void InvalidFixedCenterIsRejectedWithoutMovingAuthoredContent()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { new TornadoSourceFact(91, new CellCoord(1, 0), Both) });
            var bindings = new EnvironmentSemanticBindings(new[] { 91L }, Array.Empty<long>(), Array.Empty<EntityId>());
            Assert.Throws<InvalidOperationException>(() => Prepare(Initialization(Stage(1), environment, bindings)));
            Assert.That(environment.Tiles.Single().Cell, Is.EqualTo(new CellCoord(1, 0)));
        }

        [Test]
        public void Spec_CreatesSourceWithInitialDueAndValidatesThroughRuntimeFact()
        {
            var spec = new InitialRandomTornadoPlacementSpec(301, new[] { Owner.Enemy, Owner.Player }, 2, 5);
            var source = spec.CreateSource(new CellCoord(2, 1));

            Assert.That(source.Id, Is.EqualTo(301));
            Assert.That(source.Cell, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(source.Active, Is.True);
            Assert.That(source.KnownTo, Is.EqualTo(Both));
            Assert.That(source.NextDueCompletedTurn, Is.EqualTo(2));
            Assert.That(source.RepeatIntervalUnitTurns, Is.EqualTo(5));
            var defaults = new InitialRandomTornadoPlacementSpec(302, Both).CreateSource(CellCoord.Zero);
            Assert.That(defaults.NextDueCompletedTurn, Is.EqualTo(3));
            Assert.That(defaults.RepeatIntervalUnitTurns, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InitialRandomTornadoPlacementSpec(0, Both));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InitialRandomTornadoPlacementSpec(301, Both, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InitialRandomTornadoPlacementSpec(301, Both, 3, 0));
            Assert.Throws<ArgumentNullException>(() => new InitialRandomTornadoPlacementSpec(301, null));
        }

        [Test]
        public void WithAttackProfiles_PreservesInitialRandomSpecs()
        {
            var spec = new InitialRandomTornadoPlacementSpec(301, Both);
            var initialization = Initialization(Stage(1), specs: new[] { spec });

            var copy = initialization.WithAttackProfiles(Array.Empty<CombatAttackProfile>());

            Assert.That(copy.InitialRandomTornadoPlacements, Is.EqualTo(new[] { spec }));
            Assert.That(Initialization(Stage(1)).InitialRandomTornadoPlacements, Is.Empty);
        }

        [Test]
        public void InitialState_IsDeterministicSeparateFromItemStreamAndAcceptsZero()
        {
            Assert.That(EnvironmentRandom.InitialState(0), Is.EqualTo(4282259411u));
            Assert.That(EnvironmentRandom.InitialState(1), Is.EqualTo(2520119581u));
            Assert.That(EnvironmentRandom.InitialState(1), Is.EqualTo(EnvironmentRandom.InitialState(1)));
            foreach (int seed in new[] { 0, 1, 42, -1, int.MaxValue })
                Assert.That(EnvironmentRandom.InitialState(seed), Is.Not.EqualTo(unchecked((uint)seed)), $"seed {seed}");
        }

        [Test]
        public void BattlePreparation_ExplicitProgressSeedsDefaultStateAndMustMatchInjectedState()
        {
            var prepared = Prepare(MixedBoard(7));
            var progress = prepared.CommandState.EnvironmentProgress;
            var other = new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, progress.RandomState + 1);

            var rebuilt = new BattlePreparation(prepared.Characters, prepared.Combat, prepared.Structures, environmentProgress: other);
            Assert.That(rebuilt.CommandState.EnvironmentProgress.SameValues(other), Is.True);
            Assert.DoesNotThrow(() => new BattlePreparation(prepared.Characters, prepared.Combat, prepared.Structures,
                commandState: prepared.CommandState, environmentProgress: progress));
            Assert.DoesNotThrow(() => new BattlePreparation(prepared.Characters, prepared.Combat, prepared.Structures,
                commandState: prepared.CommandState));
            Assert.Throws<ArgumentException>(() => new BattlePreparation(prepared.Characters, prepared.Combat, prepared.Structures,
                commandState: prepared.CommandState, environmentProgress: other));
        }

        /// <summary>
        /// 3x3 보드(배치 구역: y=0 플레이어, y=2 적):
        /// (0,0) 비활성 Terrain, (1,0) 장애물, (2,0) 벽 / y=1 장애물 3개 / (0,2) 본부, (1,2) 적, (2,2) 지뢰 Object.
        /// 허용 후보는 행 우선으로 (0,0), (1,2), (2,2)이다.
        /// </summary>
        private static BattleInitialization MixedBoard(int seed)
        {
            var inactive = new DestinationTileModifierFact(11, new CellCoord(0, 0), false, Both, 2, 3);
            var wall = new StaticTerrainFact(12, new CellCoord(2, 0), StaticTerrainKind.Wall, true, Both);
            var mine = new MineSourceFact(21, new CellCoord(2, 2), 1, 7, true, Both);
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { inactive, wall, mine });
            var bindings = new EnvironmentSemanticBindings(new[] { 11L, 12L }, new[] { 21L }, Array.Empty<EntityId>());
            var headquarters = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var stage = Stage(seed,
                new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 2), Rotation4.R0) },
                new[] { new InitialEnemyStructurePlacementSpec(headquarters.Key, new CellCoord(0, 2), Rotation4.R0) });
            var barrels = new[] { new CellCoord(1, 0), new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1) }
                .Select(Barrel).ToArray();
            return Initialization(stage, environment, bindings, new[] { headquarters }, barrels,
                new[] { new InitialRandomTornadoPlacementSpec(301, Both) });
        }

        private static InitialObstaclePlacementSpec[] BarrelsExcept(params CellCoord[] free) =>
            Enumerable.Range(0, 3).SelectMany(y => Enumerable.Range(0, 3).Select(x => new CellCoord(x, y)))
                .Where(cell => !free.Contains(cell)).Select(Barrel).ToArray();

        private static InitialObstaclePlacementSpec Barrel(CellCoord cell) =>
            new(new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 10, false, 0),
                ObstacleContentKind.PowderBarrel, cell, Rotation4.R0, 2, 7);

        private static StageConfig Stage(int seed,
            InitialEnemyPlacementSpec[] enemies = null, InitialEnemyStructurePlacementSpec[] structures = null) =>
            new(3, 3, 8, 1, 1, seed, enemies ?? Array.Empty<InitialEnemyPlacementSpec>(), Array.Empty<CellCoord>(),
                Array.Empty<StageGimmickSpec>(), structures ?? Array.Empty<InitialEnemyStructurePlacementSpec>());

        private static BattleInitialization Initialization(StageConfig stage, MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings bindings = null, StructureSpec[] structures = null,
            InitialObstaclePlacementSpec[] obstacles = null, InitialRandomTornadoPlacementSpec[] specs = null) =>
            new(stage, CellCoord.Zero, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) },
                structures ?? Array.Empty<StructureSpec>(), UnitSpecTestFactory.CreateDefaultCategoryCatalog(),
                environment: environment, environmentBindings: bindings, obstaclePlacements: obstacles,
                initialRandomTornadoPlacements: specs);

        private static BattlePreparation Prepare(BattleInitialization initialization)
        {
            var roster = new[]
            {
                new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Gravekeeper, CharacterOwnershipRole.Main),
                new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            };
            return BattlePreparationCalculator.Prepare(initialization, roster,
                new MainCharacterPersistentState(4, 12, 7),
                roster.ToDictionary(r => r.Id, _ => 0), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }),
                Array.Empty<CharacterCombatFacts>());
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility) =>
            new(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace,
                1, CategoryKey.Main,
                5, 0, 1, Array.Empty<TraitSpec>(), eligibility);
    }
}
