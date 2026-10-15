using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyRedeploymentResolverTests
    {
        [Test]
        public void PaddedSingleCell_NegativeAnchorCanHaveInsideEnemyDeploymentCell()
        {
            // 도형 계산만 보존한다. 패딩된 유닛의 출현·재배치 지원을 활성화하지 않는다.
            var footprint = new TileFootprint(2, 1, new[] { new CellCoord(1, 0) });
            var anchor = new CellCoord(-1, 4);
            var occupied = footprint.GetOccupiedCell(0, anchor, Rotation4.R0);
            var board = new CombatCommandState(3, 5, CombatFlowPhase.PlayerInput, default, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                Array.Empty<CombatTimelineEntrySnapshot>());
            Assert.That(footprint.CellCount, Is.EqualTo(1));
            Assert.That(occupied, Is.EqualTo(new CellCoord(0, 4)));
            Assert.That(board.IsInside(anchor), Is.False);
            Assert.That(board.IsInside(occupied), Is.True);
            Assert.That(new DeploymentZonePolicy(1, 1, 5).CanDeploy(Owner.Enemy, new[] { occupied }), Is.True);
            Assert.That(board.Structures, Is.Empty);
        }
    }
}
