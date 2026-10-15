using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions
{
    internal readonly struct AttackDamageContribution
    {
        public TraitKey Source { get; }
        public int FlatBonus { get; }

        public AttackDamageContribution(TraitKey source, int flatBonus)
        {
            if (source == TraitKey.None || Enum.IsDefined(typeof(TraitKey), source) == false)
                throw new ArgumentOutOfRangeException(nameof(source));
            if (flatBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(flatBonus));

            Source = source;
            FlatBonus = flatBonus;
        }
    }
}