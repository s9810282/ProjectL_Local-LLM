using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class CurrentProgressBufferKey
    {
        public static readonly SystemGroupKey<IStageCommandResultReadSink> CommandResults = new(nameof(CommandResults));

        public static readonly SystemGroupKey<IStageCommandResultStore> CommandResultStore = new(nameof(CommandResultStore));

        public static readonly SystemGroupKey<ICurrentProgressBufferStore> Store = new(nameof(Store));

        public static readonly SystemGroupKey<ICurrentProgressBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ICurrentProgressBufferWriteSink> Write = new(nameof(Write));
    }
}
