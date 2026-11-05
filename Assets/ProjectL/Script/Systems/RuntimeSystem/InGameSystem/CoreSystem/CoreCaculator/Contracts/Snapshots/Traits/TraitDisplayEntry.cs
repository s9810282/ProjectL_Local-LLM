using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>
    /// 표시 순서의 특성 하나와 그 출처다(특성 표시 출처 설계 TO1). 순서는 <see cref="TraitSettingsSet.Display"/>가 정한다.
    /// </summary>
    public sealed class TraitDisplayEntry
    {
        public TraitKey Key { get; }
        public TraitOrigin Origin { get; }

        public TraitDisplayEntry(TraitKey key, TraitOrigin origin)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (origin == TraitOrigin.None || Enum.IsDefined(typeof(TraitOrigin), origin) == false)
                throw new ArgumentOutOfRangeException(nameof(origin));
            Key = key;
            Origin = origin;
        }

        public bool Same(TraitDisplayEntry other) => other != null && Key == other.Key && Origin == other.Origin;
    }
}
