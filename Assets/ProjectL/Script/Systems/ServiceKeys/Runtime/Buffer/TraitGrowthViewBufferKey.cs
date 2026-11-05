using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class TraitGrowthViewBufferKey
    {
        public static readonly SystemGroupKey<ITraitGrowthViewReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<ITraitGrowthViewWriteSink> Write = new(nameof(Write));
        public static readonly SystemGroupKey<ITraitGrowthViewBufferStore> Store = new(nameof(Store));
    }
}
