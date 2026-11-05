using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class ItemInventoryBufferKey
    {
        public static readonly SystemGroupKey<IItemInventoryReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IItemInventoryStore> Store = new(nameof(Store));
    }
}
