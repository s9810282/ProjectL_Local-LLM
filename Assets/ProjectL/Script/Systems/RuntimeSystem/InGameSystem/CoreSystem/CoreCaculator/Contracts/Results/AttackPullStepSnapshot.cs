using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>실제로 도달한 끌어오기 한 걸음과 그 원인이다.</summary>
    public sealed class AttackPullStepSnapshot
    {
        public CellCoord From { get; }
        public CellCoord To { get; }
        public CombatMovementCause Cause { get; }

        public AttackPullStepSnapshot(CellCoord from, CellCoord to, CombatMovementCause cause)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause));
            if (cause.Agency != CombatMovementAgency.Forced || cause.TraitKey != TraitKey.Pull)
                throw new ArgumentException("끌어오기 걸음에는 강제 이동/Pull 원인이 필요합니다.", nameof(cause));
            if (Math.Abs((long)to.X - from.X) + Math.Abs((long)to.Y - from.Y) != 1)
                throw new ArgumentException("끌어오기 걸음은 상하좌우로 한 칸 이동해야 합니다.", nameof(to));
            From = from;
            To = to;
        }
    }
}
