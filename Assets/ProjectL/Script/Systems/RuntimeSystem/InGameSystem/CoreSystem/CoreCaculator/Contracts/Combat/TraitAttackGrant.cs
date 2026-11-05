using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class TraitAttackGrant
    {
        private readonly AttackIncreaseStatus _status;
        public EffectOriginKey Origin => _status.Origin.Value;
        public int Amount => _status.Amount;
        public int RemainingControllerTurnEnds => _status.RemainingControllerTurnEnds;

        public TraitAttackGrant(EffectOriginKey origin, int amount, int remainingControllerTurnEnds)
        {
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait) throw new ArgumentException("특성 출처가 필요합니다.", nameof(origin));
            _status = new AttackIncreaseStatus(origin, amount, remainingControllerTurnEnds);
        }

        internal TraitAttackGrant(AttackIncreaseStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));

        public AttackIncreaseStatus ToStatus() => _status;

        public bool SameValues(TraitAttackGrant other) => other != null && _status.SameValues(other._status);
    }
}
