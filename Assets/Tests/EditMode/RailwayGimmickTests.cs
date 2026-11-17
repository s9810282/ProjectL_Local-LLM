using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Tests.Core
{
    public sealed class RailwayGimmickTests
    {
        [TestCase(1, 1, 0, false)]
        [TestCase(1, 1, 1, true)]
        [TestCase(1, 1, 2, true)]
        [TestCase(5, 5, 4, false)]
        [TestCase(5, 5, 5, true)]
        [TestCase(5, 5, 6, false)]
        [TestCase(5, 5, 10, true)]
        [TestCase(2, 3, 1, false)]
        [TestCase(2, 3, 2, true)]
        [TestCase(2, 3, 3, false)]
        [TestCase(2, 3, 5, true)]
        public void DueUsesCompletedControllerEnds(int first, int repeat, long completed, bool due)
        {
            var input = Input(completed, new[] { CellCoord.Zero }, first, repeat);
            var result = CombatGimmickCalculator.TryAdvance(input);
            Assert.That(result != null, Is.EqualTo(due));
            Assert.That(input.State.Turn.CompletedControllerTurnCount, Is.EqualTo(completed));
            if (!due) return;
            Assert.That(result.Events.OfType<CombatGimmickEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.False);
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(completed));
        }

        [Test]
        public void BothOwnersDieOnce_ContiguousBeforeSettlement_WithNoCommandActorCredit()
        {
            var input = Input(5, new[] { new CellCoord(1, 3), new CellCoord(4, 3) });
            var result = CombatGimmickCalculator.TryAdvance(input);
            var activation = (RailwayGimmickActivationSnapshot)((CombatGimmickEvent)result.Events[0]).Activation;
            var ids = activation.KilledUnitIds.ToArray();
            Assert.That(ids, Is.EqualTo(new[] { new EntityId(1), new EntityId(2) }));
            for (int index = 0; index < ids.Length; index++)
            {
                var death = (CombatCharacterEvent)result.Events[index + 1];
                Assert.That(death.Kind, Is.EqualTo(CombatEventKind.Died));
                Assert.That(death.Final.EntityId, Is.EqualTo(ids[index]));
                Assert.That(death.Expected.Health.IsDead, Is.False);
                Assert.That(death.Final.Health.IsDead, Is.True);
                Assert.That(death.DamageOrigins, Is.Empty);
            }
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
            Assert.That(result.RewardSettlement.KillCount, Is.Zero);
            Assert.That(result.RewardSettlement.KillGold, Is.Zero);
            Assert.That(result.Input, Is.SameAs(input));
            // 영혼 생성은 현재 공통 정책에서 보류 중이다. 기차만 켜면서 그 정책을 복원하지 않는다.
            Assert.That(result.Field.Final.SameValues(input.Souls), Is.True);
            Assert.That(input.Characters.All(c => !c.Health.IsDead), Is.True);
        }

        [TestCase(false, true, StageOutcome.Defeat)]
        [TestCase(true, false, StageOutcome.Victory)]
        [TestCase(true, true, StageOutcome.Defeat)]
        public void CommonOutcomeKeepsMainDeathPriority(bool hitBoss, bool hitMain, StageOutcome expected)
        {
            var cells = new[] { hitBoss ? new CellCoord(4, 3) : CellCoord.Zero,
                hitMain ? new CellCoord(7, 7) : new CellCoord(0, 1) };
            var input = Input(5, cells);
            var state = new CombatCommandState(input.State.BoardWidth,input.State.BoardHeight,input.State.Phase,input.State.Structures,input.State.Turn,objective: BattleObjective.DesignatedBoss(new EntityId(2)));
            input = input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var result = CombatGimmickCalculator.TryAdvance(input);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(expected));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void InvalidIntervalsAreRejected(int first, int repeat) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new RailwayGimmickSpec(first, repeat));

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InvalidRailsFailEvenBeforeDue(int invalid)
        {
            var cells = invalid == 0 ? Array.Empty<CellCoord>() : invalid == 1
                ? new[] { CellCoord.Zero, CellCoord.Zero } : new[] { new CellCoord(8, 0) };
            Assert.Throws<InvalidOperationException>(() => CombatGimmickCalculator.TryAdvance(Input(1, cells)));
        }

        [Test]
        public void PlayerEndPublishesTurnThenRailwayThenDeathsAtTheNewCount()
        {
            var input = CombatCommandTestData.FreshCommand(Input(4, new[] { new CellCoord(1, 3) }), CombatCommandKind.EndPlayerTurn);
            var result = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(5));
            Assert.That(result.Events[0].Kind, Is.EqualTo(CombatEventKind.PlayerTurnEnded));
            Assert.That(result.Events[1], Is.TypeOf<CombatGimmickEvent>());
            Assert.That(result.Events[2].Kind, Is.EqualTo(CombatEventKind.Died));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
        }

        private static CombatCommandInput Input(long count, CellCoord[] cells, int first = 5, int repeat = 5)
        {
            var source = CombatCommandTestData.Input();
            var turn = source.State.Turn;
            var state = source.State.With(turn: new CombatTurnFacts(turn.StagePhase,
                turn.ControllerPhase, turn.ControllerTurnSequence, turn.CompletedPlayerTurnCount,
                turn.EnemyOrigins, turn.Summons, count));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0, cells,
                new StageGimmickSpec[] { new RailwayGimmickSpec(first, repeat) });
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: battle, battleInstanceId: source.BattleInstanceId,
                expectedBattleInstanceId: source.ExpectedBattleInstanceId, expectedControllerTurnSequence: source.ExpectedControllerTurnSequence);
        }
    }
}
