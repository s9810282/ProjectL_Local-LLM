using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Tests.Framework
{
    public sealed class DynamicEnvironmentPublicationTests
    {
        [Test]
        public void Tornado_PositionClockAndRandom_PublishAtomically_AndStaleProposalInstallsNothing()
        {
            var b = PrepareTornado();
            var i = TornadoInput(b);
            var r = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            Assert.That(r.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(i.State.EnvironmentProgress.RandomState));
            Assert.DoesNotThrow(() => b.BattleWriteSink.Apply(r));
            var published = b.CombatStateReadSink.Current;
            Assert.That(published.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
            Assert.That(published.CommandState.Turn.CompletedUnitTurnCount, Is.EqualTo(3));
            var stale = new CombatCommandInput(2, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: i.Battle, environmentBindings: i.EnvironmentBindings);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(CombatCommandCalculator.Calculate(stale, Array.Empty<IMovementTileRule>())));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(published));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
        }

        // Buffer는 저장 불변식만 검사한다. 주기·후보·난수 규칙은 TornadoEnvironmentTests의 Core 계산 테스트가 소유한다.
        [TestCase("Type")]
        [TestCase("Outside")]
        public void Tornado_StorageViolation_InstallsNoCharacterSourceTurnRandomOrResult(string fault)
        {
            var b = PrepareTornado();
            var i = TornadoInput(b);
            var calculated = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            var s = calculated.FinalState;
            var tornado = s.Environment.Tiles.OfType<TornadoSourceFact>().Single();
            var tiles = s.Environment.Tiles.Select(t => t.Id != tornado.Id ? t : fault == "Type"
                ? new DestinationTileModifierFact(tornado.Id, tornado.Cell, true, tornado.KnownTo, 1, 0)
                : new TornadoSourceFact(tornado.Id, new CellCoord(8, 0), tornado.KnownTo, tornado.NextDueCompletedTurn)).ToArray();
            var final = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(tiles), s.EnvironmentProgress);
            var proposal = new CombatCommandTransition(i, true, "", calculated.Characters, calculated.Field, final, calculated.Outcome,
                events: calculated.Events);
            var before = b.CombatStateReadSink.Current;
            var actor = b.CharacterReadSink.Characters.Single().ToFacts();
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Tornado_NoDrawForZeroOrSingleCandidate_PublishesWithoutRandomAdvance(int width)
        {
            var b = PrepareTornado(width);
            var i = TornadoInput(b);
            var r = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            Assert.That(r.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(i.State.EnvironmentProgress.RandomState));
            Assert.DoesNotThrow(() => b.BattleWriteSink.Apply(r));
            Assert.That(b.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState,
                Is.EqualTo(i.State.EnvironmentProgress.RandomState));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.OfType<TornadoSourceFact>().Single().Cell,
                Is.EqualTo(width == 1 ? CellCoord.Zero : CellCoord.Right));
        }

        private static ProjectL.Systems.RuntimeSystem.BufferSystems.Core.IBufferQuery PrepareTornado(int width = 8)
        {
            var b = CharacterBattleSettlementTests.Create();
            var actor = b.CharacterReadSink.Characters.Single().ToFacts();
            if (width < 8) actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner,
                actor.OwnershipRole, actor.Area, actor.Health, actor.Stats, null, CellCoord.Zero, actor.Rotation,
                new[] { CellCoord.Zero }, CellCoord.Zero, 0, 0, heldEnvironment: actor.HeldEnvironment);
            var sources = new MovementTileFact[] { new TornadoSourceFact(31, actor.Anchor.Value, new[] { Owner.Player }) };
            if (width == 8) sources = sources.Append(new StaticTerrainFact(32, CellCoord.Zero, StaticTerrainKind.HighGround,
                true, new[] { Owner.Player })).ToArray();
            var env = new MovementEnvironmentFacts(sources);
            var bindings = new EnvironmentSemanticBindings(sources.Select(t => t.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>());
            var turn = new CombatTurnFacts(StagePhase.Combat, completedUnitTurnCount: 2,
                participants: new[] { new CombatTurnParticipant(actor.EntityId, actor.Stats.Priority, actor.Stats.Delay) });
            int height = width == 8 ? 8 : 1;
            var state = new CombatCommandState(width, height, CombatFlowPhase.PlayerInput, actor.EntityId, true,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(actor.EntityId, actor.Stats.Priority, 0, true) }, turn, env);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor },
                new CombatPreparation(width, height, 4, 1000, 1, CombatFlowPhase.PlayerInput, Array.Empty<CharacterInstanceId>(),
                    environment: env, environmentBindings: bindings), commandState: state));
            return b;
        }

        private static CombatCommandInput TornadoInput(ProjectL.Systems.RuntimeSystem.BufferSystems.Core.IBufferQuery b)
        {
            var i = CombatCommandPublicationTests.Input(b);
            var profile = new CombatAttackProfile(CharacterBattlePreparationTests.Initialization().Units.Single(u => u.Key == EntityKey.Unit_MainCharacter),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            return new CombatCommandInput(i.Number, CombatCommandKind.EndUnitTurn, i.ActorId, default, i.State,
                i.Characters, new[] { profile }, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(4, 1000, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 100, 0, null),
                environmentBindings: i.EnvironmentBindings);
        }

        [Test]
        public void SourceBindingAndProgress_PublishTogether_AndStaleRepeatInstallsNothing()
        {
            var b = CharacterBattleSettlementTests.Create();
            var i = CombatCommandPublicationTests.Input(b);
            var transition = AddFlag(i);
            b.BattleWriteSink.Apply(transition);
            var published = b.CombatStateReadSink.Current;
            Assert.That(published.EnvironmentBindings.ObjectSourceIds, Has.Member(i.State.EnvironmentProgress.NextSourceId));
            Assert.That(published.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(i.State.EnvironmentProgress.NextSourceId + 1));
            Assert.DoesNotThrow(() => b.BattleWriteSink.Apply(transition));
            var stale = new CombatCommandInput(2, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, environmentBindings: i.EnvironmentBindings);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(AddFlag(stale)));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(published));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
        }

        [TestCase("MissingBinding")]
        [TestCase("DanglingBinding")]
        [TestCase("Unallocated")]
        [TestCase("OutsideBoard")]
        public void InvalidDynamicProposal_DoesNotInstallHealthEnvironmentOrResult(string fault)
        {
            var b = CharacterBattleSettlementTests.Create();
            var i = CombatCommandPublicationTests.Input(b);
            var before = b.CombatStateReadSink.Current;
            var transition = AddFlag(i, fault);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(i.Characters.Single().Health.CurrentHealth));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        private static CombatCommandTransition AddFlag(CombatCommandInput i, string fault = null)
        {
            var s = i.State;
            var p = s.EnvironmentProgress;
            var flag = new FlagSourceFact(p.NextSourceId, fault == "OutsideBoard" ? new CellCoord(s.BoardWidth, 1) : new CellCoord(1, 1),
                p.NextObjectEntryOrdinal, 7, true, new[] { Owner.Player });
            var environment = new MovementEnvironmentFacts(s.Environment.Tiles.Append(flag).ToArray());
            var progress = new EnvironmentProgressFacts(p.NextObjectEntryOrdinal + 1, p.NextSourceId + (fault == "Unallocated" ? 2 : 1), p.RandomState);
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, environment, progress);
            var bindings = new EnvironmentSemanticBindings(i.EnvironmentBindings.TerrainSourceIds,
                i.EnvironmentBindings.ObjectSourceIds.Append(fault == "DanglingBinding" ? 999 : flag.Id).ToArray(), i.EnvironmentBindings.ObstacleEntityIds);
            return new CombatCommandTransition(i, true, "", CharacterSettlementCalculator.SetHealth(i.Characters.Single(), i.Main, 4),
                new SoulFieldChange(i.Souls, i.Souls), state, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None),
                environmentBindings: fault == "MissingBinding" ? null : new EnvironmentBindingsChange(i.EnvironmentBindings, bindings));
        }

        [Test]
        public void ConsumedHistoricalBinding_RemainsValidWithoutResurrectionOnLaterCommand()
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.Apply(AddFlag(CombatCommandPublicationTests.Input(b)));
            var i = CombatCommandPublicationTests.Input(b, 2);
            var s = i.State;
            var removed = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, MovementEnvironmentFacts.Empty, s.EnvironmentProgress);
            b.BattleWriteSink.Apply(new CombatCommandTransition(i, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(i.Souls, i.Souls), removed, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None)));
            var next = CombatCommandPublicationTests.Input(b, 3);
            b.BattleWriteSink.Apply(AddFlag(next));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Count, Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Single().Id, Is.EqualTo(2));
            Assert.That(b.CombatStateReadSink.Current.EnvironmentBindings.ObjectSourceIds, Is.EqualTo(new[] { 1L, 2L }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleAllocatorOrRandom_RejectsBeforeAnyInstall(bool random)
        {
            var b = CharacterBattleSettlementTests.Create();
            var i = CombatCommandPublicationTests.Input(b);
            var s = i.State;
            var p = s.EnvironmentProgress;
            var staleState = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment,
                new EnvironmentProgressFacts(p.NextObjectEntryOrdinal, p.NextSourceId + (random ? 0 : 1), p.RandomState + (random ? 1u : 0u)));
            var stale = new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, staleState, i.Characters, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, environmentBindings: i.EnvironmentBindings);
            var before = b.CombatStateReadSink.Current;
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(AddFlag(stale)));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [Test]
        public void ExistingObjectId_CannotBeReplacedByPositionalModifier()
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.Apply(AddFlag(CombatCommandPublicationTests.Input(b)));
            var i = CombatCommandPublicationTests.Input(b, 2);
            var s = i.State;
            var flag = s.Environment.Tiles.Single();
            var replacement = new DestinationTileModifierFact(flag.Id, flag.Cell, true, new[] { Owner.Player }, 7, 0);
            var final = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(new[] { replacement }), s.EnvironmentProgress);
            var proposal = new CombatCommandTransition(i, true, "", CharacterSettlementCalculator.SetHealth(i.Characters.Single(), i.Main, 3),
                new SoulFieldChange(i.Souls, i.Souls), final, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None));
            var before = b.CombatStateReadSink.Current;
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal));
            Assert.That(b.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
        }
    }
}
