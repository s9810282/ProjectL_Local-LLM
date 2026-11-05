using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>비전투 스테이지 화면의 부모 수명입니다. 자식 화면은 각자 열고 닫습니다.</summary>
    public sealed class StageScreenRootUI : UIBase
    {
        [SerializeField] private StageOfferUI shopUI;
        [SerializeField] private StageOfferUI recoveryUI;

        public StageScreenRootUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.StageScreenRoot;

        internal StageOfferUI ShopScreen => shopUI;
        internal StageOfferUI RecoveryScreen => recoveryUI;

        /// <summary>상점·회복 화면이 모두 연결됐는지와 독립 자식 관계를 검사하고 내부 구성 검증은 각 화면에 맡깁니다.</summary>
        internal void ValidateComposition()
        {
            if (OwnerGroup != null || ParentID != UIId.None)
            {
                throw new InvalidOperationException("[StageScreenRootUI] 화면 부모는 독립 등록 루트여야 합니다.");
            }
            if (shopUI == null)
                throw new InvalidOperationException($"[StageScreenRootUI] {nameof(shopUI)} 연결이 필요합니다.");
            if (recoveryUI == null)
                throw new InvalidOperationException($"[StageScreenRootUI] {nameof(recoveryUI)} 연결이 필요합니다.");
            if (shopUI == recoveryUI || shopUI.ID != UIId.Shop || recoveryUI.ID != UIId.Recovery)
                throw new InvalidOperationException($"[StageScreenRootUI] {nameof(shopUI)}의 screenId는 Shop, {nameof(recoveryUI)}의 screenId는 Recovery여야 합니다.");
            ValidateChild(shopUI, nameof(shopUI));
            shopUI.Validate();
            ValidateChild(recoveryUI, nameof(recoveryUI));
            recoveryUI.Validate();
        }

        private void ValidateChild(UIBase screen, string field)
        {
            if (screen.gameObject.scene != gameObject.scene || screen.transform.parent != transform ||
                screen.OwnerGroup != null || screen.ParentID != ID)
            {
                throw new InvalidOperationException(
                    $"[StageScreenRootUI] {field}는 같은 씬의 실제 자식이며 그룹 멤버가 아닌 독립 등록 자식이어야 합니다.");
            }
        }

        /// <summary>부모 표시 수명이 끝났음을 알립니다.</summary>
        internal event Action Hidden;

        protected override void OnUIHidden() => Hidden?.Invoke();
    }
}
