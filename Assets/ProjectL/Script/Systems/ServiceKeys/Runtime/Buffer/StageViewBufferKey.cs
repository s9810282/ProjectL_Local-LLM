using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class StageViewBufferKey
    {
        public static readonly SystemGroupKey<IStageViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IStageViewBufferWriteSink> Write = new(nameof(Write));
    }
}
