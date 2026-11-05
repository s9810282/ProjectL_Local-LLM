namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 이미 빌린 유닛 표시 한 번의 크기 배율 대여입니다(보스 활성화 설계 D6b, 포효 커지기).
    /// </summary>
    /// <remarks>
    /// 위치·회전 자세 대여(<see cref="ICombatUnitPose"/>)와 따로 있어, 커진 채로 공격 자세를 재생할 수 있습니다.
    /// 배율은 대여를 시작할 때의 크기에 곱하고, <see cref="RestoreScale"/>이 그 크기로 되돌립니다.
    /// 구현은 ID·대여 수명을 확인하고, 다음 대여를 이전 RestoreScale이 수정하지 못하게 합니다.
    /// </remarks>
    internal interface ICombatUnitScale
    {
        bool IsAvailable { get; }
        void ApplyScale(float multiplier);
        void RestoreScale();
    }
}
