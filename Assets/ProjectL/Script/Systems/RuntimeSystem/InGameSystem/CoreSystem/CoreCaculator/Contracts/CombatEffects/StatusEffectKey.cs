using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>효과의 고정 식별자입니다. 부여 특성의 키나 효과 분류와 독립적입니다.</summary>
    public enum StatusEffectKey
    {
        None = 0,
        AttackIncrease = 1,
        AttackDecrease = 2,
        DefenseIncrease = 3,
        DefenseDecrease = 4,
        OutgoingDamageIncrease = 5,
        IncomingDamageDecrease = 6,
        Shield = 7,
        MovementRangeIncrease = 8,
        AttackRangeDecrease = 9,
        Disarm = 10,
        Bind = 11,
        KnockbackImmunity = 12,
        WeaknessExposure = 13,
        /// <summary>도발 범위 안의 AI 진영 유닛입니다. 도발한 유닛을 우선 공격·추격합니다(값 없음, 조건이 맞는 동안).</summary>
        Taunted = 14
    }

    public enum StatusEffectClassification
    {
        Buff = 1,
        Debuff = 2
    }

    public static class StatusEffectCatalog
    {
        public static StatusEffectClassification ClassificationOf(StatusEffectKey key) => key switch
        {
            StatusEffectKey.AttackIncrease or StatusEffectKey.DefenseIncrease or
            StatusEffectKey.OutgoingDamageIncrease or StatusEffectKey.IncomingDamageDecrease or
            StatusEffectKey.Shield or StatusEffectKey.MovementRangeIncrease or
            StatusEffectKey.KnockbackImmunity => StatusEffectClassification.Buff,
            StatusEffectKey.AttackDecrease or StatusEffectKey.DefenseDecrease or
            StatusEffectKey.AttackRangeDecrease or StatusEffectKey.Disarm or
            StatusEffectKey.Bind or StatusEffectKey.WeaknessExposure or StatusEffectKey.Taunted => StatusEffectClassification.Debuff,
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };
    }
}
