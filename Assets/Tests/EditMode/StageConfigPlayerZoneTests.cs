using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class StageConfigPlayerZoneTests
    {
        [Test]
        public void ZoneConstructor_RejectsEmptyOrOutsideZone()
        {
            Assert.Throws<ArgumentNullException>(() => Stage(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage(PlayerDeploymentZone.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage(PlayerDeploymentZone.Rows(4)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage(PlayerDeploymentZone.Of(new[] { new CellCoord(4, 0) })));
        }

        [Test]
        public void ZoneConstructor_KeepsCellZone_AndOldDepthPropertiesReadZero()
        {
            var zone = PlayerDeploymentZone.Of(new[] { new CellCoord(3, 2), CellCoord.Zero });
            var stage = Stage(zone);
            Assert.That(stage.PlayerZone, Is.SameAs(zone));
            Assert.That(stage.PlayerDeploymentDepth, Is.Zero);
            Assert.That(stage.EnemyDeploymentDepth, Is.Zero);
        }

        [Test]
        public void DepthConstructor_BuildsRowZone_AndIgnoresEnemyDepth()
        {
            // 적 배치 구역이 없어 옛 "두 줄 수 합 < 높이" 검사도 없다.
            var stage = new StageConfig(8, 6, 8, 4, 4);
            Assert.That(stage.PlayerZone.RowDepth, Is.EqualTo(4));
            Assert.That(stage.PlayerDeploymentDepth, Is.EqualTo(4));
            Assert.That(stage.EnemyDeploymentDepth, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageConfig(8, 6, 8, 0, 2));
        }

        private static StageConfig Stage(PlayerDeploymentZone zone) => new StageConfig(4, 3, 8, zone, 0,
            Array.Empty<InitialEnemyPlacementSpec>(), Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(),
            Array.Empty<InitialEnemyStructurePlacementSpec>());
    }
}
