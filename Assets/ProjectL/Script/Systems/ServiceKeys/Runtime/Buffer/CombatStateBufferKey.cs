using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class CombatStateBufferKey
    {
        public static readonly SystemGroupKey<ICombatResultHistoryReadSink> History = new(nameof(History));

        public static readonly SystemGroupKey<ICombatStateBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ICombatResultReadSink> Results = new(nameof(Results));

        public static readonly SystemGroupKey<ICombatStateBufferStore> Store = new(nameof(Store));
    }
}
