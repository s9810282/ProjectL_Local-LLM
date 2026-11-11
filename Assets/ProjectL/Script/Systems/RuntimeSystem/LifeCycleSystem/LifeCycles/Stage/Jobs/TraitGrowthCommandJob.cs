using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>증강 선택 하나를 실행 순간에 예약하고 그 번호만 Core에 처리시킵니다. 몸통은 <see cref="StageRequestCommandJob"/>입니다(회복과 같은 꼴).</summary>
    internal sealed class TraitGrowthCommandJob : StageRequestCommandJob
    {
        private readonly ITraitGrowthCommandProcessingService _core;
        private readonly ITraitGrowthRequestExecution _owner;

        internal TraitGrowthCommandJob(ITraitGrowthCommandProcessingService core, ITraitGrowthRequestExecution owner)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        protected override string NotificationErrorKey => "TraitGrowthRequestNotificationError";

        protected override bool TryReserveAndBegin(out int number) =>
            _owner.TryReserveExecution(out number) && _owner.TryBeginExecution(number);

        protected override bool Process(int number) => _core.ProcessNextTraitGrowthCommand(number);

        protected override void NotifyFinished(int number, bool processed, Exception error) =>
            _owner.NotifyExecutionFinished(number, processed, error);
    }
}
