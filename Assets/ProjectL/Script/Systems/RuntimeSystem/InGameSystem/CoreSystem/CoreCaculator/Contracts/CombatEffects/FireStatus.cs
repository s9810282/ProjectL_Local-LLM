using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    public sealed class FireStatus : AppliedStatus
    {
        public int Damage => 2;
        public int RemainingAffectedSideStarts { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.AffectedSideTurnStarts;

        public FireStatus(EffectOriginKey origin, int remainingAffectedSideStarts) : base(StatusEffectKey.Fire, origin)
        {
            if (remainingAffectedSideStarts < 1) throw new ArgumentOutOfRangeException(nameof(remainingAffectedSideStarts));
            RemainingAffectedSideStarts = remainingAffectedSideStarts;
        }

        public override bool SameValues(AppliedStatus other) => other is FireStatus status &&
            Origin == status.Origin && RemainingAffectedSideStarts == status.RemainingAffectedSideStarts;
    }
}
