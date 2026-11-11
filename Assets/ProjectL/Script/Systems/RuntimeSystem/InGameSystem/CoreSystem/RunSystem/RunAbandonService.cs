using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem
{
    public interface IRunAbandonService : IServiceModule
    {
        /// <summary>
        /// 전투가 없는 현재 런을 비웁니다. 대상이 바뀌었거나 포기할 수 없으면 상태를 바꾸지 않고 false를 반환합니다.
        /// </summary>
        bool TryAbandonRun(RunId expectedRunId);
    }

    internal sealed class RunAbandonService : IRunAbandonService, ISystemPostBindService, IExportSystemServices
    {
        private IRunProgressBufferReadSink _run;
        private ICombatStateBufferReadSink _combat;
        private IRunProgressBufferWriteSink _progress;

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _run = buffers.RunProgressReadSink;
            _combat = buffers.CombatStateReadSink;
            _progress = buffers.RunProgressWriteSink;
        }

        public bool TryAbandonRun(RunId expectedRunId)
        {
            if (_progress == null)
                throw new InvalidOperationException("[RunAbandonService] Buffer 서비스 바인딩이 필요합니다.");
            if (expectedRunId.IsEmpty || _run.RunId != expectedRunId ||
                _run.Outcome != RunOutcome.InProgress || _combat.HasBattle)
                return false;

            _progress.ResetRunProgress();
            return true;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunAbandonService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
