using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TornadoGimmickPublicationTests
    {
        [Test]
        public void ExplicitT2EntryPublishesOnceAndRetryCannotDuplicateStateOrRandomness()
        {
            using var battle = Create();
            Install(battle);
            var input = Input(battle);
            var result = ResolveEntry(input);
            var before = battle.Actor.Anchor;
            battle.Buffers.BattleWriteSink.Apply(result);
            Assert.That(battle.Actor.Anchor, Is.Not.EqualTo(before));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Gimmicks.SameValues(input.State.Gimmicks), Is.True);
            var stores = battle.CaptureStores();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            // 처리한 명령 번호의 재게시에는 기존 거래 경계의 멱등 정책을 따른다.
            battle.Buffers.BattleWriteSink.Apply(result);
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        [TestCase("stale-input")]
        [TestCase("stale-rng")]
        [TestCase("forged-group")]
        [TestCase("missing-origin")]
        public void InvalidProposalFailsBeforeInstallingCharacterStormOrRandomFacts(string corruption)
        {
            using var battle = Create();
            Install(battle);
            var input = Input(battle);
            var result = ResolveEntry(input);
            var group = (TornadoGimmickState)input.State.Gimmicks.Entries.Single();
            var old = group.Members.Single();
            var changed = input.State.Gimmicks.Replace(new TornadoGimmickState(group.Id, new[] {
                new TornadoMemberState(old.SourceId, new CellCoord(2, 1), 1, old.Phase, old.NextBoundaryCount, null) }));
            if (corruption == "stale-input")
                result = ResolveEntry(input.WithWorld(input.State.With(gimmicks: changed), input.Characters, input.Main,
                    input.SoulCounts, input.Souls, input.EnvironmentBindings));
            if (corruption == "stale-rng")
            {
                var random = input.State.EnvironmentProgress;
                result = ResolveEntry(input.WithWorld(input.State.With(environmentProgress: new EnvironmentProgressFacts(
                    random.NextObjectEntryOrdinal, random.NextSourceId, random.RandomState + 1)), input.Characters, input.Main,
                    input.SoulCounts, input.Souls, input.EnvironmentBindings));
            }
            if (corruption == "forged-group")
                result = new CombatCommandTransition(input, true, "", result.Characters, result.Field,
                    result.FinalState.With(gimmicks: changed), result.Outcome, events: result.Events,
                    pathMovement: result.PathMovement, actionUseSettlement: result.ActionUseSettlement);
            if (corruption == "missing-origin")
            {
                var events = result.Events.Select(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Relocated
                    ? new CombatCharacterEvent(c.Kind, c.Expected, c.Final, c.DamageOrigins, c.TornadoSourceId, c.DamageCause, c.MovementCause) : e).ToArray();
                result = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState, result.Outcome,
                    events: events, pathMovement: result.PathMovement, actionUseSettlement: result.ActionUseSettlement);
            }
            var stores = battle.CaptureStores();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var hp = battle.Actor.Health;
            var anchor = battle.Actor.Anchor;
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(result));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(battle.Actor.Health, Is.EqualTo(hp));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(anchor));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)), enterCombat: false);

        private static void Install(ConfiguredMovementBattle battle)
        {
            var b = battle.Buffers;
            var current = b.CombatStateReadSink.Current;
            var spec = new TornadoGimmickSpec(new[] { new TornadoMemberSpec(901, new[] { Owner.Player, Owner.Enemy }, new CellCoord(1, 1)) });
            var facts = new StageGimmickStateSet(new[] { new TornadoGimmickState(new StageGimmickId(1), new[] {
                new TornadoMemberState(901, new CellCoord(1, 1), 1, TornadoGimmickPhase.Cooldown, 1, null) }) });
            var combat = new CombatPreparation(current.BoardWidth, current.BoardHeight, current.ReserveCapacity,
                current.TimelineSeed, current.Phase, current.DrawOrder, current.DrawCursor, current.PlayerZone, current.TraitSettings,
                current.RailCells.ToArray(), new[] { spec }, current.Environment, current.EnvironmentBindings,
                current.ObstacleContents, current.CommandState.WallBlocking);
            var preparation = new BattlePreparation(b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(),
                combat, current.Structures, commandState: current.CommandState.With(gimmicks: facts),
                objective: current.CommandState.Objective, membership: current.Membership, rosterRevision: b.CharacterReadSink.RosterRevision);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(preparation);
            var assembler = battle.Publication().Assembler;
            int enterNumber = b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
            var enter = assembler.Assemble(new CombatActionRequest(enterNumber, CombatCommandKind.EnterCombat,
                battle.Actor.EntityId, default, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            b.BattleWriteSink.Apply(CombatCommandCalculator.Calculate(enter, Array.Empty<IMovementTileRule>()));
            var begin = assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.StartControllerTurn,
                battle.Actor.EntityId, default, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            b.BattleWriteSink.Apply(CombatCommandCalculator.Advance(begin, Array.Empty<IMovementTileRule>()));
        }

        private static CombatCommandInput Input(ConfiguredMovementBattle battle)
        {
            var b = battle.Buffers;
            int number = b.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            return battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move,
                battle.Actor.EntityId, default, path: new MovementPath(new[] { battle.Actor.Anchor.Value, new CellCoord(1, 1) }),
                expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
        }

        private static CombatCommandTransition ResolveEntry(CombatCommandInput input)
        {
            // 운영 호출 전환 전이므로 준비된 실제 Buffer 입력에 내부 투영·접촉 계산기만 명시적으로 호출한다.
            var type = typeof(CombatMovementCalculator).Assembly.GetType(typeof(CombatMovementCalculator).Namespace + ".MovementProjection", true);
            object projection = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { input, false }, null);
            type.GetMethod("BeginOrdinaryMove", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projection, null);
            type.GetMethod("Enter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projection, new object[] { new CellCoord(1, 1) });
            typeof(TornadoCalculator).GetMethod("ResolveGroupedEntry", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { projection, input.ActorId, new CellCoord(1, 1), Array.Empty<IMovementTileRule>() });
            var result = (CombatCommandTransition)type.GetMethod("Transition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projection, null);
            return MovementUndoPolicy.Reconcile(result);
        }
    }
}
