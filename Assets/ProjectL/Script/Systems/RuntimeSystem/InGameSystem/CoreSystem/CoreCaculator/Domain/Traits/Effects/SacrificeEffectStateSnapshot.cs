using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class SacrificeEffectStateSnapshot : IEffectStateSnapshot
    {
        internal long LastEnemyTurnSequence { get; }
        internal int ActivationsThisEnemyTurn { get; }

        internal SacrificeEffectStateSnapshot(long lastEnemyTurnSequence, int activationsThisEnemyTurn)
        {
            if (lastEnemyTurnSequence < 0) throw new ArgumentOutOfRangeException(nameof(lastEnemyTurnSequence));
            if (activationsThisEnemyTurn < 0) throw new ArgumentOutOfRangeException(nameof(activationsThisEnemyTurn));
            LastEnemyTurnSequence = lastEnemyTurnSequence;
            ActivationsThisEnemyTurn = activationsThisEnemyTurn;
        }

        public bool ValueEquals(IEffectStateSnapshot other) => other is SacrificeEffectStateSnapshot state &&
            LastEnemyTurnSequence == state.LastEnemyTurnSequence && ActivationsThisEnemyTurn == state.ActivationsThisEnemyTurn;
    }
}
