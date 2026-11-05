using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class SteadfastWillTraitSpec : TraitSpec
    {
        public SteadfastWillTraitSpec() : base(TraitKey.SteadfastWill)
        {
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            return true;
        }
    }
}
