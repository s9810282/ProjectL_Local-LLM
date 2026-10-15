using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>
    /// UnitDataSO 인스펙터에서 입력하는 특성 하나입니다. ToData로 순수 UnitTraitData를 만듭니다.
    /// </summary>
    /// <remarks>
    /// [SerializeReference]는 타입 이름으로 직렬화하므로 클래스명이나 네임스페이스를 바꾸면
    /// 이미 입력한 항목이 빈다. 바꿔야 하면 [MovedFrom]으로 옛 이름을 남긴다.
    /// </remarks>
    [Serializable]
    internal abstract class UnitTraitAuthoring
    {
        internal abstract UnitTraitData ToData();
    }

    [Serializable]
    internal sealed class FocusTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int damagePerStack = 1;
        [SerializeField, Min(1)] private int maxStacks = 3;

        internal override UnitTraitData ToData() => new FocusTraitData(damagePerStack, maxStacks);
    }

    [Serializable]
    internal sealed class MoveAttackDiscountTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int discountAmount = 1;
        [SerializeField, Min(1)] private int minimumAttackCost = 1;

        internal override UnitTraitData ToData() =>
            new MoveAttackDiscountTraitData(discountAmount, minimumAttackCost);
    }

    [Serializable]
    internal sealed class PiercingFalloffTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Range(1, 100)] private int reductionPercentPerTarget = 50;
        [Tooltip("광선 하나가 맞힐 최대 대상 수입니다. 0이면 타격하지 않습니다.")]
        [SerializeField, Min(0)] private int maxTargetsPerRay;

        internal override UnitTraitData ToData() => new PiercingFalloffTraitData(reductionPercentPerTarget, maxTargetsPerRay);
    }

    [Serializable]
    internal sealed class ThornFieldTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int thornDamage = 1;
        [SerializeField, Min(1)] private int lifetimeUnitTurns = 1;

        internal override UnitTraitData ToData() => new ThornFieldTraitData(thornDamage, lifetimeUnitTurns);
    }

    [Serializable]
    internal sealed class KnockbackTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int distanceCells = 1;

        internal override UnitTraitData ToData() => new KnockbackTraitData(distanceCells);
    }

    /// <summary>샷건 산탄. 0번이 거리 1이며, 유닛의 공격이 닿는 모든 거리를 채워야 합니다(모자라면 전투 계산 때 오류).</summary>
    [Serializable]
    internal sealed class DistanceDamageScaleTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int[] percentByDistance = { 150, 50 };

        internal override UnitTraitData ToData() => new DistanceDamageScaleTraitData(percentByDistance);
    }

    /// <summary>아킴보 난사의 처치 연쇄 공격. 설정값이 없습니다.</summary>
    [Serializable]
    internal sealed class KillChainAttackTraitAuthoring : UnitTraitAuthoring
    {
        internal override UnitTraitData ToData() => new KillChainAttackTraitData();
    }
}
