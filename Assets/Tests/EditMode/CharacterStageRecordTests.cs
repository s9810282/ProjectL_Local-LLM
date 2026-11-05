using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>유닛별 스테이지 기록 계약(설계 D1)과 그것을 담는 전투 사실·영수증 행의 규칙을 확인한다.</summary>
    public sealed class CharacterStageRecordTests
    {
        [Test]
        public void Empty_HasNoValues()
        {
            var empty = CharacterStageRecord.Empty;

            Assert.That(empty.IsEmpty, Is.True);
            Assert.That(new[] { empty.DamageTaken, empty.DamageDealt, empty.DistanceMoved, empty.Kills }, Is.All.EqualTo(0));
        }

        [Test]
        public void Add_AccumulatesEachValueSeparately()
        {
            var record = CharacterStageRecord.Empty.Add(damageTaken: 4).Add(damageDealt: 7, kills: 1).Add(distanceMoved: 3)
                .Add(damageTaken: 2, kills: 1);

            Assert.That(record.DamageTaken, Is.EqualTo(6));
            Assert.That(record.DamageDealt, Is.EqualTo(7));
            Assert.That(record.DistanceMoved, Is.EqualTo(3));
            Assert.That(record.Kills, Is.EqualTo(2));
            Assert.That(record.IsEmpty, Is.False);
        }

        [Test]
        public void Add_WithNothing_ReturnsSameObject_AndDoesNotChangeTheSource()
        {
            var source = CharacterStageRecord.Empty.Add(kills: 1);

            Assert.That(source.Add(), Is.SameAs(source));
            source.Add(kills: 5);
            Assert.That(source.Kills, Is.EqualTo(1));
        }

        [TestCase(-1, 0, 0, 0)]
        [TestCase(0, -1, 0, 0)]
        [TestCase(0, 0, -1, 0)]
        [TestCase(0, 0, 0, -1)]
        public void Add_RejectsNegativeValues(int taken, int dealt, int distance, int kills)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterStageRecord.Empty.Add(taken, dealt, distance, kills));
        }

        [Test]
        public void Add_Overflow_Throws()
        {
            var record = CharacterStageRecord.Empty.Add(damageDealt: int.MaxValue);

            Assert.Throws<OverflowException>(() => record.Add(damageDealt: 1));
        }

        [Test]
        public void SameValues_AndIsAtLeast_CompareEveryValue()
        {
            var baseRecord = CharacterStageRecord.Empty.Add(1, 1, 1, 1);

            Assert.That(baseRecord.SameValues(CharacterStageRecord.Empty.Add(1, 1, 1, 1)), Is.True);
            Assert.That(baseRecord.SameValues(null), Is.False);
            foreach (var larger in new[] { baseRecord.Add(damageTaken: 1), baseRecord.Add(damageDealt: 1),
                         baseRecord.Add(distanceMoved: 1), baseRecord.Add(kills: 1) })
            {
                Assert.That(baseRecord.SameValues(larger), Is.False);
                Assert.That(larger.IsAtLeast(baseRecord), Is.True);
                Assert.That(baseRecord.IsAtLeast(larger), Is.False);
            }
            Assert.That(baseRecord.IsAtLeast(baseRecord), Is.True);
            Assert.Throws<ArgumentNullException>(() => baseRecord.IsAtLeast(null));
        }

        [Test]
        public void Facts_DefaultToEmpty_AndOnlyOwnedCharactersMayHoldARecord()
        {
            var stats = ContractWithPreservationTests.StatsValue();
            var cell = new CellCoord(1, 1);
            var record = CharacterStageRecord.Empty.Add(damageTaken: 1);

            Assert.That(ContractWithPreservationTests.FactsValue().StageRecord, Is.SameAs(CharacterStageRecord.Empty));
            Assert.That(ContractWithPreservationTests.FactsValue().With(stageRecord: record).StageRecord.SameValues(record), Is.True);
            Assert.Throws<ArgumentException>(() => new CharacterCombatFacts(new EntityId(2), null, EntityKey.Unit_Demon,
                Owner.Enemy, null, CharacterRuntimeArea.Field, new HealthStateSnapshot(10, 10), stats, null, cell,
                Rotation4.R0, new[] { cell }, stageRecord: record));
            Assert.That(new CharacterCombatFacts(new EntityId(2), null, EntityKey.Unit_Demon, Owner.Enemy, null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(10, 10), stats, null, cell, Rotation4.R0, new[] { cell })
                .StageRecord.IsEmpty, Is.True);
        }

        [Test]
        public void Receipt_DefaultsToEmpty_AndComparesTheRecord()
        {
            var id = new CharacterInstanceId(1);
            var record = CharacterStageRecord.Empty.Add(damageDealt: 9, kills: 1);
            CharacterRewardReceipt Row(CharacterStageRecord stageRecord = null) =>
                new(id, EntityKey.Unit_Akimbo, 0, 0, 1, 0, 0, 1, 0, 0, stageRecord);

            Assert.That(Row().StageRecord, Is.SameAs(CharacterStageRecord.Empty));
            Assert.That(Row(record).StageRecord, Is.SameAs(record));
            Assert.That(Row(record).SameValues(Row(CharacterStageRecord.Empty.Add(damageDealt: 9, kills: 1))), Is.True);
            Assert.That(Row(record).SameValues(Row()), Is.False);
        }
    }
}
