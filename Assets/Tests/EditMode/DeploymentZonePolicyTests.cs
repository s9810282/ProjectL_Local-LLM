using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;

namespace ProjectL.Tests.Core
{
    public sealed class DeploymentZonePolicyTests
    {
        private static readonly CellCoord Inside = new CellCoord(1, 1);
        private static readonly CellCoord Outside = new CellCoord(0, 0);
        private static readonly DeploymentZonePolicy Policy =
            new DeploymentZonePolicy(PlayerDeploymentZone.Of(new[] { Inside, new CellCoord(2, 1) }));

        [Test]
        public void Player_DeploysOnlyInsidePlayerZone()
        {
            Assert.That(Policy.CanDeploy(Owner.Player, new[] { Inside, new CellCoord(2, 1) }), Is.True);
            Assert.That(Policy.CanDeploy(Owner.Player, new[] { Outside }), Is.False);
            Assert.That(Policy.CanDeploy(Owner.Player, new[] { Inside, Outside }), Is.False);
        }

        [Test]
        public void Enemy_DeploysAnywhereOutsidePlayerZone()
        {
            Assert.That(Policy.CanDeploy(Owner.Enemy, new[] { Outside, new CellCoord(7, 7) }), Is.True);
            Assert.That(Policy.CanDeploy(Owner.Enemy, new[] { Inside }), Is.False);
            // 다칸 적은 한 칸이라도 아군 구역 안이면 거절한다.
            Assert.That(Policy.CanDeploy(Owner.Enemy, new[] { Outside, Inside }), Is.False);
        }

        [TestCase(Owner.Neutral)]
        [TestCase(Owner.None)]
        public void OtherOwners_AreRejected(Owner owner)
        {
            Assert.That(Policy.CanDeploy(owner, new[] { Outside }), Is.False);
        }

        [Test]
        public void NullZone_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new DeploymentZonePolicy(null));
        }
    }
}
