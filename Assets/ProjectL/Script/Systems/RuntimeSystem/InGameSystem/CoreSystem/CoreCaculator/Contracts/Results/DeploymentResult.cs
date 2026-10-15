using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// View가 Drop 성공 여부와 처리 후의 전체 Stage 상태를 함께 받는 결과다.
    /// </summary>
    public readonly struct DeploymentResult
    {
        public bool Succeeded { get; }
        public StageSnapshot CurrentStage { get; }

        public DeploymentResult(bool succeeded, StageSnapshot currentStage)
        {
            CurrentStage = currentStage ?? throw new ArgumentNullException(nameof(currentStage));
            Succeeded = succeeded;
        }
    }
}
