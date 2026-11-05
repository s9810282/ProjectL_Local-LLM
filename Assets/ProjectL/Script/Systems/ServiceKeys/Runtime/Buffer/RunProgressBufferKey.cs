using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class RunProgressBufferKey
    {
        public static readonly SystemGroupKey<IRunProgressBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IRunProgressBufferStore> Store = new(nameof(Store));
    }
}
