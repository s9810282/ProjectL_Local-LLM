using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    internal sealed class RecoveryStateQueryService : IRecoveryStateReadSink, ISystemPostBindService, IExportSystemServices
    {
        private readonly RecoveryStateProjection _projection;
        private IBufferQuery _buffers;

        internal RecoveryStateQueryService(RecoveryStateProjection projection)
        {
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public bool TryGetCurrent(out RecoveryStateSnapshot state)
        {
            var facts = _projection.Capture(_buffers);
            state = facts == null ? null : _projection.Project(facts);
            return state != null;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRecoveryStateReadSink>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
