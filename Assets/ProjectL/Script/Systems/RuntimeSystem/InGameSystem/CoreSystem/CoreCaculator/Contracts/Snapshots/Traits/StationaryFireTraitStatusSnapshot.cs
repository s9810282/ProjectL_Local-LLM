using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class StationaryFireTraitStatusSnapshot : TraitStatusSnapshot
    {
        public bool MovedThisOwnTurn { get; }
        public StationaryFireTraitStatusSnapshot() : this(false) { }
        public StationaryFireTraitStatusSnapshot(bool value) : base(TraitKey.StationaryFire)
        {
            MovedThisOwnTurn = value;
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is StationaryFireTraitStatusSnapshot status && MovedThisOwnTurn == status.MovedThisOwnTurn;
    }
}
