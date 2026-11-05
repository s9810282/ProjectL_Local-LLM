using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class CombatViewBufferKey
    {
        public static readonly SystemGroupKey<ICombatViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ICombatViewBufferWriteSink> Write = new(nameof(Write));
    }
}
