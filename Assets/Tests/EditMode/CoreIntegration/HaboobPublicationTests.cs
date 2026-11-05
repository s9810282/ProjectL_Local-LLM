using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class HaboobPublicationTests
    {
        [Test]
        public void CrossingAndUndoChangeQueriesWithoutCopyingCloudMembership()
        {
            using var battle = Create(new HaboobGimmickSpec(footprintOffsets: new[] { CellCoord.Zero }));
            var cloud = Cloud(battle);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var original = battle.Actor.Anchor.Value;
            Assert.That(cloud.Cells.Single(), Is.EqualTo(new CellCoord(4, 1)));
            var before = CombatCommandCalculator.GetUnitAttackRange(Query(battle)).RangeCells.ToArray();
            Assert.That(before.Length, Is.GreaterThan(0));
            battle.Submit(battle.PreviewMove(battle.Actor, Enumerable.Range(0, 5).Select(x => new CellCoord(x, 1)).ToArray()));
            Assert.That(Cloud(battle).SameValues(cloud), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(state.EnvironmentProgress), Is.True);
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(Query(battle)).RangeCells.Length, Is.EqualTo(0));
            Assert.That(battle.Actor.StatusEffects.Entries, Is.Empty);
            int token = battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single().MoveCommandNumber;
            battle.Submit(CombatViewCommandData.UndoMove(battle.Actor.EntityId, battle.Buffers.CombatStateReadSink.BattleInstanceId,
                battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence, token));
            Assert.That(battle.Actor.Anchor.Value, Is.EqualTo(original));
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(Query(battle)).RangeCells.ToArray(), Is.EqualTo(before));
            Assert.That(Cloud(battle).SameValues(cloud), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(state.EnvironmentProgress), Is.True);
        }

        private static CombatCommandInput Query(ConfiguredMovementBattle battle) => battle.Publication().Assembler.Assemble(
            new CombatActionRequest(1, CombatCommandKind.Attack, battle.Actor.EntityId, default));

        [Test]
        public void WarningAndRelocationPublishOnceAndRestoreFromCurrentState()
        {
            using var battle = Create();
            var initial = Cloud(battle);
            uint random = battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState;
            RockfallPublicationTests.EndPlayer(battle);
            Assert.That(Cloud(battle).Phase, Is.EqualTo(HaboobGimmickPhase.RelocationWarning));
            Assert.That(Cloud(battle).Cells, Is.EqualTo(initial.Cells));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState, Is.EqualTo(random));
            battle.AdvanceToPlayerOrDay();
            var after = Cloud(battle);
            Assert.That(after.Phase, Is.EqualTo(HaboobGimmickPhase.Active));
            Assert.That(after.Cells.SequenceEqual(initial.Cells), Is.False);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(Cloud(battle).SameValues(after), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleOrUnsupportedAreaPublicationRejectsAtomically(bool forgedFinal)
        {
            using var battle = Create();
            var input = EndInput(battle);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            if (forgedFinal) result = result.WithFinalState(result.FinalState.With(gimmicks: input.State.Gimmicks));
            else
            {
                var current = Cloud(battle);
                input = input.WithWorld(input.State.With(gimmicks: input.State.Gimmicks.Replace(new HaboobGimmickState(current.Id,
                    current.Phase, 99, current.Cells))), input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
                result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            }
            var before = battle.CaptureStores();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(result));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
        }

        [Test]
        public void FailedPreparationLeavesAllExistingStoresUntouched()
        {
            using var battle = Create();
            var b = battle.Buffers;
            Assert.That(b.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
            var owned = deck.OrderedIds.Select(id => b.CharacterReadSink.TryGetOwned(id, out var record) ? record : null).ToArray();
            Assert.That(b.CharacterReadSink.TryGetMainState(deck.MainId, out var main), Is.True);
            var definition = new Source(new StageGimmickSpec[] { new HaboobGimmickSpec(footprintOffsets:
                new[] { CellCoord.Zero, new CellCoord(8, 0) }) }).Read(battle.Database, 1, new StageKey(1101));
            var stores = battle.CaptureStores();
            var state = b.CombatStateReadSink.Current.CommandState;
            Assert.That(Assert.Throws<InvalidOperationException>(() => BattlePreparationCalculator.Prepare(definition, owned, main,
                owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                new InitialDrawPlan(owned.Where(o => o.Role == CharacterOwnershipRole.Sub).Select(o => o.Id).ToArray()),
                Array.Empty<CharacterCombatFacts>(), deck)).Message, Does.Contain("영역"));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
        }

        internal static HaboobGimmickState Cloud(ConfiguredMovementBattle battle) =>
            battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.OfType<HaboobGimmickState>().Single();

        internal static ConfiguredMovementBattle Create(HaboobGimmickSpec spec = null, bool enterCombat = true) =>
            new(ConfiguredMovementBattle.Config, new Source(new StageGimmickSpec[] { spec ?? new HaboobGimmickSpec() }), enterCombat: enterCombat);

        internal static CombatCommandInput EndInput(ConfiguredMovementBattle battle)
        {
            var b = battle.Buffers;
            int number = b.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            return battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.EndPlayerTurn,
                default, default, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
        }

        internal sealed class Source : IBattleInitializationSource
        {
            private readonly StageGimmickSpec[] _specs;
            private readonly CellCoord[] _rails;
            private readonly bool _tornado;
            internal Source(StageGimmickSpec[] specs, CellCoord[] rails = null, bool tornado = false)
            {
                _specs = specs;
                _rails = rails ?? Array.Empty<CellCoord>();
                _tornado = tornado;
            }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var s = d.Stage;
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerZone, s.TimelineSeed,
                    s.InitialEnemyPlacements.ToArray(), _rails, _tornado ? _specs.Concat(new StageGimmickSpec[] {
                        new TornadoGimmickSpec(new[] { new TornadoMemberSpec(301, new[] { Owner.Player, Owner.Enemy }, new CellCoord(6, 4),
                            firstActivationAfterControllerTurns: 1, repeatIntervalControllerTurns: 1) }) }).ToArray() : _specs, s.InitialEnemyStructurePlacements.ToArray());
                var environment = d.Environment;
                var bindings = d.EnvironmentBindings;
                return new BattleInitialization(stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures, d.Categories,
                    d.AttackProfiles, environment, bindings, d.ObstaclePlacements, d.WallBlocking,
                    d.ObjectiveKind, d.ItemDefinitions, d.Progression, d.InitialRandomTornadoPlacements, d.ClearRewards, d.TraitGrowth);
            }
        }
    }
}
