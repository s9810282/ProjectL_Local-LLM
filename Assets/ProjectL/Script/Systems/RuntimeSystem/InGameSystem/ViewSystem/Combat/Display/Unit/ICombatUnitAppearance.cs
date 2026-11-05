namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 외형 층 대여 하나입니다(연출 조립 설계 B13). 지금 층은 이동 투명도(입력 미리보기의 원본 흐림) 하나이고, 플래시는 B 단계에서 더합니다.
    /// 다른 대여가 그 층을 가져가거나 표시가 반환되면 이 대여의 <see cref="Restore"/>는 아무 일도 하지 않습니다.
    /// </summary>
    internal interface ICombatUnitAppearance
    {
        /// <summary>이 대여가 아직 그 층을 소유하는지입니다.</summary>
        bool IsCurrent { get; }

        /// <summary>층을 원래 외형으로 되돌립니다. 여러 번 불러도 한 번만 되돌립니다.</summary>
        void Restore();
    }
}
