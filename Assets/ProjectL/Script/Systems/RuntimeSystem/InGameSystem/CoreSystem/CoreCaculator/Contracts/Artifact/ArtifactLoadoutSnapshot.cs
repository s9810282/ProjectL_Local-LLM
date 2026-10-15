using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>한 시점의 Artifact Layout Revision과 배치 효과 복사본입니다.</summary>
    public readonly struct ArtifactLoadoutSnapshot
    {
        public long Revision { get; }
        public ReadOnlyMemory<ArtifactPlacedEffectSnapshot> Effects { get; }

        public ArtifactLoadoutSnapshot(long revision, ArtifactPlacedEffectSnapshot[] effects)
        {
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (effects == null) throw new ArgumentNullException(nameof(effects));

            var copiedEffects = new ArtifactPlacedEffectSnapshot[effects.Length];
            Array.Copy(effects, copiedEffects, effects.Length);
            Revision = revision;
            Effects = copiedEffects;
        }
    }
}
