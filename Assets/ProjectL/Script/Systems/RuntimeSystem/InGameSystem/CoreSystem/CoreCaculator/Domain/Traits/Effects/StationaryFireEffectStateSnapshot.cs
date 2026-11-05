using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class StationaryFireEffectStateSnapshot : IEffectStateSnapshot
    {
        public bool MovedThisOwnTurn { get; }
        internal StationaryFireEffectStateSnapshot(bool value)
        {
            MovedThisOwnTurn = value;
        }
        public bool ValueEquals(IEffectStateSnapshot other) => other is StationaryFireEffectStateSnapshot state && MovedThisOwnTurn == state.MovedThisOwnTurn;
    }
}
