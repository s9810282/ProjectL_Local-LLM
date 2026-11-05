using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class CharacterBufferKey
    {
        public static readonly SystemGroupKey<ITraitGrowthStateReadSink> GrowthState = new(nameof(GrowthState));
        public static readonly SystemGroupKey<ITraitGrowthResultReadSink> GrowthResults = new(nameof(GrowthResults));
        public static readonly SystemGroupKey<ITraitGrowthStateStore> GrowthStore = new(nameof(GrowthStore));
        public static readonly SystemGroupKey<ICharacterBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ICharacterBufferWriteSink> Write = new(nameof(Write));

        public static readonly SystemGroupKey<ICharacterBufferStore> Store = new(nameof(Store));
        public static readonly SystemGroupKey<ICharacterStatsReadSink> StatsRead = new(nameof(StatsRead));
        public static readonly SystemGroupKey<ICharacterStatsRequestSink> StatsRequest = new(nameof(StatsRequest));
        public static readonly SystemGroupKey<ICharacterStatsStore> StatsStore = new(nameof(StatsStore));
    }
}
