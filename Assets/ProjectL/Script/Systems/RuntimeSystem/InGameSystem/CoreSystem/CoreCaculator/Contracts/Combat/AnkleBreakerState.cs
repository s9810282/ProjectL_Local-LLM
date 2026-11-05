using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>피격자가 받은 제한의 출처와 해당 진영 종료 횟수입니다.</summary>
    public sealed class AnkleBreakerState
    {
        private readonly ActionRestrictionStatus _status;
        public EffectOriginKey Origin => _status.Origin.Value;
        public int RemainingAffectedSideTurnEnds => _status.RemainingAffectedSideTurnEnds;

        public AnkleBreakerState(EffectOriginKey origin, int remainingAffectedSideTurnEnds)
        {
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId <= 0 || origin.DefinitionKey != (int)TraitKey.AnkleBreaker)
                throw new ArgumentException("일치하는 특성 출처가 필요합니다.", nameof(origin));
            if (remainingAffectedSideTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(remainingAffectedSideTurnEnds));
            _status = new ActionRestrictionStatus(StatusEffectKey.Bind, origin, remainingAffectedSideTurnEnds);
        }

        internal AnkleBreakerState(ActionRestrictionStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (status.Key != StatusEffectKey.Bind) throw new ArgumentException("제한 종류가 일치하지 않습니다.", nameof(status));
            _status = status;
        }

        public ActionRestrictionStatus ToStatus() => _status;

        public bool SameValues(AnkleBreakerState other) => other != null && Origin == other.Origin &&
            RemainingAffectedSideTurnEnds == other.RemainingAffectedSideTurnEnds;
    }
}
