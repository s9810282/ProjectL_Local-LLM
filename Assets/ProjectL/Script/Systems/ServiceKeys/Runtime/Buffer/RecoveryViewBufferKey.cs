using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class RecoveryViewBufferKey
    {
        public static readonly SystemGroupKey<IRecoveryViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IRecoveryViewBufferWriteSink> Write = new(nameof(Write));
    }
}
