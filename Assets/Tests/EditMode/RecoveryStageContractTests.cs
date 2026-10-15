using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Run;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryStageContractTests
    {
        [TestCase("mode")]
        [TestCase("percent-low")]
        [TestCase("percent-high")]
        [TestCase("flat-negative")]
        [TestCase("option-id")]
        [TestCase("cost")]
        [TestCase("heal")]
        [TestCase("stage-key")]
        [TestCase("null-options")]
        [TestCase("empty-options")]
        [TestCase("null-option")]
        [TestCase("duplicate-option")]
        public void Specs_RejectMalformedInputs(string invalid)
        {
            RecoveryOptionSpec option = Option(1);
            var key = new StageKey(100);
            TestDelegate create = invalid switch
            {
                "mode" => () => new RecoveryHealSpec((RecoveryHealMode)99, 1),
                "percent-low" => () => new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 0),
                "percent-high" => () => new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 101),
                "flat-negative" => () => new RecoveryHealSpec(RecoveryHealMode.Flat, -1),
                "option-id" => () => new RecoveryOptionSpec(RecoveryOptionId.Empty, 0, option.Heal),
                "cost" => () => new RecoveryOptionSpec(option.Id, -1, option.Heal),
                "heal" => () => new RecoveryOptionSpec(option.Id, 0, null),
                "stage-key" => () => new RecoveryStageSpec(StageKey.Empty, new[] { option }),
                "null-options" => () => new RecoveryStageSpec(key, null),
                "empty-options" => () => new RecoveryStageSpec(key, Array.Empty<RecoveryOptionSpec>()),
                "null-option" => () => new RecoveryStageSpec(key, new RecoveryOptionSpec[] { null }),
                "duplicate-option" => () => new RecoveryStageSpec(key, new[] { option, Option(1) }),
                _ => throw new ArgumentOutOfRangeException(nameof(invalid))
            };

            Assert.That(create, Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void FrozenOptionsAndRetainedSnapshotSurviveSourceAndResolutionChanges()
        {
            RecoveryStageEntryRequest request = Request();
            RecoveryOptionSpec original = Option(1);
            var source = new[] { original };
            var spec = new RecoveryStageSpec(request.StageKey, source);
            var state = new RecoveryStageState(request, spec);
            source[0] = Option(2);

            Assert.That(spec.Options.Length, Is.EqualTo(1));
            Assert.That(state.TryGetOption(original.Id, out RecoveryOptionSpec selected), Is.True);
            Assert.That(selected, Is.SameAs(original));
            Assert.That(state.TryGetOption(source[0].Id, out RecoveryOptionSpec missing), Is.False);
            Assert.That(missing, Is.Null);

            var optionSnapshot = new RecoveryOptionSnapshot(original.Id, original.Cost, 5, true, RecoveryFailureReason.None);
            var snapshots = new[] { optionSnapshot };
            var before = Snapshot(request, snapshots);
            snapshots[0] = new RecoveryOptionSnapshot(source[0].Id, 0, 0, false, RecoveryFailureReason.NoEffect);

            state.ApplyPrevalidated(selected.Id, selected.Cost, 5);

            Assert.That(state.IsResolved, Is.True);
            Assert.That(state.AppliedOptionId, Is.EqualTo(original.Id));
            Assert.That(state.SpentAmount, Is.EqualTo(original.Cost));
            Assert.That(state.HealedAmount, Is.EqualTo(5));
            Assert.That(before.Options.Span[0], Is.SameAs(optionSnapshot));
            Assert.That(before.AppliedOptionId, Is.Null);
            Assert.That(before.IsResolved, Is.False);
            Assert.That(before.MainCharacterHealth, Is.EqualTo(request.Health));
            Assert.That(before.Wallet.Balance, Is.EqualTo(10));

            var after = new RecoveryStageSnapshot(state.RunId, state.EntryId, state.NodeId, state.StageKey,
                request.ExpectedRunRevision + 1, new HealthStateSnapshot(10, 20), new PlayerWalletSnapshot(7),
                new[] { new RecoveryOptionSnapshot(original.Id, original.Cost, 5, false, RecoveryFailureReason.AlreadyResolved) },
                state.AppliedOptionId);
            RecoveryCommandResult success = RecoveryCommandResult.Success(after, alreadyApplied: true);
            Assert.That(success.Succeeded && success.AlreadyApplied, Is.True);
            Assert.That(success.CurrentRecovery.IsResolved, Is.True);
            Assert.That(success.CurrentRecovery.AppliedOptionId, Is.EqualTo(original.Id));
        }

        [TestCase("run")]
        [TestCase("entry")]
        [TestCase("node")]
        [TestCase("stage")]
        [TestCase("revision")]
        [TestCase("default-health")]
        [TestCase("dead-health")]
        public void EntryRequest_RejectsInvalidIdentityRevisionOrHealth(string invalid)
        {
            RecoveryStageEntryRequest valid = Request();
            Assert.That(() => new RecoveryStageEntryRequest(
                invalid == "run" ? RunId.Empty : valid.RunId,
                invalid == "entry" ? StageEntryId.Empty : valid.EntryId,
                invalid == "revision" ? -1 : valid.ExpectedRunRevision,
                invalid == "node" ? StageNodeId.Empty : valid.NodeId,
                invalid == "stage" ? StageKey.Empty : valid.StageKey,
                invalid == "default-health" ? default :
                invalid == "dead-health" ? new HealthStateSnapshot(0, 20) : valid.Health),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void State_RejectsContentForADifferentStage()
        {
            Assert.Throws<ArgumentException>(() => new RecoveryStageState(Request(),
                new RecoveryStageSpec(new StageKey(101), new[] { Option(1) })));
        }

        [Test]
        public void HealSpecs_AcceptPercentageEndpointsAndIgnoreFullValue()
        {
            Assert.DoesNotThrow(() => new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 1));
            Assert.DoesNotThrow(() => new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 100));
            Assert.DoesNotThrow(() => new RecoveryHealSpec(RecoveryHealMode.Full, int.MinValue));
            Assert.DoesNotThrow(() => new RecoveryHealSpec(RecoveryHealMode.Full, int.MaxValue));
        }

        [Test]
        public void Snapshots_RejectInconsistentAvailabilityProjectionAndOptionIdentity()
        {
            var id = new RecoveryOptionId(1);
            RecoveryStageEntryRequest request = Request();
            var option = new RecoveryOptionSnapshot(id, 3, 5, true, RecoveryFailureReason.None);

            Assert.Throws<ArgumentException>(() => new RecoveryOptionSnapshot(id, 3, 5, true, RecoveryFailureReason.InsufficientFunds));
            Assert.Throws<ArgumentException>(() => new RecoveryOptionSnapshot(id, 3, 5, false, RecoveryFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecoveryOptionSnapshot(id, 3, -1, true, RecoveryFailureReason.None));
            Assert.Throws<ArgumentException>(() => Snapshot(request, new[] { option, option }));
            Assert.Throws<ArgumentException>(() => Snapshot(request, new[] { option }, new RecoveryOptionId(2)));
            Assert.Throws<ArgumentException>(() => Snapshot(request,
                new[] { new RecoveryOptionSnapshot(id, 3, 16, true, RecoveryFailureReason.None) }));
        }

        [Test]
        public void ResultFactories_KeepFailuresDistinctFromSuccessfulRequestsAndRetries()
        {
            RecoveryStageEntryRequest request = Request();
            RecoveryStageEntryRequestResult entry = RecoveryStageEntryRequestResult.Success(request);
            Assert.That(entry.Succeeded, Is.True);
            Assert.That(entry.Request, Is.SameAs(request));
            RecoveryStageEntryRequestResult rejectedEntry = RecoveryStageEntryRequestResult.Failure(RunFailureReason.StageKindMismatch);
            Assert.That(rejectedEntry.Succeeded, Is.False);
            Assert.That(rejectedEntry.Request, Is.Null);
            Assert.That(rejectedEntry.FailureReason, Is.EqualTo(RunFailureReason.StageKindMismatch));

            RecoveryStageSnapshot snapshot = Snapshot(request,
                new[] { new RecoveryOptionSnapshot(new RecoveryOptionId(1), 3, 5, true, RecoveryFailureReason.None) });
            RecoveryCommandResult rejected = RecoveryCommandResult.Failure(RecoveryFailureReason.UnknownOption, snapshot);
            Assert.That(rejected.Succeeded || rejected.AlreadyApplied, Is.False);
            Assert.That(rejected.CurrentRecovery, Is.SameAs(snapshot));
            Assert.That(RecoveryCommandResult.Failure(RecoveryFailureReason.Disposed).CurrentRecovery, Is.Null);
            Assert.Throws<ArgumentNullException>(() => RecoveryStageEntryRequestResult.Success(null));
            Assert.Throws<ArgumentNullException>(() => RecoveryCommandResult.Success(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryStageEntryRequestResult.Failure(RunFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryStageEntryRequestResult.Failure((RunFailureReason)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryCommandResult.Failure(RecoveryFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryCommandResult.Failure((RecoveryFailureReason)999));
        }

        private static RecoveryOptionSpec Option(int id) =>
            new RecoveryOptionSpec(new RecoveryOptionId(id), 3,
                new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 50));

        private static RecoveryStageEntryRequest Request() =>
            new RecoveryStageEntryRequest(new RunId(Guid.NewGuid()), new StageEntryId(Guid.NewGuid()),
                4, new StageNodeId(2), new StageKey(100), new HealthStateSnapshot(5, 20));

        private static RecoveryStageSnapshot Snapshot(RecoveryStageEntryRequest request,
            RecoveryOptionSnapshot[] options, RecoveryOptionId? appliedOptionId = null) =>
            new RecoveryStageSnapshot(request.RunId, request.EntryId, request.NodeId, request.StageKey,
                request.ExpectedRunRevision, request.Health, new PlayerWalletSnapshot(10), options, appliedOptionId);
    }
}
