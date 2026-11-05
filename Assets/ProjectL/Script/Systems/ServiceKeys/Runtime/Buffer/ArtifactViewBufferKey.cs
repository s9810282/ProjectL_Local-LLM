using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class ArtifactViewBufferKey
    {
        public static readonly SystemGroupKey<IArtifactViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IArtifactViewBufferWriteSink> Write = new(nameof(Write));
    }
}
