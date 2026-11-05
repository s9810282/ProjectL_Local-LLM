using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    public sealed class StationaryFireRuntimeState : TraitRuntimeState
    {
        public bool MovedThisOwnTurn { get; private set; }
        public StationaryFireRuntimeState() : this(false) { }
        public StationaryFireRuntimeState(bool value) : base(TraitKey.StationaryFire)
        {
            MovedThisOwnTurn = value;
        }
        internal override TraitRuntimeState Clone() => new StationaryFireRuntimeState(MovedThisOwnTurn);
        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not StationaryFireRuntimeState state) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
            MovedThisOwnTurn = state.MovedThisOwnTurn;
        }
    }
}
