using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>스테이지 선택의 Canvas 구성과 화면 연결을 검증합니다. 화면 개폐와 진행 판단은 소유하지 않습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class StageMapCanvasView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private StageSelectPanelUI selection;
        [SerializeField] private StageScreenRootUI screens;
        [Tooltip("레벨업 특성 성장(증강) 선택 화면입니다. 공통 Canvas 바로 아래 마지막 자식으로 배치합니다.")]
        [SerializeField] private TraitGrowthUI traitGrowth;

        internal StageSelectPanelUI Selection => selection;
        internal StageScreenRootUI Screens => screens;
        internal TraitGrowthUI TraitGrowth => traitGrowth;

        /// <summary>공통 Canvas와 기존 등록 화면의 구성 관계를 연결 전에 검사합니다.</summary>
        internal void ValidateComposition()
        {
            if (canvas == null || selection == null || screens == null || traitGrowth == null)
            {
                throw new InvalidOperationException(
                    "[StageMapCanvasView] canvas, selection, screens, traitGrowth 연결이 필요합니다.");
            }
            if (canvas.gameObject != gameObject)
            {
                throw new InvalidOperationException(
                    "[StageMapCanvasView] 공통 Canvas와 같은 오브젝트에 부착해야 합니다.");
            }
            ValidateRootCanvas();
            if (selection.gameObject.scene != gameObject.scene ||
                selection.transform.parent != canvas.transform)
            {
                throw new InvalidOperationException(
                    "[StageMapCanvasView] selection은 같은 씬의 공통 Canvas 바로 아래에 있어야 합니다.");
            }
            if (screens.gameObject.scene != gameObject.scene || screens.transform.parent != canvas.transform)
            {
                throw new InvalidOperationException(
                    "[StageMapCanvasView] screens는 같은 씬의 공통 Canvas 바로 아래에 있어야 합니다.");
            }

            if (traitGrowth.gameObject.scene != gameObject.scene ||
                traitGrowth.transform.parent != canvas.transform ||
                traitGrowth.transform.GetSiblingIndex() != canvas.transform.childCount - 1)
            {
                throw new InvalidOperationException(
                    "[StageMapCanvasView] traitGrowth(UIId.TraitGrowth)는 같은 씬의 공통 Canvas 바로 아래 마지막 자식이어야 합니다.");
            }

            selection.ValidateComposition(canvas);
            Canvas slotInfoCanvas = selection.DeckScreen.ValidateChildCanvases();
            foreach (Canvas childCanvas in canvas.GetComponentsInChildren<Canvas>(true))
            {
                if (childCanvas != canvas && childCanvas != slotInfoCanvas)
                {
                    throw new InvalidOperationException(
                        $"[StageMapCanvasView] '{childCanvas.name}'은 허용되지 않는 중첩 Canvas입니다. UIId.DeckSlotInfo의 정렬용 Canvas만 허용합니다.");
                }
            }
            screens.ValidateComposition();
            traitGrowth.Validate();
        }

        private void ValidateRootCanvas()
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                canvas.GetComponent<CanvasScaler>() == null || canvas.GetComponent<GraphicRaycaster>() == null)
            {
                throw new InvalidOperationException(
                    $"[StageMapCanvasView] canvas '{canvas.name}'는 최상위 Overlay Canvas이며 CanvasScaler와 GraphicRaycaster가 필요합니다.");
            }
        }
    }
}
