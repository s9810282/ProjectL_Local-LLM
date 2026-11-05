using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 지금 이동 모드인 행위자를 읽는 좁은 계약입니다(호버 공격 범위 설계 §9). 이동 모드는 선택한 행위자의 칸을 눌러 경로 지정을 시작할 수 있는 상태이고,
    /// 판정은 경로 입력이 이동 모드 범위·Drag 시작과 같은 조건 하나로 합니다. 유닛 호버(<see cref="CombatUnitRangeHover"/>)는 이 행위자를 호버 대상에서 뺍니다.
    /// </summary>
    internal interface ICombatMoveModeReader
    {
        /// <summary>이동 모드인 행위자입니다. 이동 모드가 아니면 빈 값입니다.</summary>
        EntityId MoveModeActorId { get; }
    }
}
