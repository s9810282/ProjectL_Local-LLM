using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Framework
{
    public sealed class RecoveryDefinitionAdapterTests
    {
        [Test]
        public void Read_PreservesAuthoredOrderAndValues()
        {
            var db = new RecoveryStageTestDatabase();
            var spec = Read(db);
            var options = spec.Options.ToArray();
            Assert.That(spec.StageKey.Value, Is.EqualTo(20));
            Assert.That(options[0].Id.Value, Is.EqualTo(2));
            Assert.That(options[0].Cost, Is.EqualTo(3));
            Assert.That(options[0].Heal.Mode, Is.EqualTo(RecoveryHealMode.Flat));
            Assert.That(options[0].Heal.Value, Is.EqualTo(2));
            Assert.That(options[1].Id.Value, Is.EqualTo(1));
            Assert.That(options[1].Cost, Is.Zero);
            Assert.That(options[1].Heal.Mode, Is.EqualTo(RecoveryHealMode.Full));
            Assert.That(options[1].Heal.Value, Is.Zero);
            Assert.That(db.OptionKeys, Is.EqualTo(new[] { 2, 1 }));
            db.OptionKeys[0] = 1;
            db.Options[2] = new RecoveryOptionData(2, 99, RecoveryHealMode.Full, 7);
            Assert.That(spec.Options.Span[0].Id.Value, Is.EqualTo(2));
            Assert.That(spec.Options.Span[0].Cost, Is.EqualTo(3));
        }

        [TestCase("missing-root")]
        [TestCase("null-root")]
        [TestCase("null-detail")]
        [TestCase("wrong-key")]
        [TestCase("wrong-chapter")]
        [TestCase("boss")]
        [TestCase("shop")]
        [TestCase("combat")]
        [TestCase("empty")]
        [TestCase("duplicate")]
        [TestCase("zero-key")]
        [TestCase("missing-option")]
        [TestCase("null-option")]
        [TestCase("wrong-option-id")]
        [TestCase("negative-cost")]
        [TestCase("invalid-mode")]
        [TestCase("invalid-percent")]
        [TestCase("invalid-flat")]
        [TestCase("missing-ingame")]
        [TestCase("missing-stages")]
        [TestCase("missing-options")]
        public void Read_RejectsMalformedDefinition(string fault)
        {
            var db = new RecoveryStageTestDatabase();
            switch (fault)
            {
                case "missing-root": db.Stages.Remove(20); break;
                case "null-root": db.Stages[20] = null; break;
                case "null-detail": db.Stages[20] = new StageRootData(20, 1, StageMapRole.Regular, null); break;
                case "wrong-key": Root(db, key: 21); break;
                case "wrong-chapter": Root(db, chapter: 2); break;
                case "boss": Root(db, role: StageMapRole.Boss); break;
                case "shop": db.Stages[20] = db.Stages[100]; break;
                case "combat": Root(db, data: new NonRecoveryDetail()); break;
                case "empty": Root(db, data: new EventStageDetail(Array.Empty<int>())); break;
                case "duplicate": Root(db, data: new EventStageDetail(new[] { 1, 1 })); break;
                case "zero-key": Root(db, data: new EventStageDetail(new[] { 0 })); break;
                case "missing-option": db.Options.Remove(1); break;
                case "null-option": db.Options[1] = null; break;
                case "wrong-option-id": db.Options[1] = new RecoveryOptionData(2, 0, RecoveryHealMode.Full, 0); break;
                case "negative-cost": db.Options[1] = new RecoveryOptionData(1, -1, RecoveryHealMode.Full, 0); break;
                case "invalid-mode": db.Options[1] = new RecoveryOptionData(1, 0, (RecoveryHealMode)99, 0); break;
                case "invalid-percent": db.Options[1] = new RecoveryOptionData(1, 0, RecoveryHealMode.PercentOfMax, 101); break;
                case "invalid-flat": db.Options[1] = new RecoveryOptionData(1, 0, RecoveryHealMode.Flat, -1); break;
                case "missing-ingame": db.MissingInGame = true; break;
                case "missing-stages": db.MissingStages = true; break;
                case "missing-options": db.MissingOptions = true; break;
            }
            var error = Assert.Catch(() => Read(db));
            Assert.That(error, Is.InstanceOf<ArgumentException>().Or.InstanceOf<InvalidOperationException>()
                .Or.InstanceOf<System.Collections.Generic.KeyNotFoundException>());
        }

        [TestCase(RecoveryHealMode.Full, int.MinValue)]
        [TestCase(RecoveryHealMode.Flat, 0)]
        [TestCase(RecoveryHealMode.PercentOfMax, 1)]
        [TestCase(RecoveryHealMode.PercentOfMax, 100)]
        public void Read_PreservesValidHealBoundary(RecoveryHealMode mode, int value)
        {
            var db = new RecoveryStageTestDatabase();
            db.Options[1] = new RecoveryOptionData(1, 0, mode, value);
            var spec = Read(db);
            Assert.That(spec.Options.Span[1].Heal.Mode, Is.EqualTo(mode));
            Assert.That(spec.Options.Span[1].Heal.Value, Is.EqualTo(value));
        }

        [Test]
        public void Read_InvalidArgumentsRejectBeforeLookup()
        {
            var db = new RecoveryStageTestDatabase();
            Assert.Catch(() => Read(null));
            Assert.Catch(() => Read(db, 0));
            Assert.Catch(() => Read(db, 1, StageKey.Empty));
            Assert.That(db.StageReads, Is.Zero);
            Assert.That(db.OptionReads, Is.Empty);
        }

        private static void Root(RecoveryStageTestDatabase db, int key = 20, int chapter = 1,
            StageMapRole role = StageMapRole.Regular, IStageData data = null)
        {
            db.Stages[20] = new StageRootData(key, chapter, role, data ?? new EventStageDetail(new[] { 1 }));
        }

        private sealed class NonRecoveryDetail : IStageData
        {
            public StageType StageType => StageType.Combat;
            public void Validate() { }
        }

        private static RecoveryStageSpec Read(IDataBaseQueryContext db, int chapter = 1, StageKey? key = null)
            => RecoveryDefinitionAdapter.Read(db, chapter, key ?? new StageKey(20));
    }
}
