using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CombatRewardDefinitionTests
    {
        [Test]
        public void MissingExcludedAndEligibleZero_AreDistinct()
        {
            UnitKillRewardSpec missing = null;
            var excluded = UnitKillRewardSpec.Excluded;
            var zero = new UnitKillRewardSpec(0, 0);
            Assert.That(missing, Is.Null);
            Assert.That(excluded.IsConfigured, Is.True);
            Assert.That(excluded.IsEligible, Is.False);
            Assert.That((excluded.Gold, excluded.Experience), Is.EqualTo((0, 0)));
            Assert.That(zero.IsConfigured && zero.IsEligible, Is.True);
            Assert.That((zero.Gold, zero.Experience), Is.EqualTo((0, 0)));
        }

        [Test]
        public void InvalidAmounts_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnitKillRewardSpec(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnitKillRewardSpec(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageClearRewardSpec(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageClearRewardSpec(0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageClearRewardSpec(0, 0, -1));
        }

        [Test]
        public void DefinitionAndProfileCopy_PreserveConfiguredRewards()
        {
            var reward = new UnitKillRewardSpec(25, 25);
            var cells = new[] { CellCoord.Zero };
            var spec = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, cells,
                10, MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 10, 5, 30, Array.Empty<TraitSpec>(),
                UnitOwnerEligibility.EnemyOnly, killReward: reward);
            var units = new[] { spec };
            var clear = new StageClearRewardSpec(7, 8, 9);
            var initialization = new BattleInitialization(new StageConfig(8, 8, 4, 2, 2),
                CellCoord.Zero, Rotation4.R0, units, Array.Empty<StructureSpec>(),
                UnitSpecTestFactory.CreateDefaultCategoryCatalog(), clearRewards: clear);
            var copy = initialization.WithAttackProfiles(Array.Empty<CombatAttackProfile>());
            var definition = new UnitCatalog(units, TraitRegistry.CreateDefault()).GetRequired(spec.Key);
            cells[0] = new CellCoord(99, 99);
            units[0] = null;

            Assert.That(definition.KillReward, Is.SameAs(reward));
            Assert.That(initialization.Units[0].KillReward, Is.SameAs(reward));
            Assert.That(copy.ClearRewards, Is.SameAs(clear));
            Assert.That(copy.Units[0], Is.SameAs(spec));
            Assert.That(spec.FootprintLocalCells.Span[0], Is.EqualTo(CellCoord.Zero));
            var context = new CombatBattleFacts(4, 1, 2, 2,
                Array.Empty<CharacterInstanceId>(), 0, clearRewards: clear);
            Assert.That(context.ClearRewards, Is.SameAs(clear));
        }
    }
}
