using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    public sealed class MovementRangeDecreaseStatus : AppliedStatus
    {
        public int Magnitude => 1;
        public long ExpiresAtCompletedControllerTurnCount { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.NextAffectedSideTurnEnd;

        public MovementRangeDecreaseStatus(EffectOriginKey origin, long expiresAtCompletedControllerTurnCount)
            : base(StatusEffectKey.MovementRangeDecrease, origin)
        {
            if (expiresAtCompletedControllerTurnCount < 0) throw new ArgumentOutOfRangeException(nameof(expiresAtCompletedControllerTurnCount));
            ExpiresAtCompletedControllerTurnCount = expiresAtCompletedControllerTurnCount;
        }

        public override bool SameValues(AppliedStatus other) => other is MovementRangeDecreaseStatus status &&
            Origin == status.Origin && ExpiresAtCompletedControllerTurnCount == status.ExpiresAtCompletedControllerTurnCount;
    }
}
