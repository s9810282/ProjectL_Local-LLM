using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class EnvironmentTerrainRuleTests
    {
        /// <summary>고지대 Wall 무시 설계 D6: 피해 감소는 저지대 → 고지대만 70%이고 고지대에서 쏘면 감소가 없다.</summary>
        [TestCase(false, true, 70)]
        [TestCase(true, true, 100)]
        [TestCase(true, false, 100)]
        [TestCase(false, false, 100)]
        public void HighGroundDamage_ReducesOnlyLowToHigh(bool fromHigh, bool toHigh, int expected)
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                Terrain(11, CellCoord.Zero, active: fromHigh),
                Terrain(12, CellCoord.Right, active: toHigh)
            });
            Assert.That(EnvironmentTerrainRules.RemainingDamagePercent(environment, Bindings(11, 12),
                CellCoord.Zero, CellCoord.Right), Is.EqualTo(expected));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(4, 3)]
        public void SandstormRange_PreservesZero(int input, int expected)
        {
            Assert.That(EnvironmentTerrainRules.SandstormRange(input), Is.EqualTo(expected));
        }

        [Test]
        public void TerrainFacts_IgnoreInactiveOrUnclassifiedSources()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                Terrain(11, CellCoord.Zero, active: false),
                Terrain(12, CellCoord.Right),
                Terrain(13, CellCoord.Up, StaticTerrainKind.Sandstorm),
                CellEnvironmentTestData.Terrain(14, new CellCoord(2, 0))
            });
            var bindings = Bindings(11, 13, 14);
            Assert.That(EnvironmentTerrainRules.HasTerrain(environment, bindings,
                CellCoord.Zero, StaticTerrainKind.HighGround), Is.False);
            Assert.That(EnvironmentTerrainRules.HasTerrain(environment, bindings,
                CellCoord.Right, StaticTerrainKind.HighGround), Is.False);
            Assert.That(EnvironmentTerrainRules.HasTerrain(environment, bindings,
                CellCoord.Up, StaticTerrainKind.HighGround), Is.False);
            Assert.That(EnvironmentTerrainRules.HasTerrain(environment, bindings,
                CellCoord.Up, StaticTerrainKind.Sandstorm), Is.True);
            Assert.That(EnvironmentTerrainRules.HasTerrain(environment, bindings,
                new CellCoord(2, 0), StaticTerrainKind.HighGround), Is.False);
        }

        [Test]
        public void VisibilityCopy_PreservesTerrainKind()
        {
            var source = new StaticTerrainFact(11, CellCoord.Right, StaticTerrainKind.Sandstorm,
                true, new[] { Owner.Enemy, Owner.Player });
            var hidden = new StaticTerrainFact(12, CellCoord.Up, StaticTerrainKind.HighGround,
                true, new[] { Owner.Enemy });
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { source, hidden });
            var visible = environment.VisibleTo(Owner.Player);
            var copy = (StaticTerrainFact)visible.Tiles.Single();
            Assert.That(copy.Kind, Is.EqualTo(StaticTerrainKind.Sandstorm));
            Assert.That(copy.Id, Is.EqualTo(11));
            Assert.That(copy.Cell, Is.EqualTo(CellCoord.Right));
            Assert.That(copy.Active, Is.True);
            Assert.That(copy.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(EnvironmentTerrainRules.HasTerrain(visible, Bindings(11, 12),
                CellCoord.Right, StaticTerrainKind.Sandstorm), Is.True);
            Assert.That(EnvironmentTerrainRules.HasTerrain(visible, Bindings(11, 12),
                CellCoord.Up, StaticTerrainKind.HighGround), Is.False);
            Assert.That(source.SameValues(source.WithKnownTo(new[] { Owner.Player, Owner.Enemy })), Is.True);
            Assert.That(source.SameValues(Terrain(11, CellCoord.Right)), Is.False);
        }

        [Test]
        public void LocalRules_RejectNegativeInputsWithoutAddingRetiredSurcharge()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnvironmentTerrainRules.SandstormRange(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Terrain(11, CellCoord.Zero, (StaticTerrainKind)99));
        }

        [Test]
        public void CommandCopies_PreserveCapturedBindings()
        {
            var bindings = Bindings(11);
            var input = WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right),
                MovementEnvironmentFacts.Empty, bindings);
            var intent = CombatCommandComposition.Intent(input, CombatCommandKind.Move);
            var aimed = CombatAttackQueries.WithAim(input, AttackAim.ForCell(CellCoord.Up));
            var projected = new MovementProjection(input).CurrentInput();
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var after = CombatCommandComposition.Project(move);
            var merged = CombatCommandComposition.Merge(input, new[] { move });
            foreach (var copied in new[] { intent, aimed, projected, after, merged.Input })
                Assert.That(copied.EnvironmentBindings, Is.SameAs(bindings));
            Assert.That(MovementTestData.Route(after, CellCoord.Right, CellCoord.Up + CellCoord.Right)
                .EnvironmentBindings, Is.SameAs(bindings));
            Assert.That(CombatCommandTestData.Input().EnvironmentBindings, Is.SameAs(EnvironmentSemanticBindings.Empty));
        }

        [Test]
        public void StaticTerrain_IsNotDispatchedAsArrivalRuleOrConsumed()
        {
            var terrain = Terrain(11, CellCoord.Right);
            var input = WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right),
                new MovementEnvironmentFacts(new MovementTileFact[] { terrain }), Bindings(11));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(MovementTestData.Actor(move).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(move.FinalState.Environment.Tiles.Single().SameValues(terrain), Is.True);
        }

        private static StaticTerrainFact Terrain(long id, CellCoord cell,
            StaticTerrainKind kind = StaticTerrainKind.HighGround, bool active = true) =>
            new(id, cell, kind, active, new[] { Owner.Player });

        private static EnvironmentSemanticBindings Bindings(params long[] ids) =>
            new(ids, Array.Empty<long>(), Array.Empty<EntityId>());

        private static CombatCommandInput WithEnvironment(CombatCommandInput input,
            MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.IsNight,s.Structures,s.ThornFields,s.Turn,environment);
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state,
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls,
                input.Artifacts, input.TargetCell, input.Rotation, input.Battle, input.ProcessingKind,
                input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems, bindings,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);
        }
    }
}
