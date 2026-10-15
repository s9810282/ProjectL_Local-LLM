using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatResultContractTests
    {
        [Test]
        public void CommandResult_DistinguishesAcceptanceFromRuleRejectionWithInputNumberGaps()
        {
            var accepted = new CombatResult(1, CombatProcessingKind.Command, 3, true, "");
            var rejected = new CombatResult(2, CombatProcessingKind.Command, 7, false,
                "Attack.InsufficientActionCost");

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Reason, Is.Empty);
            Assert.That(accepted.CommandNumber, Is.EqualTo(3));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Sequence, Is.EqualTo(2));
            Assert.That(rejected.Kind, Is.EqualTo(CombatProcessingKind.Command));
            Assert.That(rejected.CommandNumber, Is.EqualTo(7));
            Assert.That(rejected.Reason, Is.EqualTo("Attack.InsufficientActionCost"));
        }

        [TestCase(true, "")]
        [TestCase(false, "Timeline.NoActiveUnit")]
        public void AutomaticResult_HasNoOriginatingCommand(bool accepted, string reason)
        {
            var result = new CombatResult(4, CombatProcessingKind.Advance, 0, accepted, reason);
            Assert.That(result.CommandNumber, Is.Zero);
            Assert.That(result.Kind, Is.EqualTo(CombatProcessingKind.Advance));
            Assert.That(result.Accepted, Is.EqualTo(accepted));
        }

        [TestCase(0, CombatProcessingKind.Command, 1)]
        [TestCase(-1, CombatProcessingKind.Command, 1)]
        [TestCase(1, CombatProcessingKind.Command, 0)]
        [TestCase(1, CombatProcessingKind.Command, -1)]
        [TestCase(1, CombatProcessingKind.Advance, 1)]
        [TestCase(1, CombatProcessingKind.Advance, -1)]
        [TestCase(1, (CombatProcessingKind)99, 0)]
        public void Result_RejectsInvalidProcessingIdentity(int sequence, CombatProcessingKind kind,
            int commandNumber)
        {
            Assert.That(() => new CombatResult(sequence, kind, commandNumber, true, ""),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(true, "Attack.Rejected")]
        [TestCase(false, "")]
        [TestCase(false, " ")]
        [TestCase(false, null)]
        public void Result_RejectsInconsistentAcceptanceAndReason(bool accepted, string reason)
        {
            Assert.That(() => new CombatResult(1, CombatProcessingKind.Command, 1, accepted, reason),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Result_ExposesOnlyImmutableMetadata()
        {
            Assert.That(typeof(CombatResult).IsSealed, Is.True);
            foreach (var property in typeof(CombatResult).GetProperties())
                Assert.That(property.GetSetMethod(true), Is.Null, property.Name);
            Assert.That(typeof(CombatResult).GetFields(), Is.Empty);
        }
    }
}
