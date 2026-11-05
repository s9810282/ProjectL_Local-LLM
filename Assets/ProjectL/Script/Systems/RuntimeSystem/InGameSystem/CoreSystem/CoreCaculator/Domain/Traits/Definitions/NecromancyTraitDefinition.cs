using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class NecromancyTraitDefinition : TraitDefinition
    {
        public EntityKey SummonedUnitKey { get; }
        public int MaxLivingSummons { get; }
        public long FirstActionDelay { get; }

        public NecromancyTraitDefinition(EntityKey summonedUnitKey, int maxLivingSummons, long firstActionDelay)
            : base(TraitKey.Necromancy)
        {
            if (summonedUnitKey == EntityKey.None)
                throw new ArgumentOutOfRangeException(nameof(summonedUnitKey));
            if (maxLivingSummons < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLivingSummons));
            if (firstActionDelay < 0)
                throw new ArgumentOutOfRangeException(nameof(firstActionDelay));
            SummonedUnitKey = summonedUnitKey;
            MaxLivingSummons = maxLivingSummons;
            FirstActionDelay = firstActionDelay;
        }
    }
}
