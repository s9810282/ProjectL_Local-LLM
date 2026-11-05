using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class LeapTraitSpec : TraitSpec
    {
        public LeapTraitSpec() : base(TraitKey.Leap)
        {
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            return true;
        }
    }
}
