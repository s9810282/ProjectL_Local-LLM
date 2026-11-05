using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class ImpactBoltRuntimeState : TraitRuntimeState
    {
        public ImpactBoltRuntimeState()
            : base(TraitKey.ImpactBolt)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new ImpactBoltRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not ImpactBoltRuntimeState)
            {
                throw new ArgumentException(
                    "ImpactBolt state requires a matching final state.",
                    nameof(source));
            }
        }
    }
}
