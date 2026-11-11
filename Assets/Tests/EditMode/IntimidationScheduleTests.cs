using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class IntimidationScheduleTests
    {
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(200)]
        public void AttackReduction_DoesNotChangePriorityControllerStateOrEligibility(int priority)
        {
            var input = Scheduled(priority: priority);
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            var changed = IntimidationAttackTests.Mark(victim, 20);
            var result = Finish(input, changed);
            Assert.That(CombatEffectiveStatsCalculator.Priority(changed), Is.EqualTo(priority));
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
            Assert.That(changed.ActionUse, Is.EqualTo(victim.ActionUse));
            Assert.That(CombatEffectiveStatsCalculator.AttackPower(changed),
                Is.EqualTo(CombatEffectiveStatsCalculator.AttackPower(victim)));
            var next = CombatCommandComposition.Project(result);
            var repeated = Finish(next, changed);
            Assert.That(repeated.FinalState.SameValues(next.State), Is.True);
        }

        [Test]
        public void RealAttack_PublishesAttackReductionAndPreservesTurnState()
        {
            var input = Scheduled();
            var result = IntimidationAttackTests.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var victim = result.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final;
            Assert.That(victim.Debuffs.Intimidation.AttackReductionPercent, Is.EqualTo(20));
            Assert.That(result.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(victim.ActionUse, Is.EqualTo(input.Characters.Single(c => c.EntityId.Value == 2).ActionUse));
            var repeated = CombatCommandCalculator.Calculate(CombatCommandComposition.Project(result), Array.Empty<IMovementTileRule>());
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.Reason, Is.EqualTo("ActionAlreadyUsed"));
            Assert.That(repeated.Events, Is.Empty);
        }

        [Test]
        public void FlagPickup_DoesNotChangeDebuffOrControllerFacts()
        {
            var input = Scheduled();
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            var flagged = HeldEnvironmentEffects.AcquireFlag(victim, FlagLifecycleTests.Flag(bonus: 7));
            Assert.That(flagged, Is.SameAs(victim));
            Assert.That(Finish(input, flagged).FinalState.SameValues(input.State), Is.True);
        }

        [Test]
        public void TerminalCleanup_DoesNotStartAnotherController()
        {
            var input = Scheduled();
            var deadMain = CombatCommandCalculator.Hurt(input.Characters.Single(c => c.EntityId.Value == 10), 100);
            var result = Finish(input, deadMain);
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
        }

        internal static CombatCommandInput Scheduled(int priority = 10)
        {
            var input = IntimidationAttackTests.Caster();
            return IntimidationAttackTests.Replace(input, c => c.EntityId.Value == 2
                ? IntimidationAttackTests.Mark(WithPriority(c, priority), 20) : c);
        }

        private static CharacterCombatFacts WithPriority(CharacterCombatFacts c, int priority) =>
            c.With(stats: c.Stats.With(priority: priority));

        internal static CombatCommandTransition Finish(CombatCommandInput input, CharacterCombatFacts changed, CombatCommandState state = null)
        {
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[changed.EntityId] = changed;
            return CombatCommandCalculator.Finish(input, projected, input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, input.State.ThornFields.ToDictionary(f => f.Cell), input.State.Structures.ToArray(),
                new List<long>(), new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null, proposedState: state);
        }
    }
}
