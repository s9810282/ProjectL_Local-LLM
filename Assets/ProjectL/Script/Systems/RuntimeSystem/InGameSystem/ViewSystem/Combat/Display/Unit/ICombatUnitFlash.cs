using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 하나의 피격 플래시 층 대여입니다(조각·조립 설계 BP9). 유닛마다 하나이고, 이동 반투명 층과는 표시의 외형 소유자가 합쳐 씁니다.
    /// </summary>
    /// <remarks>
    /// 구현은 ID·대여 수명을 확인하고, 다음 대여를 이전 <see cref="RestoreFlash"/>가 수정하지 못하게 합니다.
    /// 표시가 꺼지거나 다시 대여되면 대여가 취소되고, 시작 때 받은 취소 콜백을 부릅니다.
    /// 외곽선 강조 대여(<see cref="ICombatUnitOutline"/>)와는 다른 층입니다. 둘이 같이 있으면 강조 외곽선 위에 플래시 외곽선을 섞습니다.
    /// </remarks>
    internal interface ICombatUnitFlash
    {
        bool IsAvailable { get; }

        /// <summary>플래시 모습(몸 색·외곽선)을 세기(0~1)만큼 섞어 씁니다. 0이면 플래시 층만 지웁니다.</summary>
        void SetFlash(CombatFlashLook look, float strength);

        /// <summary>플래시 층을 끝냅니다. 이미 끝났거나 다른 대여로 바뀌었으면 아무 일도 하지 않습니다.</summary>
        void RestoreFlash();
    }
}
