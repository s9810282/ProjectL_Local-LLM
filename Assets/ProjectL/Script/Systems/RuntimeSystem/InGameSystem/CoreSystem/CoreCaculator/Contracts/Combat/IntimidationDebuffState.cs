using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>피격 대상의 위협 공격력 감소와 출처를 정규 원본에서 읽습니다.</summary>
    public sealed class IntimidationDebuffState
    {
        private readonly AttackDecreaseStatus _status;
        public int AttackReductionPercent => _status.ReductionPercent;
        public EffectOriginKey? Origin => _status.Origin;

        public IntimidationDebuffState(int attackReductionPercent, EffectOriginKey? origin = null)
        {
            if (attackReductionPercent < 1 || attackReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(attackReductionPercent));

            if (origin.HasValue && (!origin.Value.IsValid || origin.Value.Kind != EffectOwnerKind.Trait ||
                origin.Value.OwnerInstanceId <= 0 || origin.Value.DefinitionKey != (int)TraitKey.Intimidation))
                throw new ArgumentException("위협의 원본 특성 출처가 필요합니다.", nameof(origin));
            _status = new AttackDecreaseStatus(origin, attackReductionPercent);
        }

        internal IntimidationDebuffState(AttackDecreaseStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));

        public AttackDecreaseStatus ToStatus() => _status;

        public bool SameValues(IntimidationDebuffState other) =>
            other != null && AttackReductionPercent == other.AttackReductionPercent && Origin == other.Origin;
    }
}
