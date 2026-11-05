using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class TraitTrailTests
    {
        [Test]
        public void HandUnitAvailability_PreservesNormalFailure()
        {
            var input = CombatCommandTestData.Input(empty: true);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()) : c).ToArray());
            var availability = CombatCommandCalculator.GetActionAvailability(input);
            Assert.That(availability.MoveFailure, Is.EqualTo(MovementFailureReason.NotInCombat));
            Assert.That(availability.CanMove, Is.False);
        }

        [Test]
        public void EnemyVoluntaryMove_EmitsBothPublicTrailKinds()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(4, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1));
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            var trails = TraitTrailTestData.Trails(result.FinalState.Environment);
            // 흔적은 지나간 칸(출발·도착 제외)에만 두 종류씩 생긴다.
            Assert.That(trails, Is.Not.Empty);
            Assert.That(trails.Length, Is.EqualTo((result.PathMovement.DistanceSpent - 1) * 2));
            Assert.That(trails.All(t => t.KnownTo.Contains(Owner.Player) && t.KnownTo.Contains(Owner.Enemy)), Is.True);
        }

        [Test]
        public void TrailsOnlyOnPassedCells_PreviewDoesNotAllocate()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1));
            var progress = input.State.EnvironmentProgress;
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(input.State.Environment.Tiles, Is.Empty);
            Assert.That(input.State.EnvironmentProgress.SameValues(progress), Is.True);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var trails = TraitTrailTestData.Trails(result.FinalState.Environment);
            // 출발 (0,0)·도착 (2,0)에는 없고 지나간 (1,0)에만 두 종류가 생긴다.
            Assert.That(trails.Length, Is.EqualTo(2));
            Assert.That(trails.Select(t => t.Cell), Is.All.EqualTo(CellCoord.Right));
            Assert.That(trails.Select(t => ((TraitTrailFact)t).Key), Is.EquivalentTo(new[] { TraitKey.GroundCompaction, TraitKey.DustScreen }));
            Assert.That(trails.Select(t => t.Id).OrderBy(id => id), Is.EqualTo(Enumerable.Range(0, 2).Select(i => progress.NextSourceId + i)));
            Assert.That(result.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(progress.NextSourceId + 2));
            Assert.That(result.FinalState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(progress.NextObjectEntryOrdinal));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(progress.RandomState));
        }

        [Test]
        public void PreservesUnderlyingTerrain_AndPublishesBothObservers()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new GroundCompactionTraitSpec(1, 1));
            var terrain = new StaticTerrainFact(70, CellCoord.Right, StaticTerrainKind.HighGround, true, new[] { Owner.Player, Owner.Enemy });
            input = TraitTrailTestData.Environment(input, terrain);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == terrain.Id).SameValues(terrain), Is.True);
            foreach (var side in new[] { Owner.Player, Owner.Enemy })
                Assert.That(TraitTrailTestData.Trails(result.FinalState.Environment.VisibleTo(side)).Select(t => t.Cell),
                    Is.EquivalentTo(new[] { CellCoord.Right }));
        }

        [TestCase(Owner.Player, 1)]
        [TestCase(Owner.Player, 2)]
        [TestCase(Owner.Player, 3)]
        [TestCase(Owner.Enemy, 3)]
        public void Compaction_ExpiresAtNextCreatingSideEnd_WhileDustExpiresAtOpposingEnd(Owner side, int bonus)
        {
            bool player = side == Owner.Player;
            var input = player ? MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0)) :
                EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(4, 1));
            input = TraitCombatFixtures.Equip(input, player ? EntityKey.Unit_Akimbo : EntityKey.Unit_Demon,
                new GroundCompactionTraitSpec(bonus, 1), new DustScreenTraitSpec(1, 20, 1));
            var move = player ? CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()) :
                CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            // 흔적은 지나간 첫 칸에 있다(출발·도착 칸에는 없음).
            Assert.That(move.PathMovement.DistanceSpent, Is.GreaterThan(1));
            var passed = move.PathMovement.EnteredCells.First();
            var current = MovementTestData.After(move);
            long sequence = current.State.Turn.ControllerTurnSequence;
            for (int boundary = 0; boundary < 3; boundary++)
            {
                bool endsPlayer = boundary == 1 ? !player : player;
                var endInput = EnemyControllerPhaseTests.Input(current,
                    endsPlayer ? CombatControllerPhase.Player : CombatControllerPhase.Enemy, sequence: sequence + boundary);
                var result = endsPlayer ? CombatTurnCalculator.EndPlayerTurn(endInput, Array.Empty<IMovementTileRule>()) :
                    CombatTurnCalculator.EndEnemyPhase(endInput, Array.Empty<IMovementTileRule>());
                Assert.That(result.Accepted, Is.True, result.Reason);
                current = MovementTestData.After(result);
                Assert.That(TraitTrailTestData.Bonus(current, passed), Is.EqualTo(boundary < 2 ? bonus : 0));
                Assert.That(TraitTrailCalculator.RangeReductionAt(passed, current.State.Environment), Is.EqualTo(boundary == 0 ? 1 : 0));
            }
        }

        [Test]
        public void ExpiringStrongerCompaction_RevealsRemainingWeakerSource()
        {
            var input = TraitTrailTestData.Environment(MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right),
                TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1, lifetime: 2),
                TraitTrailTestData.Trail(11, CellCoord.Zero, TraitKey.GroundCompaction, movement: 2, source: 6));
            Assert.That(TraitTrailTestData.Bonus(input, CellCoord.Zero), Is.EqualTo(2));
            var ended = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(TraitTrailTestData.Bonus(MovementTestData.After(ended), CellCoord.Zero), Is.EqualTo(1));
            Assert.That(ended.FinalState.Environment.Tiles.Single(t => t.Id == 11).Active, Is.False);
        }

        [TestCase(1, Owner.Player)]
        [TestCase(2, Owner.Enemy)]
        [TestCase(3, Owner.Enemy)]
        public void SameTrailDoesNotStack_AndAllowanceIsCapturedAtStart(int bonus, Owner creatingSide)
        {
            int maximum = 3 + bonus;
            var cells = Enumerable.Range(0, maximum + 1).Select(x => new CellCoord(x, 0)).ToArray();
            var input = MovementTestData.SafeRoute(cells);
            input = TraitTrailTestData.Environment(input,
                TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: bonus, owner: creatingSide),
                TraitTrailTestData.Trail(11, CellCoord.Zero, TraitKey.GroundCompaction, movement: bonus, source: 6),
                TraitTrailTestData.Trail(12, CellCoord.Right, TraitKey.GroundCompaction, movement: 4, source: 7));
            Assert.That(TraitTrailTestData.Bonus(input, CellCoord.Zero), Is.EqualTo(bonus));
            var outside = new CellCoord(maximum + 1, 0);
            var destinations = CombatMovementCalculator.Range(input).Destinations.ToArray().Select(d => d.TargetAnchor);
            Assert.That(destinations, Has.Member(cells.Last()).And.No.Member(outside));
            Assert.That(CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanMove, Is.True);
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.True);
            var tooFar = MovementTestData.Route(input, cells.Append(outside).ToArray());
            Assert.That(CombatMovementCalculator.Preview(tooFar, Array.Empty<IMovementTileRule>()).CanMove, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(tooFar, Array.Empty<IMovementTileRule>()).Reason, Is.EqualTo("MoveRangeExceeded"));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, 80)]
        [TestCase(CombatDamageKind.ExtraAttack, 80)]
        [TestCase(CombatDamageKind.ReactionAttack, 80)]
        [TestCase(CombatDamageKind.EnvironmentDamage, 100)]
        [TestCase(CombatDamageKind.ReflectedDamage, 100)]
        public void Dust_OnlyReducesAttackDamage(CombatDamageKind kind, int expected)
        {
            var input = CombatCommandTestData.Input();
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            input = TraitTrailTestData.Environment(input,
                TraitTrailTestData.Trail(10, victim.Anchor.Value, TraitKey.DustScreen, range: 3, percent: 20),
                TraitTrailTestData.Trail(11, victim.Anchor.Value, TraitKey.DustScreen, range: 1, percent: 10, source: 6));
            Assert.That(TraitTrailTestData.Damage(input, new EntityId(1), victim.EntityId, kind), Is.EqualTo(expected));
        }

        [Test]
        public void Dust_RangeFloorsAtZero_AndDifferentReductionsMultiply()
        {
            var input = TraitDamageReductionTests.Input();
            var victim = input.Characters.Single(c => c.EntityId.Value == 5);
            input = TraitTrailTestData.Environment(input, TraitTrailTestData.Trail(10, victim.Anchor.Value, TraitKey.DustScreen, range: 9, percent: 20));
            Assert.That(TraitTrailTestData.Damage(input, new EntityId(2), victim.EntityId, CombatDamageKind.OrdinaryAttack), Is.EqualTo(40));
            var ranged = input.WithActor(victim.EntityId);
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(ranged).RangeCells.Length, Is.Zero);
        }

        [Test]
        public void TrailIdentityAndLifetime_CannotBeRewrittenOrReactivated()
        {
            var original = TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1);
            Assert.That(original.SuccessorFailure(TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 2)), Is.Not.Null);
            var expired = TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1, lifetime: 0);
            Assert.That(expired.SuccessorFailure(original), Is.Not.Null);
        }
    }

    internal static class TraitTrailTestData
    {
        internal static MovementTileFact[] Trails(MovementEnvironmentFacts environment) => environment.Tiles.Where(t => t.GetType().Name == "TraitTrailFact").ToArray();

        internal static MovementTileFact Trail(long id, CellCoord cell, TraitKey key, int movement = 0, int range = 0, int percent = 0,
            int lifetime = 1, int source = 1, Owner owner = Owner.Player)
        {
            var type = typeof(MovementEnvironmentFacts).Assembly.GetType(typeof(MovementEnvironmentFacts).Namespace + ".TraitTrailFact");
            Assert.That(type, Is.Not.Null, "Typed trail sources must use the existing environment.");
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, source, (int)key, "main");
            return (MovementTileFact)Activator.CreateInstance(type, new object[] { id, cell, origin, owner, movement, range, percent, lifetime, lifetime > 0, new[] { Owner.Player, Owner.Enemy } });
        }

        internal static CombatCommandInput Environment(CombatCommandInput input, params MovementTileFact[] tiles)
        {
            var environment = new MovementEnvironmentFacts(tiles);
            return TornadoEnvironmentTests.WithStorms(input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls,
                new EnvironmentSemanticBindings(tiles.OfType<StaticTerrainFact>().Select(t => t.Id).ToArray(),
                    tiles.OfType<ObjectSourceFact>().Select(t => t.Id).ToArray(), input.EnvironmentBindings.ObstacleEntityIds)));
        }

        internal static int Bonus(CombatCommandInput input, CellCoord cell)
        {
            var type = typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".TraitTrailCalculator");
            Assert.That(type, Is.Not.Null);
            return (int)type.GetMethod("MovementBonusAt", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { cell, input.State.Environment });
        }

        internal static int Damage(CombatCommandInput input, EntityId actorId, EntityId target, CombatDamageKind kind)
        {
            var actor = input.Characters.Single(c => c.EntityId == actorId);
            var victim = input.Characters.Single(c => c.EntityId == target);
            int amount = 100;
            if (kind == CombatDamageKind.OrdinaryAttack || kind == CombatDamageKind.ExtraAttack || kind == CombatDamageKind.ReactionAttack)
                amount = CombatDamageCalculator.Attack(input, new CombatEffectEngine(), new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                    actor, victim, amount, AttackTargetRole.Primary, 100, actor.Anchor, null, damageKind: kind).FinalDamage;
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            return CombatDamageApplication.For(input, characters, new List<CombatEvent>()).Apply(target, kind,
                new CombatDamageOrigin(actor.Anchor, actorId, 1), amount).ActualHealthLoss;
        }
    }
}
