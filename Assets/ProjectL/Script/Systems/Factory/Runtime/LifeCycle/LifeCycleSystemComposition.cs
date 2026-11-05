using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;

namespace ProjectL.Systems.Factory.Runtime.LifeCycle
{
    /// <summary>같은 LifeCycle 조립의 그룹과 덱 편집 창(C1)을 전달합니다. 창은 InGame 조립에 참조로만 넘깁니다.</summary>
    internal sealed class LifeCycleSystemComposition
    {
        internal IExportSystemServices Group { get; }
        internal IDeckEditAvailabilityRead DeckEditAvailability { get; }

        internal LifeCycleSystemComposition(IExportSystemServices group, IDeckEditAvailabilityRead deckEditAvailability)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            DeckEditAvailability = deckEditAvailability ?? throw new ArgumentNullException(nameof(deckEditAvailability));
        }
    }
}
