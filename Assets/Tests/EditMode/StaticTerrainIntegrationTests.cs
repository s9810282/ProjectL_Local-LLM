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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class StaticTerrainIntegrationTests
    {
        [Test]
        public void SuccessiveReductions_AreNotAddedOrRoundedBetweenStages()
        {
            var result = TargetDamagePipeline.ApplyReceivedPercentages(TargetDamageBreakdown.CreatePassthrough(10), 80, 70);
            Assert.That(result.DamageAfterReceivedPercentages, Is.EqualTo(5.6m));
            Assert.That(result.FinalDamage, Is.EqualTo(5));
        }

        [TestCase(StaticTerrainKind.HighGround, 14)]
        [TestCase(StaticTerrainKind.Sandstorm, 16)]
        public void TerrainDefense_DirectAndEnvironmentalDamageAgree(StaticTerrainKind kind, int expectedDamage)
        {
            var input = Terrain(CombatCommandTestData.Input(method: new TargetingMethodSpec(
                TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)), Tile(71, new CellCoord(4, 3), kind));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var moved = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(expectedDamage));
            Assert.That(moved.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final.Health.CurrentHealth,
                Is.EqualTo(100 - expectedDamage));
            var environmentDamage = CombatDamageCalculator.Environment(input, 20,
                new CombatDamageOrigin(new CellCoord(1, 3), new EntityId(10)), new CellCoord(4, 3));
            Assert.That(environmentDamage.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [TestCase(false, 7)]
        [TestCase(true, 10)]
        public void HighGround_UsesPhysicalOriginInsteadOfCreditedCharacter(bool highOrigin, int expected)
        {
            var target = new CellCoord(2, 2);
            var source = new CellCoord(1, 1);
            var credit = new CellCoord(7, 7);
            var input = Terrain(CombatCommandTestData.Input(empty: true), Tile(71, target, StaticTerrainKind.HighGround),
                Tile(72, highOrigin ? source : credit, StaticTerrainKind.HighGround));
            var result = CombatDamageCalculator.Environment(input, 10,
                new CombatDamageOrigin(source, new EntityId(10)), target);
            Assert.That(result.FinalDamage, Is.EqualTo(expected));
        }

        [TestCase(StaticTerrainKind.HighGround, 10)]
        [TestCase(StaticTerrainKind.Sandstorm, 8)]
        public void OriginlessDamage_UsesApprovedTerrainPolicy(StaticTerrainKind kind, int expected)
        {
            var input = Terrain(CombatCommandTestData.Input(empty: true), Tile(71, CellCoord.Zero, kind));
            Assert.That(CombatDamageCalculator.Environment(input, 10, new CombatDamageOrigin(null, null),
                CellCoord.Zero).FinalDamage, Is.EqualTo(expected));
        }

        [Test]
        public void Sandstorm_QueryAndExecutionAgreeOnCurrentRange()
        {
            var input = Terrain(CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(5, 3))),
                Tile(71, new CellCoord(1, 3), StaticTerrainKind.Sandstorm));
            Assert.That(EnvironmentTerrainRules.AppointRange(input, input.Characters[0]), Is.EqualTo(3));
            Assert.That(input.Characters[0].Stats.AppointRange, Is.EqualTo(4));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).RangeCells.ToArray(), Has.No.Member(new CellCoord(5, 3)));
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void Sandstorm_AiUsesTerrainAtHypotheticalAttackPosition()
        {
            var input = Terrain(EnemyMovementPathTests.Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(3, 0)),
                Tile(71, new CellCoord(1, 0), StaticTerrainKind.Sandstorm, Owner.Enemy));
            input = CombatCommandTestData.With(input, characters: new[] {
                CombatCommandTestData.With(input.Characters[0], appointRange: 2), input.Characters[1], input.Characters[2] });
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(2));
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(2, 0)));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        public void Sandstorm_PreservesCurrentRangeContributions(int current, int expected)
        {
            var input = Terrain(CombatCommandTestData.Input(), Tile(71, new CellCoord(1, 3), StaticTerrainKind.Sandstorm));
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: current);
            Assert.That(EnvironmentTerrainRules.AppointRange(input, actor), Is.EqualTo(expected));
            Assert.That(actor.Stats.AppointRange, Is.EqualTo(current));
        }

        [Test]
        public void Preview_SandstormDefenseClassificationMustRemainCurrent()
        {
            var input = Terrain(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right),
                Tile(71, CellCoord.Right, StaticTerrainKind.Sandstorm));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures, new[] { new ThornFieldCellSnapshot(CellCoord.Right, 3, 10) },
                s.Timeline, s.Turn, s.Environment));
            input = Rebind(input, input.EnvironmentBindings);
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            var expected = MovementContextCalculator.Capture(input, Owner.Player, rules);
            Assert.That(expected.VisibleTerrainBindings.TerrainSourceIds, Is.EqualTo(new[] { 71L }));
            Assert.That(MovementContextCalculator.Matches(expected, Rebind(input, EnvironmentSemanticBindings.Empty),
                Owner.Player, rules), Is.False);
            var hidden = Terrain(input, Tile(71, CellCoord.Right, StaticTerrainKind.Sandstorm, Owner.Enemy));
            Assert.That(MovementContextCalculator.Capture(hidden, Owner.Player, rules).VisibleTerrainBindings.TerrainSourceIds, Is.Empty);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void ForcedMovement_OnlyExplicitKnockbackRejectsAscent(bool knockback, bool moved)
        {
            var input = Terrain(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right),
                Tile(71, new CellCoord(2, 0), StaticTerrainKind.HighGround));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(s.Environment.Tiles.Append(new PushFact()).ToArray())));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new PushRule(knockback) });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(moved ? new CellCoord(2, 0) : CellCoord.Right));
        }

        private sealed class PushFact : MovementTileFact
        {
            internal PushFact() : base(80, CellCoord.Right, "fixture-push", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => other is PushFact && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class PushRule : IMovementTileRule
        {
            private readonly bool knockback;
            internal PushRule(bool knockback) { this.knockback = knockback; }
            public string Key => "fixture-push";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                input.Phase == MovementEffectPhase.Recovery ? new MovementStepEffects() : new(forcedMovement:
                new[] { new MovementForcedMoveRequest(input.MoverId, Rotation4.R90, 1,
                    knockback ? MovementDisplacementCause.Knockback : MovementDisplacementCause.Unspecified) });
        }

        private static CombatCommandInput Rebind(CombatCommandInput input, EnvironmentSemanticBindings bindings) =>
            new(input.Number, input.Kind, input.ActorId, input.Aim, input.State, input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation,
                input.Battle, input.ProcessingKind, input.Path, input.MovementContext, 1, input.ArtifactItems, bindings);

        [Test]
        public void Sandstorm_AppliesAfterMatchupAndHeavy()
        {
            var input = Terrain(CombatCommandTestData.Input(), Tile(71, new CellCoord(4, 3), StaticTerrainKind.Sandstorm));
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateRider(false));
            var victim = CombatCommandTestData.With(input.Characters[2], category: CategorySnapshot.CreateHeavy(Rotation4.R270, false, 50));
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Rider), profiles[0].Method);
            profiles[1] = new CombatAttackProfile(CombatCommandTestData.Spec(victim.CharacterKey, 1, CategoryKey.Heavy), profiles[1].Method);
            input = CombatCommandTestData.With(input, profiles: profiles);
            var result = CombatDamageCalculator.Attack(input, new CombatEffectEngine(), new TargetDamagePipeline(
                new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), actor, victim, 20,
                AttackTargetRole.Primary, 100, actor.Anchor, null);
            Assert.That(result.DamageAfterMatchup, Is.EqualTo(14));
            Assert.That(result.IncomingReduction, Is.EqualTo(7));
            Assert.That(result.DamageAfterReceivedPercentages, Is.EqualTo(5.6m));
            Assert.That(result.FinalDamage, Is.EqualTo(5));
        }

        [Test]
        public void HiddenSandstorm_DoesNotLeakThroughPreview()
        {
            var input = Terrain(CombatCommandTestData.Input(method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0)), Tile(71, new CellCoord(4, 3), StaticTerrainKind.Sandstorm, Owner.Enemy));
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(20));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final.Health.CurrentHealth, Is.EqualTo(84));
        }

        [Test]
        public void Sandstorm_PierceQueriesAndExecutionUseReducedReach()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = Terrain(CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true),
                Tile(71, new CellCoord(1, 3), StaticTerrainKind.Sandstorm));
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var chars = new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy) };
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Cannon,
                new PiercingFalloffTraitSpec(30, 99)), method);
            input = CombatCommandTestData.With(input, chars, profiles: profiles);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).RangeCells.ToArray(), Has.No.Member(new CellCoord(5, 3)));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.Targets.Length, Is.EqualTo(2));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 20, 14 }));
        }

        [Test]
        public void SandstormTransit_UsesCurrentCellBeforeDestinationSettlement()
        {
            var input = Terrain(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0)),
                Tile(71, CellCoord.Right, StaticTerrainKind.Sandstorm));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures, new[] { new ThornFieldCellSnapshot(CellCoord.Right, 3, 10) },
                s.Timeline, s.Turn, s.Environment));
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            Assert.That(CombatMovementCalculator.Preview(input, rules).ProjectedActor.Health.CurrentHealth, Is.EqualTo(92));
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(92));
        }

        [Test]
        public void DepartureAndReturn_RecomputeRangeWithoutMutatingBaseStats()
        {
            var input = Terrain(MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right),
                Tile(71, CellCoord.Zero, StaticTerrainKind.Sandstorm));
            Assert.That(EnvironmentTerrainRules.AppointRange(input, input.Characters[0]), Is.EqualTo(3));
            var moved = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var after = MovementTestData.After(moved);
            Assert.That(EnvironmentTerrainRules.AppointRange(after, MovementTestData.Actor(moved)), Is.EqualTo(4));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(after), Array.Empty<IMovementTileRule>());
            var restored = MovementTestData.After(returned);
            Assert.That(EnvironmentTerrainRules.AppointRange(restored, MovementTestData.Actor(returned)), Is.EqualTo(3));
            Assert.That(MovementTestData.Actor(returned).Stats.AppointRange, Is.EqualTo(4));
        }

        [TestCase(false, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public void Knockback_LowToHighRejected_OtherEdgesAllowed(bool fromHigh, bool toHigh, bool allowed)
        {
            var input = CombatCommandTestData.Input(method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var chars = input.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20,
                CategoryKey.Base, new KnockbackTraitSpec(1)), profiles[0].Method);
            input = CombatCommandTestData.With(input, chars, profiles: profiles);
            input = Terrain(input, new[] { fromHigh ? Tile(71, new CellCoord(4, 3), StaticTerrainKind.HighGround) : null,
                toHigh ? Tile(72, new CellCoord(5, 3), StaticTerrainKind.HighGround) : null }.Where(t => t != null).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final.Anchor,
                Is.EqualTo(allowed ? new CellCoord(5, 3) : new CellCoord(4, 3)));
        }

        internal static StaticTerrainFact Tile(long id, CellCoord cell, StaticTerrainKind kind, Owner owner = Owner.Player) =>
            new(id, cell, kind, true, new[] { owner });

        internal static CombatCommandInput Terrain(CombatCommandInput input, params StaticTerrainFact[] terrain)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(terrain));
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell,
                input.Rotation, input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId,
                input.ArtifactItems, new EnvironmentSemanticBindings(terrain.Select(t => t.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()));
        }
    }
}
