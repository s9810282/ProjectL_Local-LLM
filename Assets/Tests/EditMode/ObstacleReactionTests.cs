using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class ObstacleReactionTests
    {
        [Test]
        public void PierceAndBlast_PreserveRaySubtotalAndCombinedDamage()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 0), 100, 7,
                new[] { CellCoord.Zero, new CellCoord(0, 3), new CellCoord(0, 6) }),
                Barrel(52, new CellCoord(3, 3), 10, 7));
            var chars = input.Characters.Where(c => c.EntityId == input.ActorId || c.EntityId == new EntityId(10)).ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(chars[0].CharacterKey, 20,
                CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99)),
                new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0));
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, chars, profiles: profiles),
                AttackAim.ForDirection(AttackDirection.Right));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var target = preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(51));
            Assert.That(target.RayHits.Length, Is.EqualTo(2));
            Assert.That(target.RayHits.ToArray().Sum(h => h.FinalDamage), Is.EqualTo(40));
            Assert.That(target.ExpectedDamage, Is.EqualTo(47));
            Assert.That(target.SecondaryDamage, Is.EqualTo(7));
            Assert.That(target.TargetHealthAfter, Is.EqualTo(53));
            Assert.That(result.FinalState.Structures.Single(s => s.EntityId == target.TargetId).Health.CurrentHealth,
                Is.EqualTo(target.TargetHealthAfter));
        }

        [Test]
        public void Ai_SelectsBarrelWhenOnlyBlastCanReachHostileUnit()
        {
            var barrel = Barrel(51, new CellCoord(2, 1), 1, 7);
            var input = Copy(EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(3, 1)),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(barrel.Structure.Anchor));
            var result = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final.Health.CurrentHealth,
                Is.EqualTo(93));
        }

        [Test]
        public void Ai_AvoidsBlastOnlyFriendlyVictimWhenSafeAttackExists()
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.Radial, 100), empty: true);
            var barrel = Barrel(51, new CellCoord(4, 4), 10, 7);
            input = Copy(input, characters: input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(1, 5), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Akimbo, new CellCoord(5, 5), Owner.Player) }).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            var result = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.False);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Final.EntityId == new EntityId(4) &&
                e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth), Is.False);
        }

        [Test]
        public void EnvironmentalStructureDamage_ResolvesBarrelInCurrentMovementProjection()
        {
            var route = MovementTestData.SafeRoute(8, 2, 1, CellCoord.Zero, CellCoord.Right);
            var barrel = Barrel(51, new CellCoord(2, 0), 1, 7);
            var input = Copy(route, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(new MovementTileFact[] { new BarrelTriggerFact() })));
            var rules = new IMovementTileRule[] { new BarrelTriggerRule() };
            Assert.That(CombatMovementCalculator.Preview(input, rules).ProjectedActor.Health.CurrentHealth, Is.EqualTo(93));
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Count(), Is.EqualTo(1));
            var expected = MovementContextCalculator.Capture(input, Owner.Player, rules);
            var changed = Copy(input, profiles: new[] { new ObstacleContentProfile(new EntityId(51), ObstacleContentKind.PowderBarrel, 9) });
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, rules), Is.False);
        }

        private sealed class BarrelTriggerFact : MovementTileFact
        {
            internal BarrelTriggerFact() : base(80, CellCoord.Right, "fixture-barrel-trigger", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => other is BarrelTriggerFact && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
        }
        private sealed class BarrelTriggerRule : IMovementTileRule
        {
            public string Key => "fixture-barrel-trigger";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { new EntityId(51) };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Recovery
                ? new MovementStepEffects() : new MovementStepEffects(damage: new[] {
                    new MovementHealthRequest(new EntityId(51), 1, new CombatDamageOrigin(input.EnteredCell, input.MoverId)) });
        }

        [Test]
        public void Pierce_ReactPerHitAndDoNotReactAfterDestruction()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 0), 30, 7,
                new[] { CellCoord.Zero, new CellCoord(0, 3), new CellCoord(0, 6) }));
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var chars = input.Characters.Where(c => c.EntityId == input.ActorId || c.EntityId == new EntityId(10)).ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(chars[0].CharacterKey, 20,
                CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99)), method);
            input = CombatCommandTestData.With(input, chars, profiles: profiles);
            input = CombatAttackQueries.WithAim(input, AttackAim.ForDirection(AttackDirection.Right));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Select(e => e.Reaction),
                Is.EqualTo(new[] { ObstacleReactionKind.Hit, ObstacleReactionKind.Destroyed }));
            Assert.That(result.Events.OfType<CombatStructureEvent>().Where(e => e.Final.EntityId == new EntityId(51))
                .Select(e => e.Final.Health.CurrentHealth), Is.EqualTo(new[] { 10, 0 }));
            var projected = CombatCommandComposition.Project(result);
            foreach (var cell in result.FinalState.Structures.Single().OccupiedCells)
                Assert.That(CellEnvironmentQuery.Read(result.FinalState, projected.Characters,
                    input.EnvironmentBindings, cell).ObstacleIds, Is.Empty);
        }

        [Test]
        public void Blast_MultipleFootprintContactsDamageOnceAndReleaseFullFootprint()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7), Barrel(52, new CellCoord(5, 2), 100, 7,
                new[] { CellCoord.Zero, new CellCoord(0, 1), new CellCoord(0, 2) }));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId == input.ActorId || c.EntityId == new EntityId(10)).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Structures.Single(s => s.EntityId == new EntityId(52)).Health.CurrentHealth, Is.EqualTo(93));
            var dead = result.FinalState.Structures.Single(s => s.EntityId == new EntityId(51));
            Assert.That(dead.Anchor, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(dead.OccupiedCells, Is.EqualTo(input.State.Structures[0].OccupiedCells));
            Assert.That(result.Events.OfType<CombatStructureEvent>().Single(e => e.Final.EntityId == dead.EntityId).Final.Health.IsDead, Is.True);
            Assert.That(result.Field.Final.Entries, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DirectQueries_IncludeCommonObstacleWithoutHostility(bool enemy)
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7));
            if (enemy)
            {
                var barrel = Barrel(51, new CellCoord(2, 1), 10, 7);
                input = Copy(EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(3, 1)),
                    structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
                input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(2, 1)));
            }
            Assert.That(CombatCommandCalculator.GetAttackRange(input).InRangeTargetIds.ToArray(), Does.Contain(new EntityId(51)));
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
        }

        [Test]
        public void ZeroDamage_DoesNotEmitReactionOrHealthEvent()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var structures = input.State.Structures.ToDictionary(s => s.EntityId);
            var events = new System.Collections.Generic.List<CombatEvent>();
            var calculator = new ObstacleReactionCalculator(input, input.State.Environment, characters, structures, events,
                new System.Collections.Generic.Dictionary<EntityId, CellCoord>());
            calculator.DamageStructure(new EntityId(51), 0, new CombatDamageOrigin(input.Characters[0].Anchor, input.ActorId));
            calculator.ResolveExplosions();
            Assert.That(events, Is.Empty);
            Assert.That(structures[new EntityId(51)].Health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void Blast_LargeCharacterReceivesOneDamageAndKeepsItsPose()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var c = characters[new EntityId(3)];
            var cells = new[] { new CellCoord(4, 4), new CellCoord(5, 4) };
            characters[c.EntityId] = new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, c.Health, c.Stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, cells, c.MovementCheckpointAnchor, 0, 0);
            var structures = input.State.Structures.ToDictionary(s => s.EntityId);
            var events = new System.Collections.Generic.List<CombatEvent>();
            var calculator = new ObstacleReactionCalculator(input, input.State.Environment, characters, structures, events,
                new System.Collections.Generic.Dictionary<EntityId, CellCoord>());
            calculator.DamageStructure(new EntityId(51), 10, new CombatDamageOrigin(input.Characters[0].Anchor, input.ActorId));
            calculator.ResolveExplosions();
            Assert.That(characters[c.EntityId].Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(characters[c.EntityId].OccupiedCells, Is.EqualTo(cells));
            Assert.That(events.OfType<CombatCharacterEvent>().Count(e => e.Final.EntityId == c.EntityId), Is.EqualTo(1));
        }

        [Test]
        public void UnchangedObstacleProfile_CopiesDoNotLoseDefinition()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 100, 7));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var projected = CombatCommandComposition.Project(result);
            Assert.That(projected.ObstacleContents.Single().SameValues(input.ObstacleContents.Single()), Is.True);
            Assert.That(projected.State.Structures.Single().Health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void Explosion_UsesOwnOriginForTerrainAndLeavesSourcesUnchanged()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 10));
            var s = input.State;
            var terrain = new StaticTerrainFact(71, new CellCoord(5, 3), StaticTerrainKind.HighGround, true, new[] { Owner.Player });
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(new MovementTileFact[] { terrain }));
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                environmentBindings: new EnvironmentSemanticBindings(new[] { 71L }, Array.Empty<long>(), input.EnvironmentBindings.ObstacleEntityIds),
                obstacleContents: input.ObstacleContents);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(4)).Final.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.FinalState.Environment.SameValues(state.Environment), Is.True);
            var damage = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId == new EntityId(4));
            Assert.That(damage.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(damage.DamageOrigins.Single().AttackGroup, Is.GreaterThan(1));
        }

        [Test]
        public void Barrel_HitsAllFactionsAndStructures()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7));
            var chars = input.Characters.Append(CombatCommandTestData.Character(12, EntityKey.Unit_Akimbo,
                new CellCoord(3, 3), Owner.Player)).ToArray();
            input = Copy(input, characters: chars, structures: input.State.Structures.Append(
                new StructureCombatFacts(new EntityId(60), new StructureSpec(EntityKey.Structure_Headquarters,
                    1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                    new CellCoord(5, 4), Rotation4.R0, new[] { new CellCoord(5, 4) })).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(12)).Final.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.FinalState.Structures.Single(s => s.EntityId == new EntityId(60)).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single().Reaction, Is.EqualTo(ObstacleReactionKind.Destroyed));
        }

        [Test]
        public void DiamondChain_ExplodesEachBarrelOnce()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 20), Barrel(52, new CellCoord(5, 2), 10, 20),
                Barrel(53, new CellCoord(5, 4), 10, 20), Barrel(54, new CellCoord(6, 3), 10, 20));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.OfType<CombatObstacleReactionEvent>().Where(e => e.Reaction == ObstacleReactionKind.Destroyed).ToArray();
            Assert.That(events.Select(e => e.ObstacleId), Is.EqualTo(new[] { new EntityId(51), new EntityId(52), new EntityId(53), new EntityId(54) }));
            Assert.That(events.Select(e => e.AttackGroup).Distinct().Count(), Is.EqualTo(4));
            Assert.That(events.All(e => e.Origin.CreditedCharacterId == input.ActorId), Is.True);
            Assert.That(events.Select(e => e.Origin.PhysicalCell), Is.EqualTo(input.State.Structures.Select(s => (CellCoord?)s.Anchor)));
            Assert.That(result.FinalState.Structures.All(s => s.Health.IsDead), Is.True);
            Assert.That(result.Field.Final.Entries, Is.Empty);
        }

        [Test]
        public void NonlethalBarrel_DoesNotExplode()
        {
            var result = CombatCommandCalculator.Calculate(Barrels(Barrel(51, new CellCoord(4, 3), 100, 7)), Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single().Reaction, Is.EqualTo(ObstacleReactionKind.Hit));
            Assert.That(result.Characters.Characters.Select(c => c.Final.EntityId), Has.No.Member(new EntityId(2)));
        }

        [Test]
        public void ObstacleProfiles_SurviveCopiesAndAreImmutable()
        {
            var input = Barrels(Barrel(51, new CellCoord(4, 3), 10, 7));
            var copy = CombatAttackQueries.WithAim(input, input.Aim);
            Assert.That(copy.ObstacleContents.Single().SameValues(input.ObstacleContents.Single()), Is.True);
            Assert.That(CombatCommandComposition.Intent(input, input.Kind, input.Aim).ObstacleContents.Count, Is.EqualTo(1));
        }

        internal static (StructureCombatFacts Structure, ObstacleContentProfile Profile) Barrel(int id, CellCoord cell, int hp, int explosion,
            CellCoord[] localCells = null)
        {
            localCells ??= new[] { CellCoord.Zero };
            var definition = new StructureSpec(EntityKey.Structure_PowderBarrel, localCells.Max(c => c.X) + 1,
                localCells.Max(c => c.Y) + 1, localCells, hp, false, 0);
            return (new StructureCombatFacts(new EntityId(id), definition, new HealthStateSnapshot(hp, hp), cell,
                Rotation4.R0, localCells.Select(c => c + cell).ToArray(), Owner.Neutral),
                new ObstacleContentProfile(new EntityId(id), ObstacleContentKind.PowderBarrel, explosion));
        }

        internal static CombatCommandInput Barrels(params (StructureCombatFacts Structure, ObstacleContentProfile Profile)[] barrels)
        {
            var input = CombatCommandTestData.Input(method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId != new EntityId(2)).ToArray());
            return Copy(input, structures: barrels.Select(b => b.Structure).ToArray(), profiles: barrels.Select(b => b.Profile).ToArray());
        }

        internal static CombatCommandInput Copy(CombatCommandInput input, CharacterCombatFacts[] characters = null,
            StructureCombatFacts[] structures = null, ObstacleContentProfile[] profiles = null)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                structures ?? s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment);
            profiles ??= input.ObstacleContents.ToArray();
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state, characters ?? input.Characters.ToArray(),
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation,
                input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId == 0 ? 1 : input.BattleInstanceId, input.ArtifactItems,
                new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds, input.EnvironmentBindings.ObjectSourceIds,
                    profiles.Select(p => p.EntityId).ToArray()), profiles);
        }

        [Test]
        public void LethalUnit_EmitsDestructionOnly() =>
            Assert.That(ObstacleReactionCalculator.Select(10, 0), Is.EqualTo(ObstacleReactionKind.Destroyed));

        [Test]
        public void NonlethalUnit_EmitsHitOnly() =>
            Assert.That(ObstacleReactionCalculator.Select(10, 3), Is.EqualTo(ObstacleReactionKind.Hit));

        [Test]
        public void AlreadyDestroyed_DoesNotReact() =>
            Assert.That(ObstacleReactionCalculator.Select(0, 0), Is.EqualTo(ObstacleReactionKind.None));

        [Test]
        public void UnchangedOrIncreasedHealth_DoesNotReact()
        {
            Assert.That(ObstacleReactionCalculator.Select(10, 10), Is.EqualTo(ObstacleReactionKind.None));
            Assert.That(ObstacleReactionCalculator.Select(10, 11), Is.EqualTo(ObstacleReactionKind.None));
        }

        [Test]
        public void BarrelMask_ClipsEdgesAndExcludesCenter()
        {
            var corner = ObstacleReactionCalculator.BarrelCells(CellCoord.Zero, 5, 5);
            Assert.That(corner, Is.EquivalentTo(new[] { CellCoord.Right, new CellCoord(0, 1), new CellCoord(1, 1) }));
            var center = ObstacleReactionCalculator.BarrelCells(new CellCoord(2, 2), 5, 5);
            Assert.That(center.Count, Is.EqualTo(8));
            Assert.That(center, Has.No.Member(new CellCoord(2, 2)));
            Assert.That(ObstacleReactionCalculator.BarrelCells(CellCoord.Zero, 1, 1), Is.Empty);
        }

        [Test]
        public void InvalidGeometryOrHealth_Rejects()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleReactionCalculator.Select(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleReactionCalculator.BarrelCells(CellCoord.Zero, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleReactionCalculator.BarrelCells(new CellCoord(5, 0), 5, 5));
        }
    }
}
