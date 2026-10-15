using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// StageSelect 씬에 두는 아티팩트 팝업의 참조 묶음입니다. 동작은 없고 <see cref="ArtifactViewSystem"/>이 씬 진입 때 한 번 찾아 씁니다.
    /// </summary>
    /// <remarks>
    /// 이 오브젝트와 팝업 Canvas는 항상 켜 둡니다(D13). 비활성 오브젝트는 씬 진입 때 찾지 못합니다.
    /// 버튼·목록·문구는 <see cref="ArtifactPopupUI"/>(현강 UI 구조)가 소유합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactSceneRoot : MonoBehaviour
    {
        [Tooltip("3D 보드·모델·배경을 그리는 컴포넌트입니다.")]
        [SerializeField] private ArtifactPopupView popupView;
        [Tooltip("버튼·보유 목록·문구를 맡는 팝업 UI입니다.")]
        [SerializeField] private ArtifactPopupUI popupUI;
        [Tooltip("보드와 배경을 표시하는 실제 출력 카메라입니다. 열 때마다 검색하지 않습니다.")]
        [SerializeField] private Camera outputCamera;

        internal ArtifactPopupView PopupView => popupView;
        internal ArtifactPopupUI PopupUI => popupUI;
        internal Camera OutputCamera => outputCamera;

        /// <summary>참조가 빠졌거나 이 오브젝트가 숨겨지는 Panel 안에 있으면 예외를 던집니다.</summary>
        internal void Validate()
        {
            if (popupView == null || popupUI == null || outputCamera == null)
                throw new InvalidOperationException("[ArtifactSceneRoot] Popup view, popup UI and output camera are required.");

            popupView.Validate();
            popupUI.Validate();
            if (transform.IsChildOf(popupUI.Panel.transform))
                throw new InvalidOperationException("[ArtifactSceneRoot] The scene root must stay outside the panel it hides.");
        }
    }
}
