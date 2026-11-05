using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>아티팩트 UI의 화면 영역 판정 계약입니다. 상태를 바꾸지 않습니다(로비 리팩토링 2차 설계 N-57).</summary>
    public interface IArtifactPopupScreenAreas
    {
        bool TryPickPool(Vector2 screen, out int artifactId);
        bool IsReturnArea(Vector2 screen);
        bool IsBlockingArea(Vector2 screen);

        /// <summary>포인터가 보유 목록 슬롯 위에 있는지입니다. 슬롯 상세는 UI가 갖으므로 보드 호버는 이때 상세를 바꾸지 않습니다(N-83).</summary>
        bool IsPoolHovered { get; }
    }
}
