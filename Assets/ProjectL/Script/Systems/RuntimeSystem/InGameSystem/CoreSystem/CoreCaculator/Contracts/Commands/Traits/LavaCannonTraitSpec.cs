using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class LavaCannonTraitSpec : TraitSpec
    {
        public LavaCannonTraitSpec() : base(TraitKey.LavaCannon)
        {
        }

        protected override bool SameValuesCore(TraitSpec other) => true;
    }
}
