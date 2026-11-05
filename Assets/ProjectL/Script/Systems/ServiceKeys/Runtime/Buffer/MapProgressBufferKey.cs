using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class MapProgressBufferKey
    {
        public static readonly SystemGroupKey<IMapProgressBufferStore> Store = new(nameof(Store));

        public static readonly SystemGroupKey<IMapProgressBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IMapProgressBufferWriteSink> Write = new(nameof(Write));
    }
}
