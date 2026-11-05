using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>배경 내부 참조와 출력 연결을 제공합니다. 연결의 보관·해제 시점은 씬 Scope가 결정합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbyBackgroundView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image image;

        internal void ValidateComposition()
        {
            if (canvas == null || image == null)
                throw new InvalidOperationException("[LobbyBackgroundView] canvas와 image 연결이 필요합니다.");
            if (canvas.gameObject != gameObject || !canvas.isRootCanvas)
                throw new InvalidOperationException("[LobbyBackgroundView] 같은 오브젝트의 최상위 배경 Canvas가 필요합니다.");
            if (image.gameObject.scene != gameObject.scene || image.canvas != canvas)
                throw new InvalidOperationException($"[LobbyBackgroundView] image '{image.name}'는 지정한 배경 Canvas '{canvas.name}'에서 그려져야 합니다.");
            if (image.raycastTarget)
                throw new InvalidOperationException($"[LobbyBackgroundView] image '{image.name}'가 UI 입력을 가리지 않도록 Raycast Target을 꺼야 합니다.");
        }

        /// <summary>출력 Camera를 연결하고 렌즈 범위를 검사합니다. 실패하면 연결 전 값을 복원합니다.</summary>
        internal IDisposable ConnectOutput(Camera output, float nearClip, float farClip, Func<bool> isCurrent)
        {
            // 구성 검사는 씬 연결(LobbyCanvasView.ValidateComposition) 때 한 번 합니다(N-22).
            return LobbyBackgroundCameraBinding.Create(canvas, output, nearClip, farClip, isCurrent);
        }
    }
}
