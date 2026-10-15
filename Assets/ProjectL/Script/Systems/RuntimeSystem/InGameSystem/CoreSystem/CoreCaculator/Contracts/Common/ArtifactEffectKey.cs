using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>배치된 Artifact 효과의 발동 시점 또는 활성 구간입니다. 실제 판정은 소비 측에서 수행합니다.</summary>
    public enum ArtifactTriggerType
    {
        // 스테이지에 진입했을 때.
        StageEntered,
        // 스테이지 진행 동안 활성화되는 효과. 상시 보정인지 반복 발동인지는 별도 규칙이 필요합니다.
        StageActive,
        // 스테이지를 클리어했을 때.
        StageCleared,
        // 스테이지에 실패했을 때.
        StageFailed,
        // 낮 페이즈로 전환했을 때.
        DayStarted,
        // 밤 페이즈로 전환했을 때.
        NightStarted,
        // 유닛의 턴이 시작될 때. Source로 해당 유닛을 필터링합니다.
        UnitTurnStarted,
        // 유닛의 턴이 종료될 때.
        UnitTurnEnded,
        // 공격을 시도했을 때. 검증 및 비용 지불과의 선후 관계는 별도 확정이 필요합니다.
        AttackAttempted,
        // 공격이 성공했을 때. 명중/피해 발생 중 성공 기준은 별도 확정이 필요합니다.
        AttackSucceeded,
        // 공격이 종료되었을 때. 후속 효과와의 처리 순서는 소비 측 규칙입니다.
        AttackEnded,
        // 유닛이 사망했을 때.
        UnitDied,
        // 유닛이 피해를 받았을 때.
        UnitDamaged
    }

    /// <summary>Source와 Target에서 사용하는 진영 필터입니다. 아군/적군은 효과 소유자 기준입니다.</summary>
    public enum ArtifactFactionFilter
    {
        // 진영 제한 없음.
        Any,
        // 아군 유닛.
        Ally,
        // 적군 유닛.
        Enemy
    }

    /// <summary>Trigger 발생 당시 게임 상황에 추가로 요구하는 조건입니다.</summary>
    public enum ArtifactConditionType
    {
        // 추가 조건 없음.
        None,
        // 현재 낮 페이즈인 경우.
        IsDay,
        // 현재 밤 페이즈인 경우.
        IsNight,
        // 필드에 배치된 아군 수와 적군 수를 comparison으로 비교합니다.
        CompareUnitCounts
    }

    /// <summary>아군 유닛 수(왼쪽)와 적군 유닛 수(오른쪽)의 비교 방식입니다.</summary>
    public enum ArtifactComparisonType
    {
        // 아군 수가 적군 수보다 적음.
        LessThan,
        // 아군 수와 적군 수가 같음.
        Equal,
        // 아군 수가 적군 수보다 많음.
        GreaterThan
    }

    /// <summary>필드에 배치된 유닛 중 효과 대상을 선택하는 방식입니다. 진영/카테고리/체력 필터와 함께 사용합니다.</summary>
    public enum ArtifactTargetType
    {
        // 필터를 만족하는 모든 유닛.
        AllMatchingUnits,
        // 필터를 만족하는 유닛 중 무작위 한 명.
        RandomMatchingUnit,
        // 마지막으로 공격을 수행한 유닛. 최근 공격 기록의 범위는 별도 확정이 필요합니다.
        LastAttacker,
        // 보스 유닛.
        Boss,
        // Run 진행 상태를 이어받는 메인 캐릭터.
        MainCharacter,
        // 생성된 영혼 위에 실제 도착하여 수집하는 아군 Unit.
        Collector
    }

    /// <summary>전투 필드에서 효과가 적용될 위치입니다. Artifact 배치판의 좌표가 아닙니다.</summary>
    public enum ArtifactLocationType
    {
        // 별도의 위치 지정 없이 Target에 효과를 적용합니다.
        None,
        // 대상 유닛이 현재 배치된 위치.
        TargetPosition,
        // 사망한 유닛의 위치. 소비 측에서 사망 문맥을 제공해야 합니다.
        DeathPosition,
        // 무작위 위치. 선택 가능한 범위는 별도 확정이 필요합니다.
        RandomPosition,
        // 지정 범위. 범위의 기준점과 형태 데이터는 별도 확정이 필요합니다.
        Area
    }

    /// <summary>대상에게 적용할 변화의 종류입니다. 수치와 기간은 ArtifactEffectData에서 지정합니다.</summary>
    public enum ArtifactEffectType
    {
        // 체력 회복. 고정량/잃은 체력 비례 등의 계산 방식은 Value로 구분합니다.
        Heal,
        // 피해 적용.
        Damage,
        // 지정 턴 동안 행동 불가(턴 스킵).
        Stun,
        // Speed 증감.
        ModifySpeed,
        // 넉백 이동. 거리와 별도로 방향 및 막힘 처리 규칙이 필요합니다.
        Knockback,
        // statId로 지정한 스탯 변경.
        ModifyStat,
        // 행동 비용 증감.
        ModifyActionCost,
        // 사망 위치에 Stage 한정 영혼을 생성합니다.
        SpawnSoul
    }

    /// <summary>효과 수치를 고정량 또는 기준값에 대한 백분율로 해석합니다.</summary>
    public enum ArtifactValueType
    {
        // amount를 고정량으로 사용합니다.
        Flat,
        // basis를 기준으로 amount%를 계산합니다. 예: amount가 20이면 20%.
        Percent,
        // 배치 아군보다 많은 적군 1기당 amount를 적용합니다.
        UnitCountDifference
    }

    /// <summary>Percent 수치의 계산 기준입니다. 효과 종류와 호환되는 기준인지 소비 전에 검증해야 합니다.</summary>
    public enum ArtifactValueBasis
    {
        // 비율 기준 없음. Flat에서 사용합니다.
        None,
        // 대상의 잃은 체력(최대 HP - 현재 HP).
        MissingHp,
        // 대상의 최대 HP.
        MaxHp,
        // 변경 대상 스탯의 현재 값. 기존 보정의 포함 여부와 계산 순서는 별도 확정이 필요합니다.
        CurrentStat
    }
}
