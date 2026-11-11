using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>전투 카메라 선택과 원래 Priority 복원을 맡습니다. 카탈로그 재수집을 가로질러 Handle을 보관하지 않습니다.</summary>
    internal sealed class CombatCameraSelection
    {
        private readonly ICameraCatalogService _catalog;
        private readonly string _name;
        private Transform _camera;
        private int _originalPriority;
        private bool _selected;

        internal CombatCameraSelection(ICameraCatalogService catalog, CombatCameraSnapshot camera)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _camera = camera.Transform != null ? camera.Transform
                : throw new ArgumentException("카메라 오브젝트가 필요합니다.", nameof(camera));
            _name = camera.Name;
        }

        internal bool HasCamera => _camera != null;

        internal void Begin()
        {
            if (_camera == null)
                throw new InvalidOperationException("[CombatCameraSelection] 선택할 카메라가 해제되거나 파괴됐습니다.");
            if (_selected)
                throw new InvalidOperationException("[CombatCameraSelection] 카메라가 이미 선택됐습니다.");
            if (!string.Equals(_camera.gameObject.name, _name, StringComparison.Ordinal))
                throw new InvalidOperationException($"[CombatCameraSelection] 연결 중 카메라 이름이 바뀌었습니다: {_name}");

            using (ICameraCatalogHandle handle = _catalog.GetHandle(_name))
            {
                _originalPriority = handle.Priority;
                // RaisePriority가 일부 적용 후 실패해도 Session이 원래 값으로 복구합니다.
                _selected = true;
                handle.RaisePriority();
            }
        }

        /// <summary>살아 있는 대상만 카탈로그에서 다시 빌려 복원합니다. 씬 파괴 뒤에는 이름 조회를 하지 않습니다.</summary>
        internal void Release()
        {
            bool restore = _selected && _camera != null;
            _selected = false;
            _camera = null;
            if (!restore)
                return;

            using (ICameraCatalogHandle handle = _catalog.GetHandle(_name))
                handle.SetPriority(_originalPriority);
        }
    }
}
