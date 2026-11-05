using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class CurrencyBufferKey
    {
        public static readonly SystemGroupKey<ICurrencyBufferStore> Store = new(nameof(Store));

        public static readonly SystemGroupKey<ICurrencyBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ICurrencyBufferWriteSink> Write = new(nameof(Write));
    }
}
