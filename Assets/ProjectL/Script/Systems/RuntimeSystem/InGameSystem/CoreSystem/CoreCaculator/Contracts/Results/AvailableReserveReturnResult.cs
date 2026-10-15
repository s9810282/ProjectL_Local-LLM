using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AvailableReserveReturnResult
    {
        public bool Attempted { get; }
        public DeploymentResult Deployment { get; }

        public AvailableReserveReturnResult(bool attempted, DeploymentResult deployment)
        {
            if (deployment.CurrentStage == null)
                throw new ArgumentException("CurrentStage is required.", nameof(deployment));
            if (!attempted && deployment.Succeeded)
                throw new ArgumentException("An unattempted return cannot succeed.", nameof(deployment));
            Attempted = attempted;
            Deployment = deployment;
        }
    }
}
