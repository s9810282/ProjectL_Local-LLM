using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class SacrificeTraitStatusSnapshot : TraitStatusSnapshot
    {
        public long LastEnemyTurnSequence { get; }
        public int ActivationsThisEnemyTurn { get; }

        public SacrificeTraitStatusSnapshot() : this(0, 0)
        {
        }

        public SacrificeTraitStatusSnapshot(long lastEnemyTurnSequence, int activationsThisEnemyTurn) : base(TraitKey.Sacrifice)
        {
            if (lastEnemyTurnSequence < 0) throw new ArgumentOutOfRangeException(nameof(lastEnemyTurnSequence));
            if (activationsThisEnemyTurn < 0) throw new ArgumentOutOfRangeException(nameof(activationsThisEnemyTurn));
            LastEnemyTurnSequence = lastEnemyTurnSequence;
            ActivationsThisEnemyTurn = activationsThisEnemyTurn;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is SacrificeTraitStatusSnapshot state &&
            LastEnemyTurnSequence == state.LastEnemyTurnSequence && ActivationsThisEnemyTurn == state.ActivationsThisEnemyTurn;
    }
}
