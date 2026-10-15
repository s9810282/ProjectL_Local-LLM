using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class ShopCandidateSpec
    {
        public EntityKey CharacterKey { get; }
        public int Cost { get; }

        public ShopCandidateSpec(EntityKey characterKey, int cost)
        {
            if (characterKey == EntityKey.None) throw new ArgumentException(nameof(characterKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            CharacterKey = characterKey;
            Cost = cost;
        }
    }
}
