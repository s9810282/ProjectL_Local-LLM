using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>출처별 소모 잔량과 마지막 초기화 경계입니다. 자격은 현재 배치에서 다시 투영합니다.</summary>
    public sealed class TraitShieldGrant
    {
        private readonly ShieldStatus _status;
        public EffectOriginKey Origin => _status.Origin.Value;
        public int RemainingBudget => _status.RemainingBudget;
        public bool Eligible => _status.Eligible;
        public long LastResetControllerTurnCount => _status.LastResetControllerTurnCount;

        public TraitShieldGrant(EffectOriginKey origin, int remainingBudget, bool eligible, long lastResetControllerTurnCount)
            : this(origin, remainingBudget, eligible, lastResetControllerTurnCount, null) { }

        public TraitShieldGrant(EffectOriginKey origin, int remainingBudget, bool eligible, long lastResetControllerTurnCount,
            Owner? expiresAtSideTurnStart)
        {
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId <= 0 ||
                (origin.DefinitionKey != (int)TraitKey.SoloAction && origin.DefinitionKey != (int)TraitKey.Cohesion &&
                 origin.DefinitionKey != (int)TraitKey.PositionSwap))
                throw new ArgumentException("방어막 특성 출처가 필요합니다.", nameof(origin));
            if (remainingBudget < 0) throw new ArgumentOutOfRangeException(nameof(remainingBudget));
            if (lastResetControllerTurnCount < 0) throw new ArgumentOutOfRangeException(nameof(lastResetControllerTurnCount));
            _status = new ShieldStatus(origin, remainingBudget, eligible, lastResetControllerTurnCount, expiresAtSideTurnStart);
        }

        internal TraitShieldGrant(ShieldStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));

        public ShieldStatus ToStatus() => _status;

        public bool SameValues(TraitShieldGrant other) => other != null && _status.SameValues(other._status);
    }
}
