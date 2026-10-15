using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactSoulStateTests
    {

        [TestCase(2, 7, 0, 12, 27, 2, 7)]
        [TestCase(0, 5, 10, 25, 25, 0, 5)]
        public void GrowthMath_UsesIndependentAmountsAndCapsHealing(
            int attack,
            int maxHealth,
            int heal,
            int expectedHealth,
            int expectedMaxHealth,
            int expectedAttack,
            int expectedMaxBonus)
        {
            (HealthStateSnapshot health, SoulGrowthState growth) = SoulGrowthMath.Collect(
                new HealthStateSnapshot(attack == 0 ? 19 : 12, 20),
                SoulGrowthState.Empty,
                new SoulGrowthAmounts(attack, maxHealth, heal));

            Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(health.MaxHealth, Is.EqualTo(expectedMaxHealth));
            Assert.That(growth.AttackBonus, Is.EqualTo(expectedAttack));
            Assert.That(growth.MaxHealthBonus, Is.EqualTo(expectedMaxBonus));
            Assert.That(growth.Revision, Is.EqualTo(1));
        }
    }
}
