using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class ForcedDisplacementTests
    {
        [TestCase(4, 2, 3, 3, -1, 1)]
        [TestCase(2, 2, 2, 5, 0, 1)]
        [TestCase(4, 4, 7, 4, 1, 0)]
        public void BranchGeometry_ExtendsCardinalAndDiagonalHitDirections(
            int attackerX, int attackerY,
            int hitX, int hitY,
            int expectedStepX, int expectedStepY)
        {
            CellCoord step = ForcedDisplacementGeometry.ResolveBranchStep(
                new CellCoord(attackerX, attackerY),
                new CellCoord(hitX, hitY));

            Assert.That(step, Is.EqualTo(new CellCoord(expectedStepX, expectedStepY)));
            Assert.That(
                ForcedDisplacementGeometry.ResolveDestinationAnchor(
                    new CellCoord(hitX, hitY),
                    step,
                    1),
                Is.EqualTo(new CellCoord(
                    hitX + expectedStepX,
                    hitY + expectedStepY)));
        }
    }
}
