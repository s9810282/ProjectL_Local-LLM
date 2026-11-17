using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
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
    internal sealed class PiercingFalloffTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Range(1, 100)] private int reductionPercentPerTarget = 25;
        [Tooltip("광선 하나가 맞힐 최대 대상 수입니다. 0이면 타격하지 않습니다.")]
        [SerializeField, Min(0)] private int maxTargetsPerRay = 4;

        internal override UnitTraitData ToData() => new PiercingFalloffTraitData(reductionPercentPerTarget, maxTargetsPerRay);
    }

    [Serializable]
    internal sealed class DeadHandTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int damagePercent = 100;

        internal override UnitTraitData ToData() => new DeadHandTraitData(damagePercent);
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

    /// <summary>사령술의 소환 정의입니다. SO 참조는 입력 계층에서만 사용하고 키로 내보냅니다.</summary>
    [Serializable]
    internal sealed class NecromancyTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField] private UnitDataSO summonedUnit;
        [SerializeField, Min(1)] private int maxLivingSummons = 1;
        [Tooltip("사령술이 보류된 동안 쓰지 않는 값입니다(진영 턴에서 소환 시점 미정). 옛 행동 타임라인의 첫 행동 지연이었습니다.")]
        [SerializeField, Min(0)] private long firstActionDelay;

        internal override UnitTraitData ToData() => new NecromancyTraitData(
            summonedUnit != null ? summonedUnit.Key : EntityKey.None, maxLivingSummons, firstActionDelay);
    }

    /// <summary>
    /// 포효 발동 카운트와 카운트가 쌓이는 양입니다. 넉백 거리는 기존 넉백 특성에 입력합니다.
    /// 카운트는 밤 피격과 진영 턴 종료로 쌓이고, 발동 카운트에 닿으면 포효한 뒤 0으로 돌아갑니다.
    /// </summary>
    [Serializable]
    internal sealed class RoarTraitAuthoring : UnitTraitAuthoring
    {
        [Tooltip("포효가 발동하는 카운트입니다. 아래 양들로 쌓인 카운트가 이 값에 닿으면 포효합니다.")]
        [SerializeField, Min(1)] private int hitThreshold = 1;

        [Tooltip("밤에 피격될 때마다 쌓이는 카운트입니다(한 공격으로 여러 번 맞아도 한 번). 0이면 피격으로는 쌓이지 않습니다.")]
        [SerializeField, Min(0)] private int countPerHit = 1;

        [Tooltip("이 유닛의 진영 턴이 끝날 때마다 쌓이는 카운트입니다(보스면 적 턴 종료). 0이면 쌓이지 않습니다.")]
        [SerializeField, Min(0)] private int countPerOwnSideTurnEnd;

        [Tooltip("상대 진영 턴이 끝날 때마다 쌓이는 카운트입니다(보스면 플레이어 턴 종료). 0이면 쌓이지 않습니다.")]
        [SerializeField, Min(0)] private int countPerOpposingSideTurnEnd;

        internal override UnitTraitData ToData() =>
            new RoarTraitData(hitThreshold, countPerHit, countPerOwnSideTurnEnd, countPerOpposingSideTurnEnd);
    }
}
