using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public abstract class TraitSpec
    {
        public TraitKey TraitKey { get; }

        protected TraitSpec(TraitKey traitKey)
        {
            if (traitKey == TraitKey.None || Enum.IsDefined(typeof(TraitKey), traitKey) == false)
                throw new ArgumentOutOfRangeException(nameof(traitKey));

            this.TraitKey = traitKey;
        }
    }
}