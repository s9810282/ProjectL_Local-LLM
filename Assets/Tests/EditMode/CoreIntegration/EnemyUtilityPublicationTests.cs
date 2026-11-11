using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyUtilityPublicationTests
    {
        [TestCase(1)]
        [TestCase(37)]
        [TestCase(1000)]
        public void UtilityDrainAndPreparedPublishTheSameSingleResult(int clockStep)
        {
            using var prepared = new EnemyAdvancePreparationTests.Fixture { TickStep = clockStep };
            using var sync = new EnemyDecisionSourceStampTests.SourceFixture();
            var buffers = prepared.Source.Battle.Buffers;
            var direct = sync.Battle.Buffers;
            buffers.CombatViewWriteSink.Submit(prepared.Source.Battle.EndPlayerTurn());
            direct.CombatViewWriteSink.Submit(sync.Battle.EndPlayerTurn());
            var pending = buffers.CombatViewReadSink.Commands.ToArray();
            var snapshot = prepared.Source.Battle.CaptureStores();
            long sequence = buffers.CombatStateReadSink.LatestResultSequence;
            int handled = buffers.CombatStateReadSink.HandledCommandNumber;

            prepared.Ready();
            Assert.That(prepared.Source.Battle.CaptureStores(), Is.EqualTo(snapshot));
            var input = sync.Assembler.Assemble(sync.Request);
            var transition = CombatCommandCalculator.AdvanceUtility(input, MovementRules());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(sync.Battle.Publication().Publisher.Publish(transition), Is.True);

            Assert.That(prepared.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(buffers.CombatViewReadSink.Commands, Is.EqualTo(pending));
            EnemyAdvancePreparationTests.AssertBattleValues(buffers, direct);
            buffers.CombatResultReadSink.TryGetLatest(out var actual);
            direct.CombatResultReadSink.TryGetLatest(out var expected);
            EnemyAdvancePreparationTests.AssertValues(actual, expected, "utility result");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadyUtilityCanBeCancelledOrInvalidatedWithoutPublication(bool cancel)
        {
            using var prepared = new EnemyAdvancePreparationTests.Fixture();
            var buffers = prepared.Source.Battle.Buffers;
            prepared.Ready();
            if (cancel) prepared.Service.CancelPreparation();
            else buffers.CurrencyWriteSink.SetAmount(999);
            var expected = prepared.Source.Battle.CaptureStores();
            Assert.That(prepared.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Stale));
            Assert.That(prepared.Source.Battle.CaptureStores(), Is.EqualTo(expected));
            Assert.That(prepared.Service.HasPreparation, Is.False);
        }

        private static IMovementTileRule[] MovementRules() => new IMovementTileRule[]
        {
            new ThornMovementTileRule(), new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule()
        };
    }
}
