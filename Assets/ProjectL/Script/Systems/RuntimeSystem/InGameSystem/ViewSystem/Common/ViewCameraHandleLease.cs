using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 씬 카메라 하나의 Catalog 핸들 대여입니다(검사 후속 정리 설계 Q-R12b). 스테이지 맵 카메라 끌기와 로비 씬 표시가 씁니다.
    /// </summary>
    /// <remarks>
    /// Catalog는 씬 로드 뒤 AfterLoad에서 카메라를 다시 수집하고, 그 전에 빌린 핸들은 무효가 됩니다. 그래서 빌리는 때는 AfterLoad 뒤입니다.
    /// 이미 돌려준 대여가 늦게 핸들을 받으면 바로 돌려줍니다. 핸들 Dispose는 카메라 설정을 원복하지 않습니다.
    /// </remarks>
    internal sealed class ViewCameraHandleLease : IDisposable
    {
        private readonly string _owner;
        private ICameraCatalogHandle _handle;
        private bool _disposed;

        /// <param name="owner">예외 문구 머리(예: "[StageMap]")입니다.</param>
        internal ViewCameraHandleLease(string owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        internal bool HasHandle => _handle != null;

        /// <summary>빌린 핸들입니다. 빌리기 전·돌려준 뒤에는 null입니다.</summary>
        internal ICameraCatalogHandle Handle => _handle;

        /// <summary>카메라 GameObject 이름으로 핸들을 빌립니다. 이름의 유일성은 Catalog가 씬 연결 때 검사합니다.</summary>
        internal void Borrow(ICameraCatalogService catalog, string cameraName)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (string.IsNullOrEmpty(cameraName)) throw new ArgumentException("카메라 이름이 필요합니다.", nameof(cameraName));
            if (_disposed) throw new ObjectDisposedException(nameof(ViewCameraHandleLease));
            if (_handle != null) throw new InvalidOperationException($"{_owner} 카메라 핸들을 이미 대여했습니다.");
            ICameraCatalogHandle handle = catalog.GetHandle(cameraName)
                ?? throw new InvalidOperationException($"{_owner} Catalog가 카메라 핸들을 제공하지 않았습니다.");
            if (_disposed)
            {
                handle.Dispose();
                return;
            }
            _handle = handle;
        }

        public void Dispose()
        {
            _disposed = true;
            ICameraCatalogHandle handle = _handle;
            _handle = null;
            handle?.Dispose();
        }
    }
}
