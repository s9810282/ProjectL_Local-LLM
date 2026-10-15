using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ObjectArrivalOrderTests
    {
        [Test]
        public void ExistingHazard_PrecedesObject_AndRecoveryReadsLatestHeldState()
        {
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Right, 1, true, new[] { Owner.Player }));
            var s = i.State;
            i = CombatCommandTestData.With(i, state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase,
                s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(s.Environment.Tiles.Append(new FixturePhaseProbe()).ToArray())));
            var r = CombatCommandCalculator.Calculate(i, new IMovementTileRule[] { new SheriffBadgeMovementTileRule(), new FixturePhaseProbeRule() });
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(MovementTestData.Actor(r).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(MovementTestData.Actor(r).HeldEnvironment.BadgeAvailable, Is.True);
            var activation = r.Events.ToList().FindIndex(e => e is CombatObjectActivationEvent);
            var health = r.Events.OfType<CombatCharacterEvent>().Where(e => e.Expected.Health.CurrentHealth != e.Final.Health.CurrentHealth).ToArray();
            Assert.That(r.Events.ToList().IndexOf(health[0]), Is.LessThan(activation));
            Assert.That(r.Events.ToList().IndexOf(health[1]), Is.GreaterThan(activation));
        }

        private sealed class FixturePhaseProbe : MovementTileFact
        {
            internal FixturePhaseProbe() : base(7, CellCoord.Right, "fixture-phase-probe", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => other is FixturePhaseProbe && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
        }
        private sealed class FixturePhaseProbeRule : IMovementTileRule
        {
            public string Key => "fixture-phase-probe";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                bool recovery = input.Phase == MovementEffectPhase.Recovery;
                Assert.That(input.Health.CurrentHealth, Is.EqualTo(recovery ? 99 : 100));
                return recovery ? new(healing: new[] { new MovementHealthRequest(input.MoverId, 1) })
                    : new(damage: new[] { new MovementHealthRequest(input.MoverId, 1) });
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void ArrivalVisibility_OrdinalChangesInvalidateOnlyVisibleObjectContext(bool hidden)
        {
            var known = new[] { hidden ? Owner.Enemy : Owner.Player };
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Right, 1, true, known));
            var rules = new IMovementTileRule[] { new SheriffBadgeMovementTileRule() };
            var context = MovementContextCalculator.Capture(i, Owner.Player, rules);
            var changed = WithObjects(i, new SheriffBadgeSourceFact(99, CellCoord.Right, 2, true, known));
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, rules), Is.EqualTo(hidden));
            Assert.That(context.VisibleEnvironment.Tiles.Count, Is.EqualTo(hidden ? 0 : 1));
        }

        [Test]
        public void ForcedOtherArrival_ActivatesObjectAndCapturesDestinationExpectedFacts()
        {
            var i = Objects();
            var other = CombatCommandTestData.Character(50, EntityKey.Unit_Demon, new CellCoord(2, 0), Owner.Enemy);
            i = CombatCommandTestData.With(i, i.Characters.Append(other).ToArray());
            var badge = new SheriffBadgeSourceFact(99, new CellCoord(2, 1), 1, true, new[] { Owner.Player });
            i = WithObjects(i, badge);
            var s = i.State;
            i = CombatCommandTestData.With(i, state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId,
                s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(new MovementTileFact[] { badge, new FixtureForce(other.EntityId) })));
            var rules = new IMovementTileRule[] { new SheriffBadgeMovementTileRule(), new FixtureForceRule() };
            var context = MovementContextCalculator.Capture(i, Owner.Player, rules);
            Assert.That(context.VisibleEnvironment.Tiles.Select(t => t.Id), Does.Contain(99));
            var r = CombatCommandCalculator.Calculate(i, rules);
            Assert.That(r.Characters.Characters.Single(c => c.Final.EntityId == other.EntityId).Final.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(r.FinalState.Environment.Tiles.Single(t => t.Id == 99).Active, Is.False);
            Assert.That(r.PathMovement.FinalStop, Is.EqualTo(CellCoord.Right));
        }

        private sealed class FixtureForce : MovementTileFact
        {
            internal EntityId Target { get; }
            internal FixtureForce(EntityId target, bool active = true) : base(7, CellCoord.Right, "fixture-force", 0, active, new[] { Owner.Player }) => Target = target;
            public override bool SameValues(MovementTileFact other) => other is FixtureForce f && SameSourceValues(f) && Target == f.Target;
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
        }
        private sealed class FixtureForceRule : IMovementTileRule
        {
            public string Key => "fixture-force";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { ((FixtureForce)tile).Target };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Hazard
                ? new(forcedMovement: new[] { new MovementForcedMoveRequest(((FixtureForce)tile).Target, Rotation4.R0, 1) },
                    expectedSource: tile, finalSource: new FixtureForce(((FixtureForce)tile).Target, false)) : new();
        }
        [Test]
        public void EnemyNightRedeployment_ImmediatelyAcquiresObjectAndRetainsItInParticipants()
        {
            var i = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var cell = new CellCoord(2, 7);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, cell, Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var headquarters = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), new CellCoord(0, 7), Rotation4.R0, new[] { new CellCoord(0, 7) }, Owner.Enemy);
            var badge = new SheriffBadgeSourceFact(99, cell, 1, true, new[] { Owner.Enemy });
            var turn = new CombatTurnFacts(StagePhase.Deployment, enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, cell, Rotation4.R0) });
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { headquarters }, Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>(), turn,
                new MovementEnvironmentFacts(new[] { badge }));
            var input = new CombatCommandInput(1, i.Kind, EntityId.Empty, default, state,
                new[] { i.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter), waiting }, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(10, 20, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0, 21, 10, 2),
                environmentBindings: Bindings(99));
            var r = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new SheriffBadgeMovementTileRule() });
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = r.Characters.Characters.Single(c => c.Final.EntityId == enemy.EntityId).Final;
            Assert.That(final.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(r.FinalState.Turn.Participants.Select(p => p.EntityId), Does.Contain(enemy.EntityId));
            Assert.That(r.Events.OfType<CombatObjectActivationEvent>().Single().ActivatorId, Is.EqualTo(enemy.EntityId));
        }
        [Test]
        public void Placement_ActivatesObjectWithoutSpendingMovementResources()
        {
            var i = CombatCommonCommandTests.Command(CombatCommandKind.PlaceUnit);
            var hand = CombatPlacementCalculator.Location(i.Characters[0], CharacterRuntimeArea.Hand, 0, null, null);
            i = CombatCommandTestData.With(i, characters: new[] { hand, i.Characters[1] });
            var cell = new CellCoord(2, 0);
            var badge = new SheriffBadgeSourceFact(99, cell, 1, true, new[] { Owner.Player });
            var s = i.State;
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                s.Structures, s.ThornFields, s.Timeline, new CombatTurnFacts(StagePhase.Deployment), new MovementEnvironmentFacts(new[] { badge }));
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, i.Characters, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, targetCell: cell,
                battle: new CombatBattleFacts(10, 20, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2),
                environmentBindings: Bindings(99));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new SheriffBadgeMovementTileRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(final.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(i.Characters[0].Stats.CurrentActionCost));
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(result.PathMovement, Is.Null);
        }

        [Test]
        public void AttackKnockback_ActivatesDestinationObjectOnlyInActualResolution()
        {
            var i = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            i = CombatCommandTestData.With(i, characters: i.Characters.Where(c => c.EntityId != new EntityId(3) && c.EntityId != new EntityId(4)).ToArray());
            var badge = new SheriffBadgeSourceFact(99, new CellCoord(5, 3), 1, true, new[] { Owner.Enemy });
            var input = WithObjects(i, badge);
            var rules = new IMovementTileRule[] { new SheriffBadgeMovementTileRule() };
            CombatCommandCalculator.Preview(input, rules, out var publicProjection);
            Assert.That(publicProjection.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            var r = CombatCommandCalculator.Calculate(input, rules);
            var victim = r.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final;
            Assert.That(victim.Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(victim.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(r.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId).DamageDealt, Is.EqualTo(20));
        }

        internal static CombatCommandInput WithObjects(CombatCommandInput i, params ObjectSourceFact[] objects)
        {
            var s = i.State;
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim,
                new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                    s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(objects)),
                i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind, i.Path, i.MovementContext, i.BattleInstanceId,
                i.ArtifactItems, Bindings(objects.Select(o => o.Id).ToArray()), i.ObstacleContents);
        }

        [Test]
        public void Route_ConsumesDuplicateBadgesWithoutAddingProtection()
        {
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Right, 1, true, new[] { Owner.Player }),
                new SheriffBadgeSourceFact(1, CellCoord.Right, 2, true, new[] { Owner.Player }));
            var r = CombatCommandCalculator.Calculate(i, new IMovementTileRule[] { new SheriffBadgeMovementTileRule() });
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.FinalState.Environment.Tiles.All(t => !t.Active), Is.True);
            Assert.That(MovementTestData.Actor(r).HeldEnvironment.BadgeSourceId, Is.EqualTo(99));
            Assert.That(r.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id), Is.EqualTo(new[] { 99L, 1L }));
            Assert.That(r.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(r.PathMovement.ActionCostSpent, Is.EqualTo(1));
            var returned = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(r),
                CombatCommandKind.ReturnToCheckpoint), new IMovementTileRule[] { new SheriffBadgeMovementTileRule() });
            Assert.That(returned.Accepted, Is.False);
        }

        [Test]
        public void RouteOrigin_DoesNotSynthesizeObjectArrival()
        {
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Zero, 1, true, new[] { Owner.Player }));
            var r = CombatCommandCalculator.Calculate(i, new IMovementTileRule[] { new SheriffBadgeMovementTileRule() });
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.FinalState.Environment.Tiles.Single().Active, Is.True);
            Assert.That(r.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Route_StopsRemainingObjectsOnLethalObject(bool hazardFirst)
        {
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Right, hazardFirst ? 2 : 1, true, new[] { Owner.Player }),
                new FixtureHazard(1, hazardFirst ? 1 : 2));
            var r = CombatCommandCalculator.Calculate(i,
                new IMovementTileRule[] { new SheriffBadgeMovementTileRule(), new FixtureHazardRule() });
            Assert.That(MovementTestData.Actor(r).Health.IsDead, Is.EqualTo(hazardFirst));
            Assert.That(r.FinalState.Environment.Tiles.Single(t => t.Id == 99).Active, Is.EqualTo(hazardFirst));
            Assert.That(r.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id),
                Is.EqualTo(hazardFirst ? new[] { 1L } : new[] { 99L, 1L }));
        }

        [Test]
        public void SourceConsumption_CannotRewriteEntryOrdinal()
        {
            var badge = new SheriffBadgeSourceFact(99, CellCoord.Right, 10, true, new[] { Owner.Player });
            var projection = new MovementProjection(Objects(badge));
            var rewritten = new SheriffBadgeSourceFact(99, CellCoord.Right, 11, false, new[] { Owner.Player });
            Assert.Throws<InvalidOperationException>(() => projection.ChangeSource(badge,
                new MovementStepEffects(expectedSource: badge, finalSource: rewritten)));
            Assert.That(projection.Source(99).Active, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ObjectSequence_UsesLatestHeldState_DeathLeavesLaterPickupActive(bool hazardFirst)
        {
            var badge = new SheriffBadgeSourceFact(99, CellCoord.Right, hazardFirst ? 2 : 1, true,
                new[] { Owner.Player }, priority: 100);
            var hazard = new FixtureHazard(1, hazardFirst ? 1 : 2);
            var i = Objects(badge, hazard);
            var projection = new MovementProjection(i);
            projection.Enter(CellCoord.Right, 1);
            MovementEnvironmentCalculator.ResolveObjectsAt(projection, i.ActorId, CellCoord.Right,
                new IMovementTileRule[] { new SheriffBadgeMovementTileRule(), new FixtureHazardRule() });
            Assert.That(projection.Actor.Health.IsDead, Is.EqualTo(hazardFirst));
            Assert.That(projection.Source(badge.Id).Active, Is.EqualTo(hazardFirst));
            Assert.That(projection.Source(hazard.Id).Active, Is.False);
            Assert.That(projection.Actor.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(projection.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id),
                Is.EqualTo(hazardFirst ? new[] { 1L } : new[] { 99L, 1L }));
            Assert.That(projection.ReturnBarrierOccurred, Is.True);
        }

        internal static CombatCommandInput Objects(params ObjectSourceFact[] objects)
        {
            var i = MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right);
            var s = i.State;
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim,
                new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                    s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(objects)),
                i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                path: i.Path, battleInstanceId: 1, environmentBindings: Bindings(objects.Select(o => o.Id).ToArray()));
        }

        private sealed class FixtureHazard : ObjectSourceFact
        {
            internal FixtureHazard(long id, long ordinal, bool active = true) : base(id, CellCoord.Right,
                "fixture-object-hazard", ordinal, active, new[] { Owner.Player }, priority: -100) { }
            public override ObjectSourceFact WithActive(bool active) => new FixtureHazard(Id, EntryOrdinal, active);
            public override MovementTileFact WithKnownTo(System.Collections.Generic.IReadOnlyList<Owner> knownTo) => this;
            public override bool SameValues(MovementTileFact other) => other is FixtureHazard source && SameObjectValues(source);
        }
        private sealed class FixtureHazardRule : IMovementTileRule
        {
            public string Key => "fixture-object-hazard";
            public System.Collections.Generic.IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                new(damage: new[] { new MovementHealthRequest(input.MoverId, 100) }, expectedSource: tile,
                    finalSource: ((ObjectSourceFact)tile).WithActive(false));
        }

        [Test]
        public void Order_UsesEntryOrdinalRatherThanIdPriorityOrArrayOrder()
        {
            var later = new SheriffBadgeSourceFact(1, CellCoord.Right, 20, true, new[] { Owner.Player }, priority: -100);
            var earlier = new SheriffBadgeSourceFact(99, CellCoord.Right, 10, true, new[] { Owner.Player }, priority: 100);
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { later, earlier });
            var ordered = ObjectArrivalCalculator.OrderedAt(environment, Bindings(1, 99), CellCoord.Right);
            Assert.That(ordered.Select(o => o.Id), Is.EqualTo(new[] { 99L, 1L }));
            Assert.That(earlier.WithKnownTo(new[] { Owner.Enemy }).SameValues(earlier), Is.False);
            Assert.That(((ObjectSourceFact)earlier.WithKnownTo(new[] { Owner.Enemy })).EntryOrdinal, Is.EqualTo(10));
        }

        [Test]
        public void DuplicateOrdinal_RejectsWithoutIdTieBreaker()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                new SheriffBadgeSourceFact(1, CellCoord.Right, 10, true, new[] { Owner.Player }),
                new SheriffBadgeSourceFact(2, CellCoord.Right, 10, true, new[] { Owner.Player }) });
            Assert.Throws<ArgumentException>(() => ObjectArrivalCalculator.OrderedAt(environment, Bindings(1, 2), CellCoord.Right));
        }

        [Test]
        public void Progress_IsPartOfExpectedCommandState_AndTurnCopiesPreserveIt()
        {
            var i = CombatCommandTestData.Input();
            var s = i.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment, new EnvironmentProgressFacts(50));
            var stale = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment, new EnvironmentProgressFacts(51));
            Assert.That(state.SameValues(stale), Is.False);
            Assert.That(CombatTurnCalculator.State(state, state.Turn, state.Phase).EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(50));
        }

        [Test]
        public void Progress_RejectsNextOrdinalThatCanReuseAnExistingEntry()
        {
            var i = Objects(new SheriffBadgeSourceFact(99, CellCoord.Right, 10, false, new[] { Owner.Player }));
            var s = i.State;
            Assert.Throws<ArgumentException>(() => new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase,
                s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment,
                new EnvironmentProgressFacts(10)));
        }

        internal static EnvironmentSemanticBindings Bindings(params long[] ids) =>
            new(Array.Empty<long>(), ids, Array.Empty<EntityId>());
    }
}
