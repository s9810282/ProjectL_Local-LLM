using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public enum ItemStatKind
    {
        Priority,
        PhysicalDefense
    }

    public sealed class ItemTemporaryEffect
    {
        public int ItemKey { get; }
        public ItemStatKind Stat { get; }
        public int Magnitude { get; }
        public int PreparationIndex { get; }

        public ItemTemporaryEffect(int itemKey, ItemStatKind stat, int magnitude, int preparationIndex)
        {
            if (itemKey <= 0) throw new ArgumentOutOfRangeException(nameof(itemKey));
            if (!Enum.IsDefined(typeof(ItemStatKind), stat)) throw new ArgumentOutOfRangeException(nameof(stat));
            if (magnitude <= 0) throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (preparationIndex < 0) throw new ArgumentOutOfRangeException(nameof(preparationIndex));
            ItemKey = itemKey;
            Stat = stat;
            Magnitude = magnitude;
            PreparationIndex = preparationIndex;
        }
    }
}
