using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class ShopViewBufferKey
    {
        public static readonly SystemGroupKey<IShopViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IShopViewBufferWriteSink> Write = new(nameof(Write));
    }
}
