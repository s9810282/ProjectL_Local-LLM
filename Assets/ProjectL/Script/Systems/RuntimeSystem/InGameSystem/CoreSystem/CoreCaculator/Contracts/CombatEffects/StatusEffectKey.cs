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
        Taunted = 14,
        Fire = 15,
        Bleeding = 16,
        MovementRangeDecrease = 17,
        Rampage = 18,
        /// <summary>적 전용 재구성 중입니다. 이동·공격 불가, 자기 진영 턴 시작을 정해진 횟수 지나면 부활합니다.</summary>
        Reconstructing = 19,
        Stun = 20
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
            StatusEffectKey.KnockbackImmunity or StatusEffectKey.Rampage or
            StatusEffectKey.Reconstructing => StatusEffectClassification.Buff,
            StatusEffectKey.AttackDecrease or StatusEffectKey.DefenseDecrease or
            StatusEffectKey.AttackRangeDecrease or StatusEffectKey.Disarm or
            StatusEffectKey.Bind or StatusEffectKey.Stun or StatusEffectKey.WeaknessExposure or StatusEffectKey.Taunted or
            StatusEffectKey.Fire or StatusEffectKey.Bleeding or StatusEffectKey.MovementRangeDecrease => StatusEffectClassification.Debuff,
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };
    }
}
