using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class ArtifactBufferKey
    {
        public static readonly SystemGroupKey<IArtifactCommandResultReadSink> CommandResults = new(nameof(CommandResults));

        public static readonly SystemGroupKey<IArtifactCommandResultStore> CommandStore = new(nameof(CommandStore));

        public static readonly SystemGroupKey<IArtifactBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<IArtifactInitializationWriteSink> Initialize = new(nameof(Initialize));

        public static readonly SystemGroupKey<IArtifactEditWriteSink> Edit = new(nameof(Edit));

        public static readonly SystemGroupKey<IArtifactPreviewWriteSink> Preview = new(nameof(Preview));

        public static readonly SystemGroupKey<IArtifactEditResultReadSink> EditResults = new(nameof(EditResults));

        public static readonly SystemGroupKey<IArtifactPreviewReadSink> Previews = new(nameof(Previews));
    }
}
