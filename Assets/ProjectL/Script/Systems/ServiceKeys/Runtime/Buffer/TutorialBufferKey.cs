using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class TutorialBufferKey
    {
        public static readonly SystemGroupKey<ITutorialRequestBufferWriteSink> RequestWrite = new(nameof(RequestWrite));

        public static readonly SystemGroupKey<ITutorialResultBufferReadSink> ResultRead = new(nameof(ResultRead));

        public static readonly SystemGroupKey<ITutorialStateBufferReadSink> StateRead = new(nameof(StateRead));

        public static readonly SystemGroupKey<ITutorialBufferStore> Store = new(nameof(Store));
    }
}
