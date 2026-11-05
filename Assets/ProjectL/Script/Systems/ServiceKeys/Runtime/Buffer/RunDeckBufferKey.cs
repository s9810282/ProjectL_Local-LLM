using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class RunDeckBufferKey
    {
        public static readonly SystemGroupKey<IRunDeckReadSink> Read = new(nameof(Read));
        public static readonly SystemGroupKey<IRunDeckStore> Store = new(nameof(Store));
    }
}
