using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>배경 출력 연결 한 번의 원래 값을 소유하며 현재 연결만 복원합니다.</summary>
    internal sealed class LobbyBackgroundCameraBinding : IDisposable
    {
        private readonly Func<bool> _isCurrent;
        private Canvas _canvas;
        private Camera _cameraBefore;
        private bool _disposed;

        private LobbyBackgroundCameraBinding(Canvas canvas, Func<bool> isCurrent)
        {
            _canvas = canvas;
            _cameraBefore = canvas.worldCamera;
            _isCurrent = isCurrent;
        }

        internal static LobbyBackgroundCameraBinding Create(Canvas canvas, Camera output,
            float nearClip, float farClip, Func<bool> isCurrent)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (isCurrent == null) throw new ArgumentNullException(nameof(isCurrent));
            var binding = new LobbyBackgroundCameraBinding(canvas, isCurrent);
            try
            {
                canvas.worldCamera = output;
                // 출력 연결 전에는 Camera 모드가 Overlay처럼 관측될 수 있으므로 연결 후 검사합니다.
                if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    throw new InvalidOperationException($"[LobbyBackgroundCameraBinding] 연결된 Canvas '{canvas.name}'는 Screen Space - Camera여야 합니다.");
                // Brain이 적용할 가상 카메라 렌즈를 사용합니다. 카메라 서비스는 여기서 조회하지 않습니다.
                if (canvas.planeDistance <= nearClip || canvas.planeDistance >= farClip)
                    throw new InvalidOperationException("[LobbyBackgroundCameraBinding] 배경 Canvas는 로비 카메라 렌즈의 표시 깊이 안에 있어야 합니다.");
                return binding;
            }
            catch
            {
                binding.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Canvas canvas = _canvas;
            Camera cameraBefore = _cameraBefore;
            _canvas = null;
            _cameraBefore = null;
            // 씬이 먼저 파괴됐거나 교체된 Scope의 늦은 정리이면 새 출력을 건드리지 않습니다.
            if (canvas != null && _isCurrent()) canvas.worldCamera = cameraBefore;
        }
    }
}
