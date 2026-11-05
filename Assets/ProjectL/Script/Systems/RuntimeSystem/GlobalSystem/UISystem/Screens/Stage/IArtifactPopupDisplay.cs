using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 아티팩트 UI의 표시 계약입니다(보유 목록·상태 문구·상세). 게임 규칙이나 View 타입을 참조하지 않습니다.
    /// 수명은 <see cref="IArtifactPopupLifetime"/>, 화면 영역 판정은 <see cref="IArtifactPopupScreenAreas"/>가 맡습니다
    /// (로비 리팩토링 2차 설계 N-57).
    /// </summary>
    public interface IArtifactPopupDisplay
    {
        void ShowStatus(string message);
        void ShowDetail(string displayName, string description);
        void RenderPool(IReadOnlyList<ArtifactPoolItemContent> items);
    }
}
