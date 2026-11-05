using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryInputContractTests
    {
        private static readonly Guid EntryA = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid EntryB = new Guid("22222222-2222-2222-2222-222222222222");

        [TestCase("entry")]
        [TestCase("chapter-zero")]
        [TestCase("chapter-negative")]
        [TestCase("node")]
        [TestCase("stage")]
        public void Context_RejectsEmptyIdentityAndNonpositiveChapter(string invalid)
        {
            Assert.That(() => new RecoveryEntryContext(
                invalid == "entry" ? StageEntryId.Empty : new StageEntryId(EntryA),
                invalid == "chapter-zero" ? 0 : invalid == "chapter-negative" ? -1 : 1,
                invalid == "node" ? StageNodeId.Empty : new StageNodeId(2),
                invalid == "stage" ? StageKey.Empty : new StageKey(20)),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("entry")]
        [TestCase("chapter")]
        [TestCase("node")]
        [TestCase("stage")]
        public void Context_EqualityIncludesEntryAndAllLocationFields(string changed)
        {
            RecoveryEntryContext original = Context();
            var other = new RecoveryEntryContext(
                new StageEntryId(changed == "entry" ? EntryB : EntryA),
                changed == "chapter" ? 2 : 1,
                new StageNodeId(changed == "node" ? 3 : 2),
                new StageKey(changed == "stage" ? 21 : 20));

            Assert.That(original.Equals(other), Is.False);
            Assert.That(original.Equals((object)other), Is.False);
            Assert.That(original == other, Is.False);
            Assert.That(original != other, Is.True);
        }

        [Test]
        public void Context_EqualValuesShareDictionaryKey()
        {
            RecoveryEntryContext first = Context();
            RecoveryEntryContext equal = Context();
            var entries = new Dictionary<RecoveryEntryContext, string> { [first] = "current" };
            entries[equal] = "retained";

            Assert.That(first.Equals(equal), Is.True);
            Assert.That(first.Equals((object)equal), Is.True);
            Assert.That(first == equal, Is.True);
            Assert.That(first != equal, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[first], Is.EqualTo("retained"));
            Assert.That(first.Equals(null), Is.False);
            Assert.That(first.Equals(EntryA), Is.False);
        }

        [TestCase(RecoveryCommandKind.Select)]
        [TestCase(RecoveryCommandKind.Complete)]
        public void CommandFactories_RejectDefaultContext(RecoveryCommandKind kind)
        {
            TestDelegate create = kind == RecoveryCommandKind.Select
                ? () => RecoveryCommandData.Select(default, new RecoveryOptionId(2))
                : () => RecoveryCommandData.Complete(default);

            Assert.That(create, Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Select_RequiresOptionAndRetainsContext(bool emptyOption)
        {
            RecoveryEntryContext context = Context();
            if (emptyOption)
            {
                Assert.That(() => RecoveryCommandData.Select(context, RecoveryOptionId.Empty),
                    Throws.InstanceOf<ArgumentException>());
                return;
            }

            RecoveryCommandData command = RecoveryCommandData.Select(context, new RecoveryOptionId(2));
            Assert.That(command.Kind, Is.EqualTo(RecoveryCommandKind.Select));
            Assert.That(command.Context, Is.EqualTo(context));
            Assert.That(command.Context.EntryId.Value, Is.EqualTo(EntryA));
            Assert.That(command.Context.ChapterKey, Is.EqualTo(1));
            Assert.That(command.Context.NodeId.Value, Is.EqualTo(2));
            Assert.That(command.Context.StageKey.Value, Is.EqualTo(20));
            Assert.That(command.OptionId, Is.EqualTo(new RecoveryOptionId(2)));
        }

        [Test]
        public void Complete_HasNoOption()
        {
            Assert.That(() => RecoveryCommandData.Complete(default), Throws.InstanceOf<ArgumentException>());
            RecoveryEntryContext context = Context();
            RecoveryCommandData command = RecoveryCommandData.Complete(context);

            Assert.That(command.Kind, Is.EqualTo(RecoveryCommandKind.Complete));
            Assert.That(command.Context, Is.EqualTo(context));
            Assert.That(command.OptionId, Is.Null);
        }

        private static RecoveryEntryContext Context()
        {
            return new RecoveryEntryContext(new StageEntryId(EntryA), 1,
                new StageNodeId(2), new StageKey(20));
        }
    }
}
