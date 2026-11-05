using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// Lobby 씬에 두는 아티팩트 팝업의 참조 묶음입니다. 동작은 없고 로비 연결(<c>ArtifactPopupViewFactory.FindSceneRoot</c>)이 씬 진입 때 한 번 찾아 씁니다(로비 리팩토링 2차 설계 N-76).
    /// </summary>
    /// <remarks>
    /// 이 오브젝트와 공통 LobbyCanvas는 항상 켜 둡니다(D13). 비활성 오브젝트는 씬 진입 때 찾지 못합니다.
    /// 버튼·목록·문구는 <see cref="ArtifactPopupUI"/>(현강 UI 구조)가 소유합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactSceneRoot : MonoBehaviour
    {
        [Tooltip("3D 보드·모델·드래그 미리보기를 그리는 컴포넌트입니다.")]
        [SerializeField] private ArtifactPopupView popupView;
        [Tooltip("버튼·보유 목록·문구를 맡는 팝업 UI입니다.")]
        [SerializeField] private ArtifactPopupUI popupUI;

        internal ArtifactPopupView PopupView => popupView;
        internal ArtifactPopupUI PopupUI => popupUI;

        /// <summary>참조가 빠졌거나 이 오브젝트가 숨겨지는 Panel 안에 있으면 예외를 던집니다.</summary>
        /// <remarks>출력 카메라는 씬 소유가 아니라 로비 연결 때 받아 팝업에 넘깁니다(카메라 이전 설계 L1).</remarks>
        internal void Validate()
        {
            if (popupView == null || popupUI == null)
                throw new InvalidOperationException("[ArtifactSceneRoot] 팝업 표시(popupView)와 팝업 UI(popupUI) 참조가 필요합니다.");

            popupView.Validate();
            popupUI.Validate();
            if (transform.IsChildOf(popupUI.Panel.transform))
                throw new InvalidOperationException("[ArtifactSceneRoot] 씬 루트는 팝업 UI가 숨기는 Panel 밖에 있어야 합니다.");
        }
    }
}
