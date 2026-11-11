using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 선택 화면의 범례를 함께 표시하는 그룹입니다.
    /// DeckEditUI는 그룹 멤버가 아닌 별도 등록 자식으로 연결합니다.
    /// </summary>
    public sealed class StageSelectPanelUI : UIGroup
    {
        [Tooltip("그룹 멤버가 아닌 독립 등록 자식입니다. 내부 OpenButton과 Panel은 덱 화면이 소유합니다.")]
        [SerializeField] private DeckEditUI deckEditUI;

        public StageSelectPanelUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.StageSelect;

        internal DeckEditUI DeckScreen => deckEditUI;

        /// <summary>범례 소유권을 확인합니다. 덱 편집 등 독립 화면은 그룹 멤버로 넣지 않습니다.</summary>
        internal void ValidateComposition(Canvas canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }
            if (OwnerGroup != null || ParentID != UIId.None)
            {
                throw new InvalidOperationException("[StageSelectPanelUI] 선택 그룹은 독립 등록 루트여야 합니다.");
            }
            if (deckEditUI == null || deckEditUI.gameObject.scene != gameObject.scene ||
                deckEditUI.transform.parent != transform ||
                deckEditUI.GetComponentInParent<Canvas>(true) != canvas || deckEditUI.OwnerGroup != null ||
                deckEditUI.ParentID != ID)
            {
                throw new InvalidOperationException(
                    "[StageSelectPanelUI] deckEditUI는 같은 씬의 공통 Canvas 아래 실제 자식이며 그룹 멤버가 아닌 독립 등록 자식이어야 합니다.");
            }
            // 덱 편집 화면 안쪽 구성도 씬 연결 때 검사합니다. 상점·회복 화면의 Validate와 같은 꼴입니다(평가 C-26).
            deckEditUI.Validate();
            int legendCount = 0;
            foreach (UIBase member in Instances)
            {
                if (member == null || member.OwnerGroup != this ||
                    member.gameObject.scene != gameObject.scene ||
                    member.GetComponentInParent<Canvas>(true) != canvas ||
                    member is DeckEditUI || member is StageOfferScreenUI)
                {
                    throw new InvalidOperationException(
                        "[StageSelectPanelUI] 멤버는 같은 씬의 지정 Canvas에 속한 상시 표시 UI여야 하며 독립 팝업을 넣을 수 없습니다.");
                }
                if (member is StageLegendUI) legendCount++;
            }
            if (legendCount != 1)
            {
                throw new InvalidOperationException(
                    "[StageSelectPanelUI] initialInstances에 StageLegendUI 한 개가 필요합니다.");
            }
        }
    }
}
