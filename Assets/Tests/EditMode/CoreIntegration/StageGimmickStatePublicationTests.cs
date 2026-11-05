using System;
using System.Linq;
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
    public sealed class StageGimmickStatePublicationTests
    {
        [Test]
        public void UnrelatedCommandPreservesWarningFacts()
        {
            using var battle = Create();
            InstallFacts(battle);
            var facts = battle.Buffers.CombatStateReadSink.Current.CommandState.Gimmicks;
            Apply(battle, Input(battle, CombatCommandKind.EnterCombat));
            Apply(battle, Input(battle, CombatCommandKind.StartControllerTurn, advance: true), advance: true);
            var path = new MovementPath(new[] { battle.Actor.Anchor.Value, new CellCoord(1, 1) });
            var move = Input(battle, CombatCommandKind.Move, path: path);
            var random = move.State.EnvironmentProgress.RandomState;
            CombatMovementCalculator.Preview(move, Array.Empty<IMovementTileRule>());
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Gimmicks.SameValues(facts), Is.True);
            Apply(battle, move);
            int token = battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single().MoveCommandNumber;
            Apply(battle, Input(battle, CombatCommandKind.UndoMove, token: token));
            var attack = Input(battle, CombatCommandKind.Attack, aim: AttackAim.ForCell(new CellCoord(0, 3)));
            CombatCommandCalculator.Preview(attack, Array.Empty<IMovementTileRule>());
            Apply(battle, attack);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Gimmicks.SameValues(facts), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState, Is.EqualTo(random));
            Assert.That(((RockfallGimmickState)facts.Get(new StageGimmickId(1))).WarnedCells, Is.EqualTo(new[] { new CellCoord(1, 1) }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleGimmickStateRejectsWithoutPartialCharacterWrites(bool forgedFinal)
        {
            using var battle = Create();
            InstallFacts(battle);
            var input = Input(battle, CombatCommandKind.ReturnUnit);
            var changedFacts = input.State.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(1),
                RockfallGimmickPhase.Warning, 9, new[] { new CellCoord(2, 1) }));
            if (!forgedFinal)
                input = input.WithWorld(input.State.With(gimmicks: changedFacts), input.Characters, input.Main,
                    input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Characters.Characters.Count, Is.GreaterThan(0));
            if (forgedFinal)
                transition = new CombatCommandTransition(input, true, "", transition.Characters, transition.Field,
                    transition.FinalState.With(gimmicks: changedFacts), transition.Outcome, events: transition.Events);
            var before = battle.CaptureStores();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var hp = battle.Actor.Health;
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Actor.Health, Is.EqualTo(hp));
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)), enterCombat: false);

        private static void InstallFacts(ConfiguredMovementBattle battle)
        {
            var b = battle.Buffers;
            var current = b.CombatStateReadSink.Current;
            var definitions = new StageGimmickSpec[] { new RockfallGimmickSpec(), new HaboobGimmickSpec() };
            var facts = new StageGimmickStateSet(new StageGimmickState[] {
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, new[] { new CellCoord(1, 1) }),
                new HaboobGimmickState(new StageGimmickId(2), HaboobGimmickPhase.Active, 2, new[] { new CellCoord(2, 2) }) });
            var combat = new CombatPreparation(current.BoardWidth, current.BoardHeight, current.ReserveCapacity,
                current.TimelineSeed, CombatFlowPhase.PlayerInput, current.DrawOrder, current.DrawCursor, current.PlayerZone, current.TraitSettings,
                current.RailCells.ToArray(), definitions, current.Environment, current.EnvironmentBindings,
                current.ObstacleContents, current.CommandState.WallBlocking);
            var preparation = new BattlePreparation(b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(),
                combat, current.Structures, commandState: current.CommandState.With(gimmicks: facts),
                objective: current.CommandState.Objective, membership: current.Membership, rosterRevision: b.CharacterReadSink.RosterRevision);
            b.BattleWriteSink.EndBattle();
            // 기능 계산기 활성화 전의 저장 계약 검사다. 실제 다중 Buffer 준비·설치를 사용한다.
            b.BattleWriteSink.PrepareBattle(preparation);
        }

        private static CombatCommandInput Input(ConfiguredMovementBattle battle, CombatCommandKind kind,
            MovementPath path = null, int token = 0, AttackAim aim = default, bool advance = false)
        {
            var b = battle.Buffers;
            int number = advance ? 0 : b.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            return battle.Publication().Assembler.Assemble(new CombatActionRequest(number, kind, battle.Actor.EntityId, aim,
                path: path, expectedMoveCommandNumber: token, processingKind: advance ? CombatProcessingKind.Advance : CombatProcessingKind.Command,
                expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
        }

        private static void Apply(ConfiguredMovementBattle battle, CombatCommandInput input, bool advance = false)
        {
            var transition = advance ? CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>()) :
                CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            battle.Buffers.BattleWriteSink.Apply(transition);
        }
    }
}
