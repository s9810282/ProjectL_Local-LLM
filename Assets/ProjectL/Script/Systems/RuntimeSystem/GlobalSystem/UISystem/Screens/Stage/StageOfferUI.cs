using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 이벤트 스테이지(용병·상점·훈련·회복) 공통 화면입니다. 레이아웃은 하나이고, 상점 길과 회복 길은 입력 연결·잠금·열림 인수가
    /// 서로 부딪치지 않게 인스턴스를 나눕니다(공통 레이아웃 설계 SO4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageOfferUI : StageOfferScreenUI
    {
        [Tooltip("이 인스턴스의 등록 ID입니다. 상점 길은 Shop, 회복 길은 Recovery입니다.")]
        [SerializeField] private UIId screenId = UIId.Shop;

        public override UIId ID => screenId;
        public override UIId ParentID => UIId.StageScreenRoot;

        protected override bool RequiresScrollPresentation => true;
    }
}
