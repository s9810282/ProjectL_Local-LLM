using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class StageResultBoundaryTests
    {
        [Test]
        public void TurnResult_RequiresActorOnlyOnSuccess()
        {
            var snapshot = new StageSnapshot(2, 2, StagePhase.Deployment,
                Array.Empty<EntitySnapshot>(), Array.Empty<ReserveSlotSnapshot>());
            Assert.Throws<ArgumentException>(() => new CombatTurnResult(true,
                CombatTurnFailureReason.None, EntityId.Empty, snapshot));
            Assert.Throws<ArgumentException>(() => new CombatTurnResult(false,
                CombatTurnFailureReason.NoActiveUnit, new EntityId(1), snapshot));
            var result = new CombatTurnResult(true, CombatTurnFailureReason.None,
                new EntityId(1), snapshot);
            Assert.That(result.EndedUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(result.CurrentStage, Is.SameAs(snapshot));
            var failed = new CombatTurnResult(false, CombatTurnFailureReason.NoActiveUnit,
                EntityId.Empty, snapshot);
            Assert.That(failed.EndedUnitId, Is.EqualTo(EntityId.Empty));
            Assert.That(failed.CurrentStage, Is.SameAs(snapshot));
        }

        [Test]
        public void AvailableReturn_DistinguishesRejectedAttemptFromNoSlot()
        {
            var snapshot = new StageSnapshot(2, 2, StagePhase.Deployment,
                Array.Empty<EntitySnapshot>(), Array.Empty<ReserveSlotSnapshot>());
            var failure = new DeploymentResult(false, snapshot);
            Assert.That(new AvailableReserveReturnResult(false, failure).Attempted, Is.False);
            Assert.That(new AvailableReserveReturnResult(true, failure).Attempted, Is.True);
            var success = new AvailableReserveReturnResult(true, new DeploymentResult(true, snapshot));
            Assert.That(success.Attempted, Is.True);
            Assert.That(success.Deployment.Succeeded, Is.True);
            Assert.That(success.Deployment.CurrentStage, Is.SameAs(snapshot));
            Assert.Throws<ArgumentException>(() => new AvailableReserveReturnResult(false,
                new DeploymentResult(true, snapshot)));
            Assert.Throws<ArgumentException>(() => new AvailableReserveReturnResult(true, default));
        }
    }
}
