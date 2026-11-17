using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using static ProjectL.Tests.Core.PlatformTestData;

namespace ProjectL.Tests.Core
{
    public sealed class PlatformSurfaceTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Anchor = new CellCoord(2, 1);
        private static readonly CellCoord RiderCell = new CellCoord(3, 1);
        private static readonly CellCoord Floor = new CellCoord(3, 2);

        [TestCase(ObstacleContentKind.PowderBarrel, false, false)]
        [TestCase(ObstacleContentKind.Pillar, false, false)]
        [TestCase(ObstacleContentKind.Supplies, false, false)]
        [TestCase(ObstacleContentKind.PowderBarrel, true, false)]
        [TestCase(ObstacleContentKind.PowderBarrel, false, true)]
        public void ObstacleKnockbackCannotEnterAnyPlatformCell(ObstacleContentKind kind, bool largeObstacle, bool nonAnchorPlatformCell)
        {
            var seed = CliffFallTests.Knockback();
            var origin = new CellCoord(4, 3);
            var local = largeObstacle ? new[] { CellCoord.Zero, CellCoord.Up } : new[] { CellCoord.Zero };
            var definition = new StructureSpec(ObstacleContentProfile.KeyFor(kind), 1, local.Length, local, 1, false, 0);
            var obstacle = new StructureCombatFacts(new EntityId(51), definition, new ObstacleDurabilitySnapshot(3, 3),
                origin, Rotation4.R0, local.Select(c => c + origin).ToArray());
            var profile = new ObstacleContentProfile(obstacle.EntityId, kind, 0,
                kind == ObstacleContentKind.Supplies ? 0 : (int?)null, kind == ObstacleContentKind.Supplies ? 0 : (int?)null);
            var platformCell = new CellCoord(5, largeObstacle ? 4 : nonAnchorPlatformCell ? 2 : 3);
            var platform = Platform(100, platformCell, platformCell + CellCoord.Right,
                nonAnchorPlatformCell ? 2 : 1, PlatformAxis.Vertical);
            var input = ObstacleReactionTests.Copy(seed, characters: seed.Characters.Where(c => c.EntityId.Value != 2 && c.EntityId.Value != 3).ToArray(),
                structures: new[] { obstacle }, profiles: new[] { profile });
            input = PillarTestData.WithTerrain(input, platform.OccupiedCells.Select((c, i) => (MovementTileFact)Terrain(i + 1, c)).Append(platform).ToArray());
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            var preview = CombatCommandCalculator.Preview(input, NoRules, out var predicted);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().Anchor, Is.EqualTo(origin));
            Assert.That(result.FinalState.Structures.Single().OccupiedCells, Is.EqualTo(obstacle.OccupiedCells));
            Assert.That(result.FinalState.Structures.Single().ObstacleDurability.RemainingHits, Is.EqualTo(2));
            Assert.That(predicted.FinalState.Structures.Single().Anchor, Is.EqualTo(origin));
            Assert.That(result.Events.OfType<CombatStructureEvent>().Any(e => e.MovementCause != null), Is.False);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            CombatChangeEvidenceReader.ReadAll(result);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FootprintUsesShapeAxisIndependentlyOfTheRoute(int length)
        {
            var end = new CellCoord(2, 4);
            var platform = Platform(100, Anchor, end, length);
            Assert.That(platform.Spec.TravelAxis, Is.EqualTo(PlatformAxis.Vertical));
            Assert.That(platform.OccupiedCells, Is.EqualTo(Enumerable.Range(0, length).Select(x => new CellCoord(2 + x, 1))));
            Assert.That(platform.Spec.FootprintAt(end), Is.EqualTo(Enumerable.Range(0, length).Select(x => new CellCoord(2 + x, 4))));
            Assert.That(platform.TriggersArrivalRule, Is.False);
            var vertical = Platform(101, Anchor, RiderCell, length, PlatformAxis.Vertical);
            Assert.That(vertical.Spec.TravelAxis, Is.EqualTo(PlatformAxis.Horizontal));
            Assert.That(vertical.OccupiedCells, Is.EqualTo(Enumerable.Range(0, length).Select(y => new CellCoord(2, 1 + y))));
        }

        [Test]
        public void InvalidDefinitionIsRejectedAtConstruction()
        {
            var end = new CellCoord(4, 1);
            Assert.Throws<ArgumentException>(() => Platform(1, Anchor, Anchor));
            Assert.Throws<ArgumentException>(() => Platform(1, Anchor, Floor));
            Assert.Throws<ArgumentOutOfRangeException>(() => Platform(1, Anchor, end, anchor: Floor));
            Assert.Throws<ArgumentOutOfRangeException>(() => Platform(1, Anchor, end, length: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Platform(1, Anchor, end, length: 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => Platform(0, Anchor, end));
            Assert.Throws<ArgumentOutOfRangeException>(() => Platform(1, Anchor, end, shape: (PlatformAxis)0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MovingPlatformSpec(1, 1, PlatformAxis.Horizontal, Anchor, end, Anchor, 0));
        }

        [Test]
        public void SourceIdentityAndVisibilityPreserveTypedFacts()
        {
            var platform = Platform(100, Anchor, RiderCell, 2);
            var copied = (MovingPlatformFact)platform.WithKnownTo(new[] { Owner.Enemy });
            Assert.That(copied.Spec, Is.SameAs(platform.Spec));
            Assert.That(copied.TravelSign, Is.EqualTo(platform.TravelSign));
            Assert.That(copied.OccupiedCells, Is.EqualTo(platform.OccupiedCells));
            Assert.That(new MovementEnvironmentFacts(new[] { copied }).VisibleTo(Owner.Player).Tiles, Is.Empty);
            Assert.That(platform.SameValues(copied), Is.False);
            Assert.That(platform.SameValues(copied.WithKnownTo(new[] { Owner.Player, Owner.Enemy })), Is.True);
            Assert.That(platform.SameValues(new MovingPlatformFact(platform.Spec, Anchor, -1)), Is.False);
            Assert.That(platform.SameValues(new MovingPlatformFact(platform.Spec, RiderCell, 1)), Is.False);
            Assert.Throws<ArgumentException>(() => new MovementEnvironmentFacts(new MovementTileFact[] { platform, Terrain(100, Anchor) }));
            Assert.Throws<ArgumentException>(() => EnvironmentSemanticBindings.Empty.ValidateInitialReferences(
                new MovementEnvironmentFacts(new[] { platform }), NoStructures));
        }

        [Test]
        public void InitialFootprintsMayShareARouteButMustHaveSeparateCliffs()
        {
            var end = new CellCoord(6, 1);
            var platform = Platform(100, Anchor, end, 2);
            var otherCliff = Terrain(4, end);
            var firstCliff = Terrain(1, Anchor);
            var secondCliff = Terrain(2, RiderCell);
            var other = Platform(101, Anchor, end, anchor: end);
            Assert.DoesNotThrow(() => ValidateLayout(firstCliff, secondCliff, otherCliff, platform, other));
            Assert.Throws<InvalidOperationException>(() =>
                ValidateLayout(firstCliff, secondCliff, platform, Platform(101, Anchor, end, anchor: RiderCell)));
            Assert.Throws<InvalidOperationException>(() => ValidateLayout(firstCliff, platform));
            Assert.Throws<InvalidOperationException>(() =>
                ValidateLayout(firstCliff, Terrain(3, RiderCell, StaticTerrainKind.FilledCliff), platform));
            Assert.Throws<InvalidOperationException>(() =>
                ValidateLayout(firstCliff, secondCliff, Terrain(3, RiderCell, StaticTerrainKind.HighGround), platform));
            Assert.Throws<InvalidOperationException>(() =>
                ValidateLayout(firstCliff, secondCliff, Platform(100, Anchor, new CellCoord(7, 1), 2)));
        }

        [Test]
        public void InitialPlatformAdmitsAnOrdinaryUnitAndRejectsABossOrStructure()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { Terrain(1, Anchor),
                Terrain(2, RiderCell), Platform(100, Anchor, new CellCoord(2, 3), 2) });
            var bindings = Bind(environment);
            var unit = CombatCommandTestData.Character(8, EntityKey.Unit_Demon, RiderCell, Owner.Enemy);
            var boss = unit.With(stats: unit.Stats.With(category: CategorySnapshot.CreateStateless(CategoryKey.Boss)));
            var structure = new StructureCombatFacts(new EntityId(9), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 5, false, 0), new HealthStateSnapshot(5, 5),
                RiderCell, Rotation4.R0, new[] { RiderCell });

            void Validate(StructureCombatFacts[] structures, CharacterCombatFacts[] characters) =>
                CellEnvironmentQuery.ValidateInitialPlacement(8, 8, environment, structures, characters, bindings,
                    WallBlockingSpec.Default, Array.Empty<CellCoord>());

            Assert.DoesNotThrow(() => Validate(NoStructures, new[] { unit }));
            Assert.Throws<InvalidOperationException>(() => Validate(NoStructures, new[] { boss }));
            Assert.Throws<InvalidOperationException>(() => Validate(new[] { structure }, NoCharacters));
        }

        [Test]
        public void EffectiveSupportAndWholeFootprintRouteUseDifferentSurfaceRules()
        {
            var destination = new CellCoord(2, 2);
            var platform = Platform(100, Anchor, destination, 3);
            var filled = new CellCoord(4, 2);
            var sources = new MovementTileFact[] { platform, Terrain(1, Anchor), Terrain(2, RiderCell),
                Terrain(3, new CellCoord(4, 1)), Terrain(4, destination), Terrain(5, Floor),
                Terrain(6, filled, StaticTerrainKind.FilledCliff) };
            var environment = new MovementEnvironmentFacts(sources);
            var bindings = Bind(environment);
            Assert.That(BoardSurfaceRules.HasPlatformSupport(environment, bindings, RiderCell), Is.True);
            Assert.That(BoardSurfaceRules.HasPlatformSupport(environment, EnvironmentSemanticBindings.Empty, RiderCell), Is.False);
            Assert.That(BoardSurfaceRules.IsUnsupportedCliff(environment, bindings, RiderCell), Is.False);
            Assert.That(BoardSurfaceRules.IsUnsupportedCliff(environment, bindings, destination), Is.True);
            Assert.That(EnvironmentTerrainRules.BlocksVoluntaryEntry(environment, bindings, filled), Is.False);
            Assert.That(BoardSurfaceRules.IsPlatformRouteSurface(environment, bindings, filled), Is.False);
            Assert.That(BoardSurfaceRules.IsPlatformRouteSurface(environment, bindings, platform.Spec, Anchor), Is.True);
            Assert.That(BoardSurfaceRules.IsPlatformRouteSurface(environment, bindings, platform.Spec, destination), Is.False);
            sources[6] = Terrain(6, filled);
            Assert.That(BoardSurfaceRules.IsPlatformRouteSurface(new MovementEnvironmentFacts(sources), bindings,
                platform.Spec, destination), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VoluntaryMoveBoardsOrDisembarksByOccupancy(bool disembark)
        {
            var platform = Platform(100, Anchor, new CellCoord(2, 3), 2);
            var input = OnBoard(disembark ? MovementTestData.SafeRoute(RiderCell, Floor) :
                MovementTestData.SafeRoute(Floor, RiderCell), Terrain(1, Anchor), Terrain(2, RiderCell), platform);
            var snapshot = CellEnvironmentQuery.Read(input.State, input.Characters, input.EnvironmentBindings, RiderCell);
            Assert.That(snapshot.TerrainSourceIds, Is.EquivalentTo(new long[] { 2, 100 }));
            Assert.That(CombatMovementCalculator.Preview(input, NoRules).CanMove, Is.True);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(input.Path.Endpoint));
            Assert.That(result.FinalState.Environment.Tiles.Single(tile => tile.Id == platform.Id).SameValues(platform), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MoveCanEnterSupportedVacancyOnly(bool supported)
        {
            var seed = MovementTestData.SafeRoute(Floor, RiderCell);
            var rawCliff = Terrain(2, RiderCell);
            var input = supported
                ? OnBoard(seed, rawCliff, Terrain(1, Anchor), Platform(100, Anchor, new CellCoord(2, 3), 2))
                : OnBoard(seed, rawCliff);
            if (supported)
                input = CombatCommandTestData.With(input, characters: input.Characters.Append(
                    CombatCommandTestData.Character(8, EntityKey.Unit_Demon, RiderCell, Owner.Enemy)).ToArray());
            Assert.That(CombatMovementCalculator.Preview(input, NoRules).CanMove, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(input, NoRules).Accepted, Is.False);
        }

        [TestCase(StaticTerrainKind.Cliff, false)]
        [TestCase(StaticTerrainKind.FilledCliff, true)]
        public void PlacementAndMoveAgreeOnUnderlyingTerrain(StaticTerrainKind kind, bool walkable)
        {
            var seed = MovementTestData.SafeRoute(Floor, RiderCell);
            var unassisted = OnBoard(seed, Terrain(2, RiderCell, kind));
            var supported = OnBoard(seed, Terrain(1, Anchor), Terrain(2, RiderCell), Platform(100, Anchor, new CellCoord(2, 3), 2));
            Assert.That(CombatMovementCalculator.Preview(unassisted, NoRules).CanMove, Is.EqualTo(walkable));
            Assert.That(CombatPlacementCalculator.Preview(Deployment(unassisted)).CanPlace, Is.EqualTo(walkable));
            Assert.That(CombatPlacementCalculator.Preview(Deployment(supported)).CanPlace, Is.True);
        }

        [Test]
        public void EngagementFieldAndMovePreviewAgreeAcrossTheOnlyBridge()
        {
            var origin = new CellCoord(0, 1);
            var bridge = new CellCoord(3, 1);
            var seed = EnemyFollowupEstimateTests.World(width: 7, height: 3, actorCell: origin, targetCell: new CellCoord(6, 1));
            var sides = Enumerable.Range(0, 7).SelectMany(x => new MovementTileFact[] {
                Terrain(10 + x, new CellCoord(x, 0), StaticTerrainKind.Wall),
                Terrain(20 + x, new CellCoord(x, 2), StaticTerrainKind.Wall) });
            var raw = sides.Append(Terrain(1, bridge)).ToArray();
            var unassisted = OnBoard(seed, raw);
            var assisted = OnBoard(seed, raw.Append(Platform(100, bridge, new CellCoord(4, 1))).ToArray());
            Assert.That(EnemyEngagementFieldTests.Build(unassisted).TryGetCost(origin, out _), Is.False);
            Assert.That(EnemyEngagementFieldTests.Build(assisted).TryGetCost(origin, out _), Is.True);
            var route = new[] { origin, new CellCoord(1, 1), new CellCoord(2, 1), bridge };
            Assert.That(CombatMovementCalculator.Preview(MovementTestData.Route(unassisted, route), NoRules).CanMove, Is.False);
            Assert.That(CombatMovementCalculator.Preview(MovementTestData.Route(assisted, route), NoRules).CanMove, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WeightedSpawnUsesSupportedCellsForOrdinaryUnitsAndReservesFloorForBosses(bool boss)
        {
            var bridge = new CellCoord(0, 4);
            var floor = new CellCoord(7, 5);
            var stage = new StageConfig(8, 6, 8, 4, 2);
            var cells = Enumerable.Range(0, 16).Select(i => new CellCoord(i % 8, 4 + i / 8))
                .Where(cell => cell != floor)
                .Select((cell, i) => (MovementTileFact)Terrain(i + 1, cell)).ToList();
            cells.Add(Platform(100, bridge, new CellCoord(1, 4)));
            var environment = new MovementEnvironmentFacts(cells);
            var unit = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100,
                MoveMethod.LiftAndPlace, 1, boss ? CategoryKey.Boss : CategoryKey.Main,
                1, 0, 1, Array.Empty<TraitSpec>(), UnitOwnerEligibility.EnemyOnly,
                behavior: boss ? new UnitBehaviorSpec(false, true, true) : null);
            var placements = EnemySpawnCalculator.Calculate(stage, new[] { new EnemySpawnCandidate(unit, 1) },
                boss ? 1 : 2, 7, Array.Empty<StructureSpec>(), environment, Bind(environment));
            if (boss)
                Assert.That(placements.Single().Anchor, Is.EqualTo(floor));
            else
                Assert.That(placements.Select(p => p.Anchor), Is.EquivalentTo(new[] { bridge, floor }));
        }

        [Test]
        public void PlatformPositionInvalidatesAnOtherwiseLegalMoveContext()
        {
            var platform = Platform(100, Anchor, RiderCell, 2);
            var seed = MovementTestData.SafeRoute(Floor, RiderCell);
            var input = OnBoard(seed, Terrain(1, Anchor), Terrain(2, RiderCell), Terrain(3, new CellCoord(4, 1)), platform);
            var context = MovementContextCalculator.Capture(input, Owner.Player, NoRules);
            Assert.That(context, Is.Not.Null);
            Assert.That(MovementContextCalculator.Matches(context, input, Owner.Player, NoRules), Is.True);
            var moved = new MovingPlatformFact(platform.Spec, RiderCell, 1);
            var projected = OnBoard(seed, Terrain(1, Anchor), Terrain(2, RiderCell), Terrain(3, new CellCoord(4, 1)), moved);
            Assert.That(CombatMovementCalculator.Preview(projected, NoRules).CanMove, Is.True);
            Assert.That(MovementContextCalculator.Matches(context, projected, Owner.Player, NoRules), Is.False);
            var originalOrigin = MovementUndoOriginFacts.Capture(RiderCell, input.State, input.EnvironmentBindings, input.Souls);
            var changedOrigin = MovementUndoOriginFacts.Capture(RiderCell, projected.State, projected.EnvironmentBindings, projected.Souls);
            Assert.That(originalOrigin.TerrainSourceIds, Does.Contain(platform.Id));
            Assert.That(originalOrigin.SameValues(changedOrigin), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KnockbackChecksTheSupportAtTheNewCell(bool receivesSupport)
        {
            var start = new CellCoord(4, 3);
            var destination = new CellCoord(5, 3);
            var platform = Platform(100, start, destination, length: receivesSupport ? 2 : 1);
            var input = OnBoard(CliffFallTests.Knockback(), Terrain(1, start), Terrain(2, destination), platform);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var victim = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(2));
            Assert.That(victim.Health.IsDead, Is.EqualTo(!receivesSupport));
            var falls = result.Events.OfType<CombatFallEvent>().ToArray();
            if (receivesSupport)
            {
                Assert.That(victim.Anchor, Is.EqualTo(destination));
                Assert.That(falls, Is.Empty);
            }
            else
                Assert.That(falls.Single().TerrainSourceId, Is.EqualTo(2));
        }

        [Test]
        public void ForcedMovementImmunityDoesNotPreventFallingWhenSupportIsLost()
        {
            var seed = CliffFallTests.Knockback();
            var actor = seed.Characters.Single(c => c.EntityId == new EntityId(2));
            var immune = new UnitSpec(actor.CharacterKey, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100,
                MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 1, 0, 1, Array.Empty<TraitSpec>(),
                behavior: new UnitBehaviorSpec(true, true, true));
            var input = CombatCommandTestData.With(seed, profiles: seed.Profiles.Select(profile =>
                profile.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(immune, profile.Method) : profile).ToArray());
            var cell = actor.Anchor.Value;
            var ground = Terrain(1, cell);
            var platform = Platform(100, cell, new CellCoord(5, 3));
            var supported = OnBoard(input, ground, platform);
            var noLongerSupported = OnBoard(input, ground, new MovingPlatformFact(platform.Spec, new CellCoord(5, 3), 1));
            Assert.That(CombatCommandCalculator.RequiredProfile(noLongerSupported, actor).Definition.Behavior.IsForcedMovementImmune,
                Is.True);
            Assert.That(CliffFallCalculator.ResolveAt(CliffFallTests.Projection(supported), actor.EntityId, cell), Is.False);
            Assert.That(CliffFallCalculator.ResolveAt(CliffFallTests.Projection(noLongerSupported), actor.EntityId, cell), Is.True);
        }

        private static void ValidateLayout(params MovementTileFact[] sources)
        {
            var environment = new MovementEnvironmentFacts(sources);
            var bindings = Bind(environment);
            CellEnvironmentQuery.ValidateInitialEnvironment(8, 8, environment);
            CellEnvironmentQuery.ValidateInitialPlacement(8, 8, environment, NoStructures, NoCharacters, bindings,
                WallBlockingSpec.Default, Array.Empty<CellCoord>());
        }

        private static CombatCommandInput Deployment(CombatCommandInput input) =>
            new CombatCommandInput(1, CombatCommandKind.PlaceUnit, input.ActorId, default,
                input.State.With(turn: new CombatTurnFacts(StagePhase.Deployment)), input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, RiderCell,
                battle: new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0),
                environmentBindings: input.EnvironmentBindings);
    }
}
