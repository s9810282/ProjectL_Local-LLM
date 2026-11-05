using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemInventoryChange
    {
        public ItemInventorySnapshot Expected { get; }
        public ItemInventorySnapshot Final { get; }

        public ItemInventoryChange(ItemInventorySnapshot expected, ItemInventorySnapshot final)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (final == null) throw new ArgumentNullException(nameof(final));
            Expected = expected;
            Final = final;
        }
    }
}
