using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class FocusEffectStateSnapshot : IEffectStateSnapshot
    {
        public int ActiveStacks { get; }
        public bool GainScheduled { get; }
        public bool MovedThisTurn { get; }

        internal FocusEffectStateSnapshot(int activeStacks, bool gainScheduled, bool movedThisTurn)
        {
            if (activeStacks < 0) throw new ArgumentOutOfRangeException(nameof(activeStacks));

            ActiveStacks = activeStacks;
            GainScheduled = gainScheduled;
            MovedThisTurn = movedThisTurn;
        }

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is FocusEffectStateSnapshot snapshot &&
                   snapshot.ActiveStacks == ActiveStacks &&
                   snapshot.GainScheduled == GainScheduled &&
                   snapshot.MovedThisTurn == MovedThisTurn;
        }
    }
}
