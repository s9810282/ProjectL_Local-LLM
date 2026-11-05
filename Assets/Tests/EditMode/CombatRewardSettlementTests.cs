using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class CombatRewardSettlementTests
    {
        [Test]
        public void Settlement_SnapshotsRowsAndDistinguishesConfiguredZeroClear()
        {
            var row = new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_MainCharacter,
                0, 0, 1, 0, 150, 1, 0, 150);
            var rows = new[] { row };
            var configured = new CombatRewardSettlement(1, 10, 0, 0, true, rows);
            rows[0] = null;
            Assert.That(configured.Characters[0], Is.SameAs(row));
            Assert.That(configured.GoldAwarded, Is.EqualTo(10));
            Assert.That(configured.HasClearReward, Is.True);
            Assert.That(new CombatRewardSettlement(0, 0, 0, 0, false, Array.Empty<CharacterRewardReceipt>()).HasClearReward, Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterRewardReceipt>)configured.Characters).Clear());
        }

        [TestCase(-1, 0, 0, 0)]
        [TestCase(1, -1, 0, 0)]
        [TestCase(1, 0, -1, 0)]
        [TestCase(1, 0, 0, -1)]
        public void Settlement_RejectsNegativeAmounts(int count, int kill, int clear, int fame) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatRewardSettlement(count, kill, clear, fame, true,
                Array.Empty<CharacterRewardReceipt>()));

        [Test]
        public void Settlement_RejectsMissingDuplicateRowsUnconfiguredClearAndOverflow()
        {
            var row = new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_MainCharacter,
                0, 100, 1, 0, 150, 1, 100, 150);
            Assert.Throws<ArgumentNullException>(() => new CombatRewardSettlement(0, 0, 0, 0, false, null));
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 0, 0, 0, true, new CharacterRewardReceipt[] { null }));
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 0, 0, 0, true, new[] { row, row }));
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 0, 0, 0, false, new[] { row }));
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 0, 1, 0, false, Array.Empty<CharacterRewardReceipt>()));
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 0, 0, 1, false, Array.Empty<CharacterRewardReceipt>()));
            Assert.Throws<OverflowException>(() => new CombatRewardSettlement(1, int.MaxValue, 1, 0, true, Array.Empty<CharacterRewardReceipt>()));
            // 처치 골드는 확정 처치가 있어야 한다.
            Assert.Throws<ArgumentException>(() => new CombatRewardSettlement(0, 1, 0, 0, false, Array.Empty<CharacterRewardReceipt>()));
        }
    }
}
