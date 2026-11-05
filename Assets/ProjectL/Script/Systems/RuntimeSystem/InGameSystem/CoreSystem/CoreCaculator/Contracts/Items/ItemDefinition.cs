using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemDefinition
    {
        public int Key { get; }
        public ItemEffectSpec Effect { get; }

        public ItemDefinition(int key, ItemEffectSpec effect)
        {
            if (key <= 0) throw new ArgumentOutOfRangeException(nameof(key));
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            Key = key;
            Effect = effect;
        }
    }
}
