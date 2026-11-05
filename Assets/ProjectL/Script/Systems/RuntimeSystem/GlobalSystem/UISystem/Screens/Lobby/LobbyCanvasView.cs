using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>로비 Canvas의 공통 구성과 화면 연결을 검증합니다. 화면 개폐와 씬 수명은 소유하지 않습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbyCanvasView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private LobbyBackgroundView background;
        [SerializeField] private LobbyDeckUI deckUI;
        [Tooltip("로비 화면에만 보이는 글자(제목·시설 안내)를 묶은 부모입니다. 시설 팝업 동안 숨깁니다. 팝업은 자기 제목을 갖습니다.")]
        [SerializeField] private GameObject lobbyTexts;

        internal LobbyBackgroundView Background => background;
        internal LobbyDeckUI DeckScreen => deckUI;
        internal GameObject LobbyTexts => lobbyTexts;

        internal void ValidateComposition()
        {
            if (canvas == null || background == null || deckUI == null || lobbyTexts == null)
                throw new InvalidOperationException("[LobbyCanvasView] canvas, background, deckUI, lobbyTexts 연결이 필요합니다.");
            if (canvas.gameObject != gameObject || !canvas.isRootCanvas ||
                canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                canvas.GetComponent<UnityEngine.UI.CanvasScaler>() == null ||
                canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                throw new InvalidOperationException("[LobbyCanvasView] 같은 오브젝트에 최상위 Overlay Canvas와 CanvasScaler·GraphicRaycaster가 필요합니다.");
            if (background.gameObject.scene != gameObject.scene || background.gameObject == gameObject)
                throw new InvalidOperationException("[LobbyCanvasView] background는 같은 씬의 별도 배경 Canvas에 있어야 합니다.");
            if (deckUI.gameObject.scene != gameObject.scene || deckUI.GetComponentInParent<Canvas>(true) != canvas)
                throw new InvalidOperationException("[LobbyCanvasView] deckUI는 같은 씬의 지정 Canvas에서 그려져야 합니다.");

            background.ValidateComposition();
            deckUI.Validate();
        }
    }
}
