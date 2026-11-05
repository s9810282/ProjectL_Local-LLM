using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class RecoveryResultBufferKey
    {
        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.IRecoveryResultReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.IRecoveryResultStore> Store = new(nameof(Store));
    }
}
