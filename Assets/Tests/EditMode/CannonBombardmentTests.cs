using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Task 5: Cannon Square/HorizontalRow geometry와 footprint 기반 직접 조준 금지 영역을 검증한다.
    /// </summary>
    public sealed class CannonBombardmentTests
    {

        // [사용자 확정] 금지 영역은 실제 점유 footprint를 한 칸 확장한 영역이다.
        [Test]
        public void ForbiddenArea_FollowsOneByOneFootprint()
        {
            var occupied = new[] { new CellCoord(10, 10) };

            // 3x3 전체가 금지된다.
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(10, 10)), Is.True);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(11, 11)), Is.True);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(9, 9)), Is.True);

            // 한 칸 더 밖은 허용된다.
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(12, 10)), Is.False);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(12, 12)), Is.False);
        }

        [Test]
        public void ForbiddenArea_ExpandsActualMultiCellFootprint()
        {
            // 2x2 footprint
            var occupied = new[]
            {
                new CellCoord(10, 10), new CellCoord(11, 10),
                new CellCoord(10, 11), new CellCoord(11, 11),
            };

            // footprint 자신과 한 칸 테두리는 금지된다.
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(11, 11)), Is.True);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(12, 12)), Is.True);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(9, 9)), Is.True);

            // 테두리 바깥은 허용된다. 1x1 기준이었다면 (12,10)은 허용이지만 2x2에서는 금지다.
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(12, 10)), Is.True);
            Assert.That(CannonAppointExclusionCalculator.IsForbidden(occupied, new CellCoord(13, 10)), Is.False);
        }
    }
}
