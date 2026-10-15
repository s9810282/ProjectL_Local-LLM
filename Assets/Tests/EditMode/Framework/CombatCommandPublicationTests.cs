using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public class CombatCommandPublicationTests
    {
        [Test]
        public void BadgeBlockedBlast_PublishesUseConsumptionWithoutHealthLoss()
        {
            var b = PrepareBarrel();
            var main = Main(b);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main,
                HeldEnvironmentEffects.AcquireBadge(main, 41)) }, Array.Empty<SoulCountChange>()));
            var result = CombatCommandCalculator.Calculate(BarrelInput(b), Array.Empty<IMovementTileRule>());
            b.BattleWriteSink.Apply(result);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(Main(b).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var published), Is.True);
            Assert.That(published.Events.OfType<CombatHeldEnvironmentEvent>().Single().Final.BadgeAvailable, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SuppliesReward_PublicationIsAtomicIncludingProfileValues(bool staleProfile)
        {
            var b = PrepareBarrel(supplies: true);
            var i = BarrelInput(b);
            if (staleProfile)
                i = new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles,
                    i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, environmentBindings: i.EnvironmentBindings,
                    obstacleContents: new[] { new ObstacleContentProfile(new EntityId(51), ObstacleContentKind.Supplies, 0, 3, 5) });
            var transition = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            if (staleProfile)
            {
                Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
                Assert.That(Main(b).HeldEnvironment.SuppliesAttackBonus, Is.Zero);
                Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.CurrentHealth, Is.EqualTo(100));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            }
            else
            {
                b.BattleWriteSink.Apply(transition);
                Assert.That(Main(b).HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(2));
                Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.CurrentHealth, Is.LessThan(100));
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var published), Is.True);
                Assert.That(published.Events.OfType<CombatHeldEnvironmentEvent>().Single().Final.SuppliesAttackBonus, Is.EqualTo(2));
            }
        }

        [Test]
        public void StaleHeldState_RejectsWithoutHealthOrResultPublication()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var before = Main(b);
            var charged = HeldEnvironmentEffects.AcquireBadge(before, 41);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, charged) },
                Array.Empty<SoulCountChange>()));
            var stale = new CombatCommandTransition(input, true, "",
                CharacterSettlementCalculator.SetHealth(before, input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(stale));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(Main(b).HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [Test]
        public void BarrelCommand_PublishesExplosionHealthAndReactionsTogether()
        {
            var b = PrepareBarrel();
            var input = BarrelInput(b);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.CurrentHealth, Is.EqualTo(1));
            b.BattleWriteSink.Apply(transition);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(6));
            Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.IsDead, Is.True);
            Assert.That(b.CombatStateReadSink.Current.ObstacleContents.Single().SameValues(input.ObstacleContents.Single()), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single().Reaction, Is.EqualTo(ObstacleReactionKind.Destroyed));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.HealthChanged)
                .DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(1, 0)));
        }

        [Test]
        public void StaleObstacleProfile_RejectsWithoutAnyInstall()
        {
            var b = PrepareBarrel();
            var i = BarrelInput(b);
            var stale = new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, i.State, i.Characters,
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                environmentBindings: i.EnvironmentBindings, obstacleContents:
                new[] { new ObstacleContentProfile(new EntityId(51), ObstacleContentKind.PowderBarrel, 2) });
            var transition = CombatCommandCalculator.Calculate(stale, Array.Empty<IMovementTileRule>());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CombatStateReadSink.Current.ObstacleContents.Single().ExplosionDamage, Is.EqualTo(1));
        }

        private static IBufferQuery PrepareBarrel(bool supplies = false)
        {
            var b = CharacterBattleSettlementTests.Create();
            var field = b.CombatStateReadSink.SoulField;
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.EndTurn(Main(b),
                new SoulActionChanges(Empty(), new SoulFieldChange(field, field), Array.Empty<long>())));
            var main = Main(b);
            b.BattleWriteSink.EndBattle();
            var kind = supplies ? ObstacleContentKind.Supplies : ObstacleContentKind.PowderBarrel;
            int hp = supplies ? 100 : 1;
            var structure = new StructureCombatFacts(new EntityId(51), new StructureSpec(ObstacleContentProfile.KeyFor(kind),
                1, 1, new[] { CellCoord.Zero }, hp, false, 0), new HealthStateSnapshot(hp, hp),
                new CellCoord(1, 0), Rotation4.R0, new[] { new CellCoord(1, 0) }, Owner.Neutral);
            var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), new[] { structure.EntityId });
            var combat = new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput, Array.Empty<CharacterInstanceId>(),
                environmentBindings: bindings, obstacleContents: new[] { new ObstacleContentProfile(structure.EntityId, kind,
                    supplies ? 0 : 1, supplies ? 2 : 0, supplies ? 5 : 0) });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, main.EntityId, false, new[] { structure },
                Array.Empty<ThornFieldCellSnapshot>(), new[] { CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 100, 0, true) },
                new CombatTurnFacts(StagePhase.Combat));
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, combat, new[] { structure }, 52, state));
            return b;
        }

        private static CombatCommandInput BarrelInput(IBufferQuery b)
        {
            var i = Input(b);
            var unit = CharacterBattlePreparationTests.Initialization().Units.Single(u => u.Key == EntityKey.Unit_MainCharacter);
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, AttackAim.ForCell(new CellCoord(1, 0)), i.State, i.Characters,
                new[] { new CombatAttackProfile(unit, new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)) },
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, environmentBindings: i.EnvironmentBindings,
                obstacleContents: b.CombatStateReadSink.Current.ObstacleContents);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BindingsOnlyStaleInput_RejectsWithoutPublication(bool currentHasBindings)
        {
            var b = CharacterBattleSettlementTests.Create();
            if (currentHasBindings) PrepareBoundEnvironment(b);
            var input = Input(b);
            var stale = new EnvironmentSemanticBindings(new[] { 11L }, Array.Empty<long>(), Array.Empty<EntityId>());
            var staleInput = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, environmentBindings: stale);
            var before = b.CombatStateReadSink.Current;
            var transition = new CombatCommandTransition(staleInput, true, "",
                CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            AssertUnchanged(b);
        }

        [Test]
        public void EquivalentBindings_AllowPublicationWithoutReplacingCurrentMetadata()
        {
            var b = CharacterBattleSettlementTests.Create();
            PrepareBoundEnvironment(b);
            var input = Input(b);
            var installed = b.CombatStateReadSink.Current.EnvironmentBindings;
            var reordered = new EnvironmentSemanticBindings(new[] { 12L, 11L },
                Array.Empty<long>(), Array.Empty<EntityId>());
            var captured = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, environmentBindings: reordered);
            b.BattleWriteSink.Apply(new CombatCommandTransition(captured, true, "",
                CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress()));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.Current.EnvironmentBindings, Is.SameAs(installed));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
        }

        private static void PrepareBoundEnvironment(IBufferQuery b)
        {
            var character = Main(b);
            b.BattleWriteSink.EndBattle();
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                new DestinationTileModifierFact(11, CellCoord.Zero, true, new[] { Owner.Player }, 0, 0),
                new DestinationTileModifierFact(12, CellCoord.Right, true, new[] { Owner.Player }, 0, 0)
            });
            var bindings = new EnvironmentSemanticBindings(new[] { 11L, 12L },
                Array.Empty<long>(), Array.Empty<EntityId>());
            var combat = new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput,
                Array.Empty<CharacterInstanceId>(), environment: environment, environmentBindings: bindings);
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { character }, combat));
        }

        [Test]
        public void PreparedResult_PreservesComposedPathAndOrderedEvents()
        {
            var buffers = CharacterBattleSettlementTests.Create();
            var field = buffers.CombatStateReadSink.SoulField;
            buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.EndTurn(Main(buffers),
                new SoulActionChanges(Empty(), new SoulFieldChange(field, field), Array.Empty<long>())));
            var input = Input(buffers);
            var original = input.Characters.Single();
            var from = original.Anchor.Value;
            var to = from + CellCoord.Right;
            var s = original.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange,
                s.AttackPower, s.PhysicalDefense, s.MaxActionCost, s.AttackCost, s.RemainingMoveRange - 1,
                s.CurrentActionCost - s.MoveCost, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category,
                s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus);
            var moved = new CharacterCombatFacts(original.EntityId, original.OwnedId, original.CharacterKey,
                original.Owner, original.OwnershipRole, original.Area, original.Health, stats, original.ReserveSlotIndex,
                to, original.Rotation, new[] { to }, original.MovementCheckpointAnchor, original.CommittedMoveDistance,
                original.PendingMoveActionCost + s.MoveCost, original.TemporaryAttackBonus, original.TemporaryMaxHealthBonus);
            var health = CharacterSettlementCalculator.SetHealth(moved, input.Main, 6);
            var final = health.Characters.Single().Final;
            var changes = new CharacterChanges(new[] { new CharacterStateChange(original, final) },
                health.Souls, health.Main);
            var path = new MovementPath(new[] { from, to });
            var payload = new MovementPathResult(path, new[] { to }, to, s.MoveCost, 1, MovementStopReason.Completed, true);
            var events = new CombatEvent[] { new CombatCharacterEvent(CombatEventKind.Moved, original, moved),
                new CombatCharacterEvent(CombatEventKind.HealthChanged, moved, final) };
            buffers.BattleWriteSink.Apply(new CombatCommandTransition(input, true, "", changes,
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress(), events: events, pathMovement: payload));

            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandKind, Is.EqualTo(CombatCommandKind.Attack));
            Assert.That(result.PathMovement, Is.SameAs(payload));
            Assert.That(result.Events, Is.EqualTo(events));
            Assert.That(Main(buffers).Anchor, Is.EqualTo(to));
            Assert.That(Main(buffers).Health.CurrentHealth, Is.EqualTo(6));
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidFieldOrTimeline_PublishesNeitherHealthNorResult(bool timeline)
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var state = new CombatCommandState(8, 6, input.State.Phase, input.State.ActiveActorId, false,
                input.State.Structures, timeline ? input.State.ThornFields : new[] { default(ThornFieldCellSnapshot) },
                timeline ? new[] { CombatTimelineEntrySnapshot.ForUnit(new EntityId(99), 1, 0, false) } : input.State.Timeline);
            var transition = new CombatCommandTransition(input, true, "",
                CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), state, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            AssertUnchanged(b);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidInitialFieldOrTimeline_DoesNotInstallBattle(bool timeline)
        {
            var b = CharacterBattleSettlementTests.Create();
            var original = Main(b);
            b.BattleWriteSink.EndBattle();
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                Array.Empty<StructureCombatFacts>(), timeline ? Array.Empty<ThornFieldCellSnapshot>() : new[] { default(ThornFieldCellSnapshot) },
                timeline ? new[] { CombatTimelineEntrySnapshot.ForUnit(new EntityId(99), 1, 0, false) } : Array.Empty<CombatTimelineEntrySnapshot>());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.PrepareBattle(
                new BattlePreparation(new[] { original }, CharacterBufferTestData.Combat(), commandState: state)));
            Assert.That(b.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
            Assert.That(b.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(7));
        }

        [Test]
        public void ResultNumbersAndLastResult_SurviveEndBattleAndNextPreparation()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            b.BattleWriteSink.Apply(new CombatCommandTransition(input, false, "InvalidAim", Empty(),
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress()));
            var character = Main(b);
            b.CombatResultReadSink.TryGetLatest(out var before);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { character }, CharacterBufferTestData.Combat()));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var after), Is.True);
            Assert.That(after, Is.SameAs(before));
        }

        [Test]
        public void ChangedCharacterIdentity_PublishesNoCharacterSoulOrResult()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b); var original = Main(b);
            var malformed = new CharacterCombatFacts(original.EntityId, new CharacterInstanceId(99), original.CharacterKey,
                original.Owner, original.OwnershipRole, original.Area, new HealthStateSnapshot(4, original.Health.MaxHealth),
                original.Stats, original.ReserveSlotIndex, original.Anchor, original.Rotation, original.OccupiedCells,
                original.MovementCheckpointAnchor, original.CommittedMoveDistance, original.PendingMoveActionCost);
            var health = CharacterSettlementCalculator.SetHealth(original, input.Main, 4);
            var changes = new CharacterChanges(new[] { new CharacterStateChange(original, malformed) }, Array.Empty<SoulCountChange>(), health.Main);
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 0, 1, "soul"), new CellCoord(3, 3), 5, 1) });
            var transition = new CombatCommandTransition(input, true, "", changes,
                new SoulFieldChange(input.Souls, field), input.State, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            AssertUnchanged(b);
        }

        [Test]
        public void ProcessingService_FinalAttackPublishesHpApStructuresCompletionAndProgressTogether()
        {
            var b = CharacterBattleSettlementTests.Create();
            var main = Main(b);
            var s = main.Stats;
            main = CharacterBufferTestData.WithSnapshot(main, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange,
                s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                s.AttackCost, s.RemainingMoveRange, 4, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category));
            var hq = new StructureCombatFacts(new EntityId(2),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 5, false, 0),
                new HealthStateSnapshot(5, 5), new CellCoord(2, 1), Rotation4.R0, new[] { new CellCoord(2, 1) });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, main.EntityId, false,
                new[] { hq }, Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 1, 0, true) });
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, CharacterBufferTestData.Combat(), new[] { hq }, commandState: state));
            var initial = CharacterBattlePreparationTests.Initialization();
            var profiles = initial.Units.Select(u => new CombatAttackProfile(u,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            var definition = new BattleInitialization(initial.Stage, initial.MainAnchor, initial.MainRotation,
                initial.Units, initial.Structures, initial.Categories, profiles);
            var root = new SystemBuilderHelper(); root.AddService<IBufferQuery>(b);
            var progression = new ProgressionProbe();
            var service = new CombatProcessingService(new DefinitionSource(definition), new RosterProbe(), System.Array.AsReadOnly(new IMovementTileRule[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornMovementTileRule() }), progression);
            service.InitData(new CharacterRosterDatabase()); service.PostBindService(root.BuildSystemReader().ServiceStoreReader);
            b.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(main.EntityId, AttackAim.ForCell(new CellCoord(2, 1))));
            Assert.That(service.ProcessNextCommand(), Is.True);
            Assert.That(service.ProcessNextCommand(), Is.False);
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(3));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.IsDead, Is.True);
            Assert.That(b.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(progression.Calls, Is.EqualTo(1));
            Assert.That(b.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
        }

        [TestCase(7, 13, 3, 3)]
        [TestCase(14, 15, 1, 0)]
        [TestCase(15, 15, 0, 0)]
        public void ChaliceCommand_PublishesOrderedCappedReceiptsOnce_AndPreservesReadonlyResultAfterCleanup(
            int health, int healed, long firstApplied, long secondApplied)
        {
            var b = CharacterBattleSettlementTests.Create();
            b.CharacterReadSink.TryGetMainState(out var oldMain);
            var persistent = CharacterBufferTestData.MainState(health, 15);
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetPersistentMain(Main(b), oldMain, persistent));
            var main = Main(b);
            var s = main.Stats;
            main = CharacterBufferTestData.WithSnapshot(main, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange,
                s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                s.AttackCost, s.RemainingMoveRange, 4, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category));
            var hq = new StructureCombatFacts(new EntityId(2),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 5, false, 0),
                new HealthStateSnapshot(5, 5), new CellCoord(2, 1), Rotation4.R0, new[] { new CellCoord(2, 1) });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, main.EntityId, false,
                new[] { hq }, Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 1, 0, true) });
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, CharacterBufferTestData.Combat(),
                new[] { hq }, commandState: state));
            var chalice = new ArtifactEffectData
            {
                entryId = "chalice", trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any },
                target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal },
                value = new ArtifactValueData { amount = 3 }
            };
            var loadout = new ArtifactLoadoutSnapshot(0, new[]
                { new ArtifactPlacedEffectSnapshot(1, 2, chalice), new ArtifactPlacedEffectSnapshot(1, 1, chalice) });
            var profiles = CharacterBattlePreparationTests.Initialization().Units.Select(u => new CombatAttackProfile(u,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            var input = new CombatCommandInput(1, CombatCommandKind.Attack, main.EntityId,
                AttackAim.ForCell(new CellCoord(2, 1)), state, new[] { main }, profiles, persistent,
                b.CharacterReadSink.Owned, b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => 0),
                b.CombatStateReadSink.SoulField, loadout);
            var calculated = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(calculated.Accepted, Is.True, calculated.Reason);
            var completion = new BattleCompletionChanges(new SoulActionChanges(calculated.Characters,
                calculated.Field, calculated.ConsumedSoulIds), calculated.Outcome,
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true));
            var transition = calculated.WithCompletion(completion);
            b.BattleWriteSink.Apply(transition);
            b.BattleWriteSink.Apply(transition);
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(healed, 15)));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(3));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var artifact = result.Events.OfType<CombatArtifactEvent>().Single();
            Assert.That(artifact.Activations.Select(a => a.TargetId), Is.All.EqualTo(main.EntityId));
            Assert.That(artifact.Activations.Select(a => a.RequestedAmount), Is.EqualTo(new long[] { 3, 3 }));
            Assert.That(artifact.Activations.Select(a => a.AppliedAmount), Is.EqualTo(new[] { firstApplied, secondApplied }));
            Assert.That(artifact.Activations[0].Origin.CompareTo(artifact.Activations[1].Origin), Is.LessThan(0));
            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<ArtifactActivationSnapshot>)artifact.Activations).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<CombatEvent>)result.Events).Clear());
            var final = Main(b);
            b.CombatViewWriteSink.AcknowledgePresented(result.Sequence);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { final }, CharacterBufferTestData.Combat()));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var reread), Is.True);
            Assert.That(reread, Is.SameAs(result));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(healed, 15)));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.True);
        }

        [Test]
        public void SoulCollection_ChangedFieldRejectsHealthCountAndPersistentMainTogether()
        {
            var b = CharacterBattleSettlementTests.Create();
            var main = Main(b);
            var source = new SoulFieldSnapshot(3, new[]
            {
                new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 1, 1, "soul"), main.Anchor.Value, 5, 1),
                new SoulFieldFacts(2, new EffectOriginKey(EffectOwnerKind.Artifact, 1, 1, "soul"), main.Anchor.Value, 5, 1)
            });
            b.BattleWriteSink.Apply(new SoulActionChanges(Empty(),
                new SoulFieldChange(b.CombatStateReadSink.SoulField, source), Array.Empty<long>()));
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var calculator = new SoulEffectCalculator(default);
            var planned = calculator.Collect(new SoulCollectorInput(main, b.CharacterReadSink.Owned.Single(),
                0, persistent.PersistentMaxHealth, persistent), source, true);
            var ticked = calculator.PlanCountdown(source);
            b.BattleWriteSink.Apply(new SoulActionChanges(Empty(), ticked, Array.Empty<long>()));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(planned));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(7, 10)));
            Assert.That(b.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.Zero);
            Assert.That(b.CharacterReadSink.TryGetMainState(out var after), Is.True);
            Assert.That(after.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(ticked.Final), Is.True);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out _), Is.False);
        }

        private sealed class DefinitionSource : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal DefinitionSource(BattleInitialization definition) { _definition = definition; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) => _definition;
        }
        private sealed class RosterProbe : ICharacterRosterService
        {
            public RosterAcquireResult InitializeRoster(int key) => throw new NotSupportedException();
            public RosterAcquireResult AcquireSub(EntityKey key) => throw new NotSupportedException();
            public InitialDrawPlan PlanInitialDraw(int seed) => throw new NotSupportedException();
        }
        private sealed class ProgressionProbe : IChapterProgressionCoordinator
        {
            public RunOutcome Outcome => RunOutcome.InProgress;
            public RunCompletionReceipt Completion => null;
            public ChapterContinuationResult CompleteChapter(int key, StageNodeId id) => throw new NotSupportedException();
            internal int Calls;
            public ChapterProgressionResult PlanStageResolution(int chapterKey, StageOutcome outcome)
            {
                Calls++;
                return ChapterProgressionResult.FinishStage(chapterKey, new StageNodeId(2), new StageKey(10), true);
            }
            public ChapterProgressionResult EnterChapter(int key) => throw new NotSupportedException();
            public ChapterProgressionResult PrepareStageEntry(int key, StageNodeId id) => throw new NotSupportedException();
            public ChapterProgressionResult ConfirmStageEntry(int key, StageNodeId id) => throw new NotSupportedException();
            public ChapterProgressionResult ResolveStage(int key, StageOutcome outcome) => throw new NotSupportedException();
        }

        [Test]
        public void ProcessingService_NoInputPublishesNothing()
        {
            var b = CharacterBattleSettlementTests.Create();
            Assert.That(Service(b).ProcessNextCommand(), Is.False);
            AssertUnchanged(b);
        }

        [Test]
        public void ProcessingService_HandlesRejectionOnceAndNeverAcknowledgesOrRemovesViewInput()
        {
            var b = CharacterBattleSettlementTests.Create();
            int number = b.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(new EntityId(1), AttackAim.ForCell(new CellCoord(99, 99))));
            var service = Service(b);
            Assert.That(service.ProcessNextCommand(), Is.True);
            Assert.That(service.ProcessNextCommand(), Is.False);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatViewReadSink.Commands.Single().Number, Is.EqualTo(number));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
        }

        private static ICombatProcessingService Service(IBufferQuery buffers)
        {
            var root = new SystemBuilderHelper();
            root.AddService<IBufferQuery>(buffers);
            // 명령 게시 검증은 명시적인 테스트 정의와 정상 DB 초기화 경로를 사용한다.
            var core = InGameCoreSystemFactory.Create(new DefinitionSource(CharacterBattlePreparationTests.Initialization()));
            ((ISystemInitData)core).InitData(new CharacterRosterDatabase());
            ((ISystemPostBindService)core).PostBindService(root.BuildSystemReader().ServiceStoreReader);
            return ((IInGameCoreSystemGroupService)core).Combat;
        }

        [Test]
        public void Rejection_PublishesOnceWithoutGameplayChanges()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var rejected = new CombatCommandTransition(input, false, "InvalidAim", Empty(),
                new SoulFieldChange(input.Souls, input.Souls), input.State, InProgress());
            b.BattleWriteSink.Apply(rejected);
            b.BattleWriteSink.Apply(rejected);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("InvalidAim"));
        }

        [Test]
        public void InvalidFinalSoulField_PublishesNeitherHealthNorResult()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var field = new SoulFieldSnapshot(2, new[]
            {
                new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Trait, 1, 1, "soul"), new CellCoord(99, 0), 1, 3)
            });
            var change = CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4);
            var transition = new CombatCommandTransition(input, true, "", change,
                new SoulFieldChange(input.Souls, field), input.State, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            AssertUnchanged(b);
        }

        [Test]
        public void InvalidStructureIdentity_PublishesNeitherHealthNorResult()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var invented = new StructureCombatFacts(new EntityId(2),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0),
                new HealthStateSnapshot(10, 20), new CellCoord(2, 4), Rotation4.R0, new[] { new CellCoord(2, 4) });
            var state = new CombatCommandState(8, 6, input.State.Phase, input.State.ActiveActorId,
                false, new[] { invented }, input.State.ThornFields, input.State.Timeline);
            var transition = new CombatCommandTransition(input, true, "",
                CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), state, InProgress());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            AssertUnchanged(b);
        }

        [Test]
        public void InvalidCompletionSelection_PublishesNeitherHealthNorProgressNorResult()
        {
            var b = CharacterBattleSettlementTests.Create();
            var input = Input(b);
            var outcome = new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed);
            var settled = new SoulActionChanges(CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), Array.Empty<long>());
            var completion = new BattleCompletionCalculator(default).Complete(Main(b), input.Main,
                outcome, ChapterProgressionResult.FinishStage(1, new StageNodeId(999), new StageKey(10), true), settled);
            var state = new CombatCommandState(8, 6, CombatFlowPhase.Finished, input.State.ActiveActorId,
                false, input.State.Structures, input.State.ThornFields, input.State.Timeline);
            var transition = new CombatCommandTransition(input, true, "", completion.Characters,
                settled.Field, state, outcome, completion: completion);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            AssertUnchanged(b);
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResultCounterOverflow_FaultsBeforeHealthPublication(bool defeat)
        {
            var b = CharacterBattleSettlementTests.Create();
            b.CombatStateReadSink.GetType().GetProperty("LatestResultSequence")!
                .SetValue(b.CombatStateReadSink, int.MaxValue);
            var input = Input(b);
            var changes = CharacterSettlementCalculator.SetHealth(Main(b), input.Main, 4);
            var outcome = defeat ? new StageOutcomeSnapshot(StageOutcome.Defeat,
                StageOutcomeReason.VoluntaryAbandonment) : InProgress();
            var completion = defeat ? new BattleCompletionChanges(changes, outcome,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10))) : null;
            var state = defeat ? new CombatCommandState(input.State.BoardWidth, input.State.BoardHeight,
                CombatFlowPhase.Finished, input.State.ActiveActorId, false, input.State.Structures,
                input.State.ThornFields, input.State.Timeline) : input.State;
            var transition = new CombatCommandTransition(input, true, "", changes,
                new SoulFieldChange(input.Souls, input.Souls), state, outcome, completion: completion);
            Assert.Throws<OverflowException>(() => b.BattleWriteSink.Apply(transition));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out _), Is.False);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(b.RunProgressReadSink.Completion, Is.Null);
        }

        internal static CombatCommandInput Input(IBufferQuery b, int number = 1)
        {
            b.CharacterReadSink.TryGetMainState(out var main);
            return new CombatCommandInput(number, CombatCommandKind.Attack, Main(b).EntityId,
                AttackAim.ForCell(new CellCoord(2, 1)), b.CombatStateReadSink.Current.CommandState,
                b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(), Array.Empty<CombatAttackProfile>(),
                main, b.CharacterReadSink.Owned,
                b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                b.CombatStateReadSink.SoulField, default, environmentBindings: b.CombatStateReadSink.Current.EnvironmentBindings,
                obstacleContents: b.CombatStateReadSink.Current.ObstacleContents);
        }

        private static CharacterCombatFacts Main(IBufferQuery b) => b.CharacterReadSink.Characters.Single().ToFacts();
        private static CharacterChanges Empty() => new(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
        private static StageOutcomeSnapshot InProgress() => new(StageOutcome.InProgress, StageOutcomeReason.None);
        private static void AssertUnchanged(IBufferQuery b)
        {
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out _), Is.False);
        }
    }
}
