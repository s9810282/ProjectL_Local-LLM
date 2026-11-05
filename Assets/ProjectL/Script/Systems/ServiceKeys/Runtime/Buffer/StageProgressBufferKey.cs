using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class StageProgressBufferKey
    {
        public static readonly SystemGroupKey<IStageProgressBufferStore> Store = new(nameof(Store));

        public static readonly SystemGroupKey<IStageProgressBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IStageProgressBufferWriteSink> Write = new(nameof(Write));
    }
}
