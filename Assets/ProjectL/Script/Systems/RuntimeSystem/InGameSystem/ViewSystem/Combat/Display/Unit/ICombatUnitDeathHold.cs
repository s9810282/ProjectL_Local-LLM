namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 사망 사실 반영 뒤에도 이미 빌린 모델을 유지하는 대여 하나입니다(연출 조립 설계 B13). 상태·점유·살아 있음의 계약이 아닙니다.
    /// 유지 중에는 사실 그리기가 그 유닛을 지우지 않고 위치·회전도 쓰지 않습니다.
    /// </summary>
    internal interface ICombatUnitDeathHold
    {
        /// <summary>이 유지가 아직 같은 ID·같은 표시의 대여를 붙잡고 있는지입니다.</summary>
        bool IsCurrent { get; }

        /// <summary>유지를 끝내고, 아직 같은 대여면 그 대여를 닫습니다. 반복 해제는 아무 일도 하지 않습니다.</summary>
        void Release();
    }
}
