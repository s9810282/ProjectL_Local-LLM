using System;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class RunProgressBufferTests
    {
        [Test]
        public void RunProgress_DefaultIsInProgress_AndTerminalReceiptIsImmutable()
        {
            var buffer = new RunProgressBuffer();
            Assert.That(buffer.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(buffer.Completion, Is.Null);
            var receipt = Receipt(RunOutcome.Victory);
            var store = (IRunProgressBufferStore)buffer;
            Assert.That(store.PrepareCompletion(receipt), Is.SameAs(receipt));
            Assert.That(buffer.Outcome, Is.EqualTo(RunOutcome.InProgress));
            store.InstallCompletion(receipt);
            Assert.That(buffer.Completion, Is.SameAs(receipt));
            Assert.That(buffer.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(receipt.ChapterKey, Is.EqualTo(1));
            Assert.That(receipt.NodeId, Is.EqualTo(new StageNodeId(2)));
            Assert.That(receipt.StageKey, Is.EqualTo(new StageKey(10)));
            foreach (var property in typeof(RunCompletionReceipt).GetProperties())
                Assert.That(property.SetMethod, Is.Null);
        }

        [Test]
        public void StartingHandSize_IsUnsetByDefault_InstalledPerRun_AndClearedOnReset()
        {
            var buffer = new RunProgressBuffer();
            var store = (IRunProgressBufferStore)buffer;
            Assert.That(buffer.StartingHandSize, Is.Null);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => store.InstallStartingHandSize(0));
            store.InstallStartingHandSize(4);
            Assert.That(buffer.StartingHandSize, Is.EqualTo(4));
            store.Reset();
            Assert.That(buffer.StartingHandSize, Is.Null);
        }

        [TestCase(RunOutcome.Victory)]
        [TestCase(RunOutcome.Defeat)]
        public void TerminalReceipt_RejectsSecondWrite_UntilExplicitReset(RunOutcome outcome)
        {
            var buffer = new RunProgressBuffer();
            var store = (IRunProgressBufferStore)buffer;
            var receipt = store.PrepareCompletion(Receipt(outcome));
            store.InstallCompletion(receipt);
            Assert.Throws<InvalidOperationException>(() => store.PrepareCompletion(receipt));
            Assert.Throws<InvalidOperationException>(() => store.PrepareCompletion(Receipt(
                outcome == RunOutcome.Victory ? RunOutcome.Defeat : RunOutcome.Victory)));
            Assert.That(buffer.Completion, Is.SameAs(receipt));
            store.Reset();
            Assert.That(buffer.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(buffer.Completion, Is.Null);
            Assert.DoesNotThrow(() => store.PrepareCompletion(receipt));
        }

        [TestCase(RunOutcome.InProgress, 1, 2, 10)]
        [TestCase((RunOutcome)99, 1, 2, 10)]
        [TestCase(RunOutcome.Defeat, 0, 2, 10)]
        [TestCase(RunOutcome.Defeat, 1, 0, 10)]
        [TestCase(RunOutcome.Defeat, 1, 2, 0)]
        public void Receipt_RejectsInvalidIdentity(RunOutcome outcome, int chapter, int node, int key)
            => Assert.Catch<ArgumentException>(() => new RunCompletionReceipt(outcome, chapter,
                node == 0 ? StageNodeId.Empty : new StageNodeId(node),
                key == 0 ? StageKey.Empty : new StageKey(key)));

        [Test]
        public void RunProgress_RegistersOneStoreAndPreservesOtherRuntime()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var reader = builder.BuildSystemReader().ServiceStoreReader;
            var query = reader.GetService(BufferSystemKey.Buffer);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => reader.GetService(RunProgressBufferKey.Read));
            var store = (IRunProgressBufferStore)query.RunProgressReadSink;
            store.InstallCompletion(store.PrepareCompletion(Receipt(RunOutcome.Defeat)));
            var second = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(second);
            var other = second.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            query.RunProgressWriteSink.ResetRunProgress();
            Assert.That(query.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(other.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.Throws<ArgumentNullException>(() => store.PrepareCompletion(null));
        }

        private static RunCompletionReceipt Receipt(RunOutcome outcome)
            => new(outcome, 1, new StageNodeId(2), new StageKey(10));
    }
}
