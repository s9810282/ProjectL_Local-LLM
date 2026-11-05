using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class FameBufferKey
    {
        public static readonly SystemGroupKey<IFameBufferReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IFameBufferStore> Store = new(nameof(Store));
    }
}
