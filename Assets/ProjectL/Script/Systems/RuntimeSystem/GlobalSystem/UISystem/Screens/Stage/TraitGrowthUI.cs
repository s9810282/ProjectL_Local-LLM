using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 레벨업 특성 성장(증강) 선택 화면입니다. 이벤트 스테이지와 같은 레이아웃 부품을 쓰고 프리팹은 `TraitUI`로 따로 두며, 골드·나가기는 그리지 않습니다(공통 레이아웃 설계 R4).
    /// </summary>
    /// <remarks>
    /// Parallel인 이유: Exclusive면 열 때 상점·회복·덱 편집 팝업을 그 Scope를 거치지 않고 닫습니다(설계 §14.2 UI 정책).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TraitGrowthUI : StageOfferScreenUI
    {
        // 루트가 꺼진 채 로드돼 Awake 전에 UISystem이 열 때도 Parallel이도록 생성자에서 정합니다(UIWidgetBase와 같음, 검토 L3).
        public TraitGrowthUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.TraitGrowth;

        protected override bool RequiresScrollPresentation => true;

        // 나가기가 없어 키보드·컨트롤러로도 고를 수 있게 첫 카드에 초점을 둡니다(Core 인계 §7).
        protected override bool FocusFirstCardOnRender => true;
    }
}
