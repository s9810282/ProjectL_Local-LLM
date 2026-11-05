using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Summoning;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>보류된 소환이 제어자·밤 진행·할당 상태를 변경하지 않는지 확인한다.</summary>
    public sealed class SummonTimelineTests
    {
        [TestCase(0)]
        [TestCase(7)]
        [TestCase(int.MaxValue)]
        public void AuthoredDelay_DoesNotActivateSummoningOrAdvanceController(int delay)
        {
            var input = CombatSummonTests.Input(new CellCoord(3, 3));
            var result = CombatSummonCalculator.TryCalculate(input,
                new NecromancyTraitSpec(EntityKey.Unit_Gravekeeper, 2, delay), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DeferredUnitTurnTiming"));
            Assert.That(result.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(result.FinalState.Allocation.SameValues(input.State.Allocation), Is.True);
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }
    }
}
