using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// SO 저작 데이터와 Core/View 사이에서 공유하는 효과 설정입니다.
    /// 하위 설정도 값 타입이므로 대입 및 전달 시 함께 복사됩니다.
    /// 남은 턴, 중첩 수 등 실행 중 상태는 포함하지 않습니다.
    /// </summary>
    [Serializable]
    public struct ArtifactEffectData
    {
        // 같은 Artifact 안에서 효과 항목을 구분하는 ID입니다. 기본값은 null입니다.
        public string entryId;
        public ArtifactTriggerType trigger;

        public ArtifactSourceData source;
        public ArtifactConditionData condition;
        public ArtifactTargetData target;
        public ArtifactLocationData location;
        public ArtifactEffectPayloadData effect;
        public ArtifactValueData value;

        // 단발 효과는 0, 지속 효과는 양수입니다. 감소 시점은 소비 측 규칙입니다.
        public int durationTurns;
    }

    /// <summary>
    /// Trigger의 원인이 된 유닛에 대한 필터입니다.
    /// 사건 Source가 없는 Trigger는 Any + CategoryKey.None으로 표현합니다.
    /// </summary>
    [Serializable]
    public struct ArtifactSourceData
    {
        public ArtifactFactionFilter faction;
        // None이면 카테고리 제한이 없습니다.
        public CategoryKey category;
    }

    /// <summary>Trigger 발생 당시 게임 상황에 대한 조건입니다.</summary>
    [Serializable]
    public struct ArtifactConditionData
    {
        public ArtifactConditionType type;
        // CompareUnitCounts일 때 배치된 아군 수(왼쪽)와 적군 수(오른쪽)를 비교합니다.
        public ArtifactComparisonType comparison;
    }

    /// <summary>효과를 받을 유닛의 선택 방식과 필터입니다.</summary>
    [Serializable]
    public struct ArtifactTargetData
    {
        public ArtifactTargetType type;
        public ArtifactFactionFilter faction;

        // None이면 카테고리 필터와 excludeCategory를 사용하지 않습니다.
        public CategoryKey category;
        public bool excludeCategory;

        public bool useHpRatioFilter;
        // 필터 사용 시 현재 HP / 최대 HP가 이 값 이하인 대상입니다. 범위: 0~1.
        public float maxHpRatio;
    }

    /// <summary>효과를 적용할 위치의 종류입니다.</summary>
    [Serializable]
    public struct ArtifactLocationData
    {
        // Area의 형태와 RandomPosition의 범위 데이터는 기획 확정 후 확장합니다.
        public ArtifactLocationType type;
    }

    /// <summary>적용할 효과 종류와 해당 효과 전용 설정입니다.</summary>
    [Serializable]
    public struct ArtifactEffectPayloadData
    {
        public ArtifactEffectType type;
        // ModifyStat에서 사용합니다. 기본값은 null이며 소비 측의 스탯 식별자와 대응해야 합니다.
        public string statId;
        // Knockback에서 사용하는 셀 단위 거리입니다. 방향 규칙은 별도 확정이 필요합니다.
        public int knockbackDistance;
        // SpawnSoul의 성장/회복 설정. 컴파일 시 성장 +1/+1 고정, 회복량은 데이터 값이다. 다른 효과는 모두 0.
        public SoulGrowthAmounts soulGrowth;
    }

    /// <summary>효과의 수치와 비율 계산의 기준입니다.</summary>
    [Serializable]
    public struct ArtifactValueData
    {
        public ArtifactValueType type;
        // Flat과 UnitCountDifference에서는 None, Percent에서는 비율을 적용할 기준을 지정합니다.
        public ArtifactValueBasis basis;
        // Percent에서 20은 20%를 의미합니다. maxHpRatio의 0~1 단위와 구분합니다.
        public float amount;
    }
}
