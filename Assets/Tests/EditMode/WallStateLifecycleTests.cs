using System;
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

namespace ProjectL.Tests.Core
{
    public sealed class WallStateLifecycleTests
    {
        [Test]
        public void InitializationCopyAndPreparation_PreserveNondefaultSettings()
        {
            var initialization = Initialization(new[] { Wall(71, new CellCoord(4, 3)) });
            var copy = initialization.WithAttackProfiles(initialization.AttackProfiles);
            Assert.That(copy.WallBlocking, Is.SameAs(initialization.WallBlocking));
            var prepared = Prepare(copy);
            Assert.That(prepared.Combat.WallBlocking, Is.SameAs(initialization.WallBlocking));
            Assert.That(prepared.CommandState.WallBlocking, Is.SameAs(initialization.WallBlocking));
            Assert.That(prepared.CommandState.Environment.SameValues(initialization.Environment), Is.True);
        }

        [Test]
        public void SameValues_DetectsSettingChangeEvenWithoutWalls()
        {
            var source = CombatCommandTestData.Input().State;
            Assert.That(Copy(source, 257).SameValues(Copy(source, 257)), Is.True);
            Assert.That(Copy(source, 257).SameValues(Copy(source, 128)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MovementProjection_PreservesSettingsAndSourceSnapshot(bool preview)
        {
            var input = WallMovementTests.WithWalls(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right),
                Wall(71, new CellCoord(4, 3)));
            var projection = new MovementProjection(input, preview);
            projection.Enter(CellCoord.Right, 1);
            Assert.That(projection.CurrentInput().State.WallBlocking, Is.SameAs(input.State.WallBlocking));
            Assert.That(input.Characters[0].Anchor, Is.EqualTo(CellCoord.Zero));
            Assert.That(input.State.Environment.Tiles.Single().Cell, Is.EqualTo(new CellCoord(4, 3)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AcceptedAndRejectedCommands_PreserveWallFactsAndSettings(bool blocked)
        {
            var input = WallMovementTests.WithWalls(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right),
                Wall(71, blocked ? CellCoord.Right : new CellCoord(4, 3)));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(!blocked), result.Reason);
            Assert.That(result.FinalState.WallBlocking, Is.SameAs(input.State.WallBlocking));
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            if (blocked) Assert.That(result.FinalState.SameValues(input.State), Is.True);
            else Assert.That(CombatCommandComposition.Project(result).State.WallBlocking.CornerCutTicks, Is.EqualTo(257));
            Assert.That(CombatTurnCalculator.State(input.State, input.State.Turn, input.State.Phase)
                .WallBlocking, Is.SameAs(input.State.WallBlocking));
        }

        [TestCase("character")]
        [TestCase("structure")]
        [TestCase("object")]
        [TestCase("terrain")]
        [TestCase("rail")]
        public void Preparation_RejectsWallOverlap(string occupant)
        {
            var cell = occupant == "character" ? CellCoord.Zero : new CellCoord(4, 5);
            var sources = new MovementTileFact[] { Wall(71, cell) };
            if (occupant == "object") sources = sources.Append(new MineSourceFact(72, cell, 1, 7, true,
                new[] { Owner.Player, Owner.Enemy })).ToArray();
            if (occupant == "terrain") sources = sources.Append(new StaticTerrainFact(72, cell,
                StaticTerrainKind.HighGround, true, new[] { Owner.Player })).ToArray();
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            var stage = Stage(occupant == "rail" ? new[] { cell } : null,
                occupant == "structure" ? new[] { new InitialEnemyStructurePlacementSpec(definition.Key, cell, Rotation4.R0) } : null);
            var initialization = Initialization(sources, stage,
                occupant == "structure" ? new[] { definition } : null);
            Assert.That(() => Prepare(initialization), Throws.Exception);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void Preparation_RejectsInactiveOrHiddenWall(bool active, bool publicWall)
        {
            var wall = new StaticTerrainFact(71, new CellCoord(4, 3), StaticTerrainKind.Wall, active,
                publicWall ? new[] { Owner.Player, Owner.Enemy } : new[] { Owner.Player });
            Assert.Throws<ArgumentException>(() => Prepare(Initialization(new[] { wall })));
        }

        [Test]
        public void Preparation_RejectsBentWallWithSourceEvidence()
        {
            var error = Assert.Throws<ArgumentException>(() => Prepare(Initialization(new[] {
                Wall(71, new CellCoord(3, 3)), Wall(72, new CellCoord(4, 3)), Wall(73, new CellCoord(4, 4)) })));
            Assert.That(error.Message, Does.Contain("71").And.Contain("73"));
        }

        [Test]
        public void PreparationWithoutWalls_UsesDefaultAndPreservesLegacyBehavior()
        {
            var empty = Initialization(Array.Empty<MovementTileFact>(), settings: WallBlockingSpec.Default);
            Assert.That(Prepare(empty).CommandState.WallBlocking.CornerCutTicks, Is.EqualTo(128));
        }

        internal static StaticTerrainFact Wall(long id, CellCoord cell) =>
            new(id, cell, StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });

        private static CombatCommandState Copy(CombatCommandState s, int cut) => new(s.BoardWidth, s.BoardHeight,
            s.Phase, s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment,
            s.EnvironmentProgress, new WallBlockingSpec(cut));

        private static StageConfig Stage(CellCoord[] rails = null, InitialEnemyStructurePlacementSpec[] structures = null) =>
            new(8, 6, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(), rails ?? Array.Empty<CellCoord>(),
                rails == null ? Array.Empty<StageGimmickSpec>() : new StageGimmickSpec[] { new RailwayGimmickSpec(3, 3) },
                100, structures ?? Array.Empty<InitialEnemyStructurePlacementSpec>());

        private static BattleInitialization Initialization(MovementTileFact[] sources, StageConfig stage = null,
            StructureSpec[] structures = null, WallBlockingSpec settings = null)
        {
            var unit = UnitSpecTestFactory.Create(EntityKey.Unit_MainCharacter, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 3, 1, 1, 5, 4, 1, 1, 1, Array.Empty<TraitSpec>(),
                role: UnitRole.MainCharacter, ownerEligibility: UnitOwnerEligibility.PlayerOnly);
            return new BattleInitialization(stage ?? Stage(), CellCoord.Zero, Rotation4.R0, new[] { unit },
                structures ?? Array.Empty<StructureSpec>(), UnitSpecTestFactory.CreateDefaultCategoryCatalog(),
                environment: new MovementEnvironmentFacts(sources), environmentBindings: new EnvironmentSemanticBindings(
                    sources.Where(s => s is not ObjectSourceFact).Select(s => s.Id).ToArray(),
                    sources.OfType<ObjectSourceFact>().Select(s => s.Id).ToArray(), Array.Empty<EntityId>()),
                wallBlocking: settings ?? new WallBlockingSpec(257));
        }

        private static BattlePreparation Prepare(BattleInitialization initialization)
        {
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main);
            return BattlePreparationCalculator.Prepare(initialization, new[] { owned },
                new MainCharacterPersistentState(4, 12, 7, 6, 5, 2),
                new System.Collections.Generic.Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 },
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>());
        }
    }
}
