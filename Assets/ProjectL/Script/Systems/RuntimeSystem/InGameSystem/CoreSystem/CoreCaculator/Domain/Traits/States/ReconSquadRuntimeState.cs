using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class ReconSquadRuntimeState : TraitRuntimeState
    {
        public ReconSquadRuntimeState() : base(TraitKey.ReconSquad)
        {
        }
        internal override TraitRuntimeState Clone() => new ReconSquadRuntimeState();
        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not ReconSquadRuntimeState) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
        }
    }
}
