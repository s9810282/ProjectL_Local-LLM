using System;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using static ProjectL.Tests.Core.WallStateLifecycleTests;

namespace ProjectL.Tests.Core
{
    public sealed class WallMovementTests
    {
        [Test]
        public void FlagDropCandidates_ExcludeWallCells()
        {
            var input = WithWalls(CombatCommandTestData.Input(empty: true), Wall(71, new CellCoord(2, 3)));
            var cells = FlagDropCalculator.NearestCells(input.State, input.Characters, input.State.Structures,
                input.Characters[0].Anchor.Value, input.EnvironmentBindings);
            Assert.That(cells, Has.No.Member(new CellCoord(2, 3)));
            Assert.That(cells, Is.EquivalentTo(new[] { new CellCoord(0, 3), new CellCoord(1, 2), new CellCoord(1, 4) }));
        }

        [Test]
        public void TornadoRelocation_ExcludesWallCells()
        {
            var input = WithWalls(CombatCommandTestData.Input(empty: true), Wall(71, new CellCoord(2, 3)));
            Assert.That(TornadoCalculator.UnitCandidates(input, input.Characters[0].Anchor.Value),
                Has.No.Member(new CellCoord(2, 3)));
        }

        [Test]
        public void WallCornerClearForAttack_StillOccupiedForMovement()
        {
            var input = WithWalls(MovementTestData.SafeRoute(5, 2, 1, new CellCoord(4, 6), new CellCoord(4, 5)),
                Wall(71, new CellCoord(4, 5)));
            var geometry = WallGeometryBuilder.Build(8, 8, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(3, 5), new CellCoord(4, 6), geometry.Runs.Single(), out _), Is.False);
            Assert.That(EnvironmentTerrainRules.IsWallOccupied(input.State.Environment, input.EnvironmentBindings, new CellCoord(4, 5)), Is.True);
            Assert.That(MovementRouteValidator.Validate(input, input.Path, out _), Is.EqualTo(MovementFailureReason.DestinationOccupied));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [Test]
        public void WallInMiddleOfRoute_RejectsWholeRouteWithoutSpendingAp()
        {
            var input = WithWalls(MovementTestData.SafeRoute(5, 3, 1, CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0)),
                Wall(71, CellCoord.Right));
            Assert.That(CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanMove, Is.False);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
        }

        [Test]
        public void AiPath_DetoursAroundWallAndUsesExecutableRoute()
        {
            var input = WithWalls(EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 1),
                targetCell: new CellCoord(4, 1), range: 6), Wall(71, new CellCoord(1, 1)));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.Cells, Has.No.Member(new CellCoord(1, 1)));
            var result = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path),
                Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        [Test]
        public void KnockbackIntoWall_DoesNotMove()
        {
            var input = CombatCommandTestData.Input(method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var chars = input.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20,
                CategoryKey.Base, new KnockbackTraitSpec(1)), profiles[0].Method);
            input = WithWalls(CombatCommandTestData.With(input, chars, profiles: profiles), Wall(71, new CellCoord(5, 3)));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var victim = result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final;
            Assert.That(victim.Anchor, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(80));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerDeploymentAndRedeployment_RejectWallCell(bool fromHand)
        {
            var source = WithWalls(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right), Wall(71, CellCoord.Right));
            if (fromHand)
                source = CombatCommandTestData.With(source, characters: source.Characters.Select(c => c.EntityId == source.ActorId
                    ? CombatPlacementCalculator.Location(c, CharacterRuntimeArea.Hand, 0, null, null) : c).ToArray());
            var s = source.State;
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                s.Structures, s.ThornFields, s.Timeline, new CombatTurnFacts(StagePhase.Deployment),
                s.Environment, s.EnvironmentProgress, s.WallBlocking);
            var battle = new CombatBattleFacts(8, 100, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 20, 0, null);
            var input = new CombatCommandInput(1, CombatCommandKind.PlaceUnit, source.ActorId, default, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                CellCoord.Right, Rotation4.R0, battle, environmentBindings: source.EnvironmentBindings);
            Assert.That(CombatPlacementCalculator.Preview(input).CanPlace, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyRedeployment_ExcludesWallsAndRejectsWhenNoCellRemains(bool fullRow)
        {
            var walls = (fullRow ? Enumerable.Range(0, 8) : new[] { 2 }).Select(x => Wall(71 + x, new CellCoord(x, 7))).ToArray();
            var source = WithWalls(CombatCommandTestData.Input(empty: true), walls);
            var enemy = CombatPlacementCalculator.Location(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 7), Owner.Enemy),
                CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var characters = source.Characters.Select(c => c.OwnershipRole == CharacterOwnershipRole.Main
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 4)) : c).Append(enemy).ToArray();
            var s = source.State;
            var state = new CombatCommandState(8, 8, s.Phase, s.ActiveActorId, false, s.Structures, s.ThornFields, s.Timeline,
                new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, new CellCoord(2, 7), Rotation4.R0) }),
                s.Environment, s.EnvironmentProgress, s.WallBlocking);
            var battle = new CombatBattleFacts(8, 100, 42, 2, 1, Array.Empty<CharacterInstanceId>(), 0, 20, 0, null);
            var input = new CombatCommandInput(1, CombatCommandKind.EnterCombat, source.ActorId, default, state, characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: battle, environmentBindings: source.EnvironmentBindings);
            var projected = characters.ToDictionary(c => c.EntityId);
            var projection = MovementProjection.ForArrival(input, projected, new System.Collections.Generic.List<CombatEvent>());
            Assert.That(CombatPlacementCalculator.RedeployEnemies(input, projected, projection, Array.Empty<IMovementTileRule>()), Is.EqualTo(!fullRow));
            Assert.That(projected[enemy.EntityId].Anchor, Is.EqualTo(fullRow ? (CellCoord?)null : new CellCoord(1, 7)));
        }

        [TestCase(MovementDisplacementCause.Knockback)]
        [TestCase(MovementDisplacementCause.Unspecified)]
        public void EnvironmentalForcedMovement_CannotEnterWall(MovementDisplacementCause cause)
        {
            var input = WithWalls(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right), Wall(71, new CellCoord(2, 0)));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase,
                s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(s.Environment.Tiles.Append(new PushFact()).ToArray()), wallBlocking: s.WallBlocking));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new PushRule(cause) });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(CellCoord.Right));
        }

        private sealed class PushFact : MovementTileFact
        {
            internal PushFact() : base(80, CellCoord.Right, "wall-test-push", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => other is PushFact && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class PushRule : IMovementTileRule
        {
            private readonly MovementDisplacementCause cause;
            internal PushRule(MovementDisplacementCause cause) { this.cause = cause; }
            public string Key => "wall-test-push";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                input.Phase == MovementEffectPhase.Recovery ? new MovementStepEffects() : new(forcedMovement:
                    new[] { new MovementForcedMoveRequest(input.MoverId, Rotation4.R90, 1, cause) });
        }

        internal static CombatCommandInput WithWalls(CombatCommandInput input, params StaticTerrainFact[] walls)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(walls),
                wallBlocking: new WallBlockingSpec(257));
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell,
                input.Rotation, input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId,
                input.ArtifactItems, new EnvironmentSemanticBindings(walls.Select(w => w.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()),
                input.ObstacleContents);
        }
    }
}
