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

        public ItemTemporaryEffect(int itemKey, ItemStatKind stat, int magnitude)
        {
            if (itemKey <= 0) throw new ArgumentOutOfRangeException(nameof(itemKey));
            if (!Enum.IsDefined(typeof(ItemStatKind), stat)) throw new ArgumentOutOfRangeException(nameof(stat));
            if (magnitude <= 0) throw new ArgumentOutOfRangeException(nameof(magnitude));
            ItemKey = itemKey;
            Stat = stat;
            Magnitude = magnitude;
        }
    }
}
