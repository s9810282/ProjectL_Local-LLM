using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.Core
{
    public sealed class PowderBlossomParityTests
    {
        [Test]
        public void CompleteLocalCalculationMatchesCommandAndLeavesOriginalFactsIntact()
        {
            var input = PowderBlossomFixture.Input(PlayerSkillUpgradeKey.Bloom, enemies: new[]
            {
                PowderBlossomFixture.Enemy(2, 4, 3, 5), PowderBlossomFixture.Enemy(3, 6, 6, 10),
                PowderBlossomFixture.Enemy(4, 7, 6, 100)
            }, trait: new KillChainAttackTraitSpec());
            string Encode(object value) => AttackPreviewParityTests.Serialize(value, new HashSet<string>());
            string InputFacts() => Encode(new object[] { input.State, input.Characters, input.Owned, input.Souls, input.PlayerSkillRequest });
            string OutcomeFacts(CombatCommandTransition result) => Encode(new object[] { result.Accepted, result.Characters,
                result.FinalState, result.Field, result.Outcome, result.PlayerSkill, result.Events, result.HealthExecutions, result.HealthClaims });
            var before = InputFacts();
            var local = PlayerSkillCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var command = PowderBlossomFixture.Calculate(input);
            Assert.That(command.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(input.State.EnvironmentProgress.RandomState));
            Assert.That(OutcomeFacts(local), Is.EqualTo(OutcomeFacts(command)));
            Assert.That(InputFacts(), Is.EqualTo(before));
            Assert.That(command.HealthClaims.All(c => command.Events.Any(e => ReferenceEquals(e, c.Event))), Is.True);
        }

        [TestCase(1, false, PlayerSkillFailureReason.SourceChanged)]
        [TestCase(0, true, PlayerSkillFailureReason.WrongPointCount)]
        public void InvalidIntentDoesNotChangeUsageRngOrGameplay(long revisionDelta, bool points, PlayerSkillFailureReason failure)
        {
            var input = PowderBlossomFixture.Input(revisionDelta: revisionDelta, points: points ? new[] { CellCoord.Zero } : null);
            var output = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(output.Accepted, Is.False);
            Assert.That(output.Reason, Is.EqualTo(failure.ToString()));
            Assert.That(output.FinalState, Is.SameAs(input.State));
            Assert.That(output.Events, Is.Empty);
            Assert.That(output.PlayerSkill, Is.Null);
        }
    }
}
