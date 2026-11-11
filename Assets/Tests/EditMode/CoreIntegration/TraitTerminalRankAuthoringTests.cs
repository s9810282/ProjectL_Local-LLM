using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitTerminalRankAuthoringTests
    {
        [Test]
        public void Rank2OnlyStopsAtTwo()
        {
            var data = Definition(new TraitRankValues(TraitKey.Vanguard, 4));
            Assert.That(data.IsTrainable, Is.True);
            Assert.That(data.Rank2.Amount, Is.EqualTo(4));
            Assert.That(data.Rank3, Is.Null);
        }

        [Test]
        public void ExistingThreeRanksRemainThree()
        {
            var data = Definition(new TraitRankValues(TraitKey.Vanguard, 4), new TraitRankValues(TraitKey.Vanguard, 9));
            Assert.That(((VanguardTraitData)data.Normal).AttackBonus, Is.EqualTo(1));
            Assert.That((data.Rank2.Amount, data.Rank3.Amount), Is.EqualTo((4, 9)));
        }

        [Test]
        public void Rank3WithoutRank2IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => Definition(null, new TraitRankValues(TraitKey.Vanguard, 9)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MismatchedRankKeyIsRejected(bool third)
        {
            var wrong = new TraitRankValues(TraitKey.Hunter, 4);
            Assert.Throws<InvalidOperationException>(() => Definition(
                third ? new TraitRankValues(TraitKey.Vanguard, 4) : wrong,
                third ? wrong : new TraitRankValues(TraitKey.Vanguard, 9)));
        }

        private static TraitGrowthDefinitionData Definition(TraitRankValues rank2, TraitRankValues rank3 = null) =>
            new(new VanguardTraitData(1, 1), null, "", "", "", "", rank2, rank3);
    }
}
