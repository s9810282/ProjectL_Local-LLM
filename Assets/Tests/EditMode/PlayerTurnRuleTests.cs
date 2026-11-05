using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerTurnRuleTests
    {

        [Test]
        public void NightLengthIsEight()
        {
            Assert.That(PlayerTurnRules.PlayerTurnsPerNight, Is.EqualTo(8));
        }


    }
}
