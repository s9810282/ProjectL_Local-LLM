using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class NewGimmickBoundaryTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void EndBoundaryIncrementsOnceAcrossStages(Owner side)
        {
            var input = Input(side, CellCoord.Zero);
            long beforeCount = input.State.Turn.CompletedControllerTurnCount;
            var final = End(input, side);

            Assert.That(final.Accepted, Is.True, final.Reason);
            Assert.That(final.Outcome.IsStageEnded, Is.False);
            Assert.That(final.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(beforeCount + 1));
            Assert.That(GimmickOrder(final), Is.EqualTo(new[]
            {
                typeof(CombatTornadoGimmickEvent), typeof(CombatGimmickEvent),
                typeof(CombatHaboobEvent), typeof(CombatRockfallEvent)
            }));

            var projected = CombatCommandComposition.Project(final);
            var repeated = End(projected, side);
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(beforeCount + 1));
            Assert.Throws<InvalidOperationException>(() => CombatGimmickCalculator.TryAdvance(projected,
                new StageGimmickBoundary(side, beforeCount + 2), Rules));
        }

        [TestCase(StageGimmickBoundaryPhase.Expiry)]
        [TestCase(StageGimmickBoundaryPhase.EndOccupancy)]
        [TestCase(StageGimmickBoundaryPhase.AfterCleanup)]
        [TestCase(StageGimmickBoundaryPhase.Warning)]
        public void LegacyRulesRunOnlyInScheduled(StageGimmickBoundaryPhase phase)
        {
            var input = Input(Owner.Player, CellCoord.Zero);
            var ended = CombatCommandComposition.Project(AcceptedPlayerEnd(input));
            var scheduled = new StageGimmickBoundary(Owner.Player, 1);

            Assert.That(CombatGimmickCalculator.TryAdvance(ended, new StageGimmickBoundary(Owner.Player, 1, phase), Rules), Is.Null);
            Assert.That(scheduled.Phase, Is.EqualTo(StageGimmickBoundaryPhase.Scheduled));
            Assert.That(GimmickOrder(CombatGimmickCalculator.TryAdvance(ended, scheduled, Rules)).Length, Is.EqualTo(4));
        }

        [Test]
        public void TerminalStageSkipsRemainingStages()
        {
            var input = Input(Owner.Player, new CellCoord(7, 7));
            var final = End(input, Owner.Player);

            Assert.That(final.Outcome.IsStageEnded, Is.True);
            Assert.That(final.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            Assert.That(GimmickOrder(final), Is.EqualTo(new[] { typeof(CombatTornadoGimmickEvent), typeof(CombatGimmickEvent) }));
            Assert.That(final.FinalState.Gimmicks.Get(new StageGimmickId(1)), Is.SameAs(input.State.Gimmicks.Get(new StageGimmickId(1))));
            Assert.That(final.FinalState.Gimmicks.Get(new StageGimmickId(4)), Is.SameAs(input.State.Gimmicks.Get(new StageGimmickId(4))));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EndStagesSeeProjectedRailwayAndCleanupOnlyWhileBattleContinues(bool terminal)
        {
            var input = Input(Owner.Player, terminal ? new CellCoord(7, 7) : new CellCoord(4, 3));
            var characters = input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, hp: 60, traits: new[] { new RegenerationTraitStatusSnapshot(10) })
                : c).ToArray();
            var ended = AcceptedPlayerEnd(CombatCommandTestData.With(input, characters: characters));
            var probe = new BoundaryProbe();
            var gimmicks = new StageGimmickComposition(new IStageGimmickRule[]
            {
                probe, new RailwayGimmickRule(), new TornadoGimmickRule(Rules), new RockfallGimmickRule()
            });

            var final = CombatTurnCalculator.ResolveEndBoundary(ended, Owner.Player, gimmicks);

            Assert.That(final.Outcome.IsStageEnded, Is.EqualTo(terminal));
            if (terminal)
            {
                Assert.That(probe.Observed, Is.EqualTo(new[]
                {
                    (StageGimmickBoundaryPhase.Expiry, 1L, false, 60),
                    (StageGimmickBoundaryPhase.Scheduled, 1L, false, 60)
                }));
                Assert.That(CombatCommandComposition.Project(final).Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth,
                    Is.EqualTo(60));
                return;
            }

            Assert.That(probe.Observed, Is.EqualTo(new[]
            {
                (StageGimmickBoundaryPhase.Expiry, 1L, false, 60),
                (StageGimmickBoundaryPhase.Scheduled, 1L, false, 60),
                (StageGimmickBoundaryPhase.EndOccupancy, 1L, true, 60),
                (StageGimmickBoundaryPhase.AfterCleanup, 1L, true, 70),
                (StageGimmickBoundaryPhase.Warning, 1L, true, 70)
            }));
        }

        [Test]
        public void LegacyInitializationKeepsStateIdsAndRandomSequence()
        {
            var before = SeededState();
            var initialized = StageGimmickComposition.Initialize(before, Definitions());

            Assert.That(initialized.Gimmicks.Entries.Select(e => e.Id.Value), Is.EqualTo(new[] { 1, 3, 4 }));
            var haboob = (HaboobGimmickState)initialized.Gimmicks.Get(new StageGimmickId(1));
            Assert.That(haboob.Phase, Is.EqualTo(HaboobGimmickPhase.Active));
            Assert.That(haboob.Cells, Is.EqualTo(new[] { new CellCoord(4, 6), new CellCoord(5, 6) }));
            Assert.That(haboob.NextBoundaryCount, Is.EqualTo(1));
            var tornado = ((TornadoGimmickState)initialized.Gimmicks.Get(new StageGimmickId(3))).Members.Single();
            Assert.That(tornado.Phase, Is.EqualTo(TornadoGimmickPhase.Cooldown));
            Assert.That(tornado.Center, Is.EqualTo(new CellCoord(6, 1)));
            Assert.That(tornado.HorizontalSign, Is.EqualTo(-1));
            Assert.That(tornado.LockedDirection, Is.Null);
            Assert.That(tornado.NextBoundaryCount, Is.EqualTo(1));
            var rockfall = (RockfallGimmickState)initialized.Gimmicks.Get(new StageGimmickId(4));
            Assert.That(rockfall.Phase, Is.EqualTo(RockfallGimmickPhase.Cooldown));
            Assert.That(rockfall.WarnedCells, Is.Empty);
            Assert.That(rockfall.NextBoundaryCount, Is.EqualTo(1));
            Assert.That(initialized.EnvironmentProgress.RandomState, Is.EqualTo(166005888u));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidInitializationLeavesOriginalStateUntouched(bool duplicate)
        {
            var before = SeededState();
            StageGimmickSpec invalid = duplicate
                ? new HaboobGimmickSpec()
                : new TornadoGimmickSpec(new[] { new TornadoMemberSpec(31, new[] { Owner.Player }, sideLength: 9) });
            var definitions = new[] { Definitions()[0], invalid };

            Assert.Throws<InvalidOperationException>(() => StageGimmickComposition.Initialize(before, definitions));
            Assert.That(before.Gimmicks.Entries, Is.Empty);
            Assert.That(before.EnvironmentProgress.RandomState, Is.EqualTo(123u));
        }

        private static CombatCommandState SeededState() => CombatCommandTestData.Input().State.With(
            environmentProgress: new EnvironmentProgressFacts(1, 1, 123u));

        private static StageGimmickSpec[] Definitions() => new StageGimmickSpec[]
        {
            new HaboobGimmickSpec(2, 1, new[] { CellCoord.Zero, new CellCoord(1, 0) }),
            new RailwayGimmickSpec(1, 2),
            new TornadoGimmickSpec(new[] { new TornadoMemberSpec(31, new[] { Owner.Player, Owner.Enemy }) }),
            new RockfallGimmickSpec(cooldownControllerTurns: 1)
        };

        private static CombatCommandInput Input(Owner side, CellCoord rail)
        {
            var source = CombatCommandTestData.Input();
            var definitions = Definitions();
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0, new[] { rail }, definitions);
            bool player = side == Owner.Player;
            var state = StageGimmickComposition.Initialize(SeededState(), definitions).With(
                phase: player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: player ? CombatControllerPhase.Player : CombatControllerPhase.Enemy,
                    controllerTurnSequence: 1));
            return new CombatCommandInput(1, player ? CombatCommandKind.EndPlayerTurn : CombatCommandKind.CompleteEnemyTurn,
                source.ActorId, default, state, source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, battle: battle, battleInstanceId: 1, expectedBattleInstanceId: 1,
                expectedControllerTurnSequence: 1);
        }

        private static CombatCommandTransition End(CombatCommandInput input, Owner side) => side == Owner.Player
            ? CombatTurnCalculator.EndPlayerTurn(input, Rules)
            : CombatTurnCalculator.EndEnemyPhase(input, Rules);

        private static CombatCommandTransition AcceptedPlayerEnd(CombatCommandInput input)
        {
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.StartingEnemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: 1, completedControllerTurnCount: 1);
            return CombatTurnCalculator.Changed(input, input.Characters.ToDictionary(c => c.EntityId),
                input.State.With(turn: turn, phase: CombatFlowPhase.Automatic));
        }

        private static Type[] GimmickOrder(CombatCommandTransition transition) => transition.Events
            .Where(e => e is CombatTornadoGimmickEvent or CombatGimmickEvent or CombatHaboobEvent or CombatRockfallEvent)
            .Select(e => e.GetType()).ToArray();

        private sealed class BoundaryProbe : IStageGimmickRule
        {
            public StageGimmickKey Key => StageGimmickKey.Haboob;
            internal List<(StageGimmickBoundaryPhase Phase, long Count, bool EnemyDead, int PlayerHealth)> Observed { get; } = new();

            public bool RunsAt(StageGimmickBoundaryPhase phase) => true;

            public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
                StageGimmickId id, ref uint randomState) => throw new NotSupportedException();

            public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
                StageGimmickId id, long lastBoundaryCount)
            {
                yield break;
            }

            public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
                StageGimmickId id, StageGimmickBoundary? boundary)
            {
                var end = boundary.Value;
                Observed.Add((end.Phase, input.State.Turn.CompletedControllerTurnCount,
                    input.Characters.Single(c => c.EntityId == new EntityId(2)).Health.IsDead,
                    input.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth));
                return null;
            }
        }
    }
}
