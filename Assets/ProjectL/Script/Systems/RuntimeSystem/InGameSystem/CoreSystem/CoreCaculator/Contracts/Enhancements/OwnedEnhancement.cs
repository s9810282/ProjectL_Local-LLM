using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class OwnedEnhancement
    {
        public EnhancementKey Key { get; }
        public int Level { get; }

        public OwnedEnhancement(EnhancementKey key, int level)
        {
            if (key == EnhancementKey.None || !Enum.IsDefined(typeof(EnhancementKey), key))
                throw new ArgumentOutOfRangeException(nameof(key));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            Key = key;
            Level = level;
        }

        public bool SameValues(OwnedEnhancement other) => other != null && Key == other.Key && Level == other.Level;
    }
}
