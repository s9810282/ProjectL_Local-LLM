using System;
using Cinemachine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 진행 축으로만 카메라를 움직입니다. 카메라 설정 변경은 대여한 Catalog 핸들로 전달합니다.</summary>
    internal sealed class StageMapCameraDrag : IStageMapCameraDrag, IDisposable
    {
        private readonly Camera _output;
        private readonly CameraCatalogData _initial;
        private CameraCatalogData _framing;
        private readonly string _cameraName;
        private readonly ViewCameraHandleLease _lease = new("[StageMap]");
        private ICameraCatalogService _catalog;
        private Vector3 _axis;
        private Vector3 _start;
        private Vector3 _boss;
        private Vector3 _normal;
        private Vector2 _screenAxis;
        private Vector2 _lastPointer;
        private float _minimum;
        private float _maximum;
        private float _offset;
        private bool _hasBounds;
        private bool _dragging;
        private bool _pending;
        private bool _disposed;
#if UNITY_EDITOR
        private Editor.StageMapCameraFramingSettings _previewSettings;
#endif

        /// <summary>카메라를 검증하고 초기 설정만 복사합니다. 핸들 대여는 <see cref="BindHandle"/>까지 미룹니다.</summary>
        internal StageMapCameraDrag(Camera output, CinemachineVirtualCamera camera, ICameraCatalogService catalog)
        {
            _output = output != null ? output : throw new ArgumentNullException(nameof(output));
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _initial = new CameraCatalogData(camera);
            _framing = _initial;
            if (camera.Follow != null || camera.LookAt != null)
                throw new InvalidOperationException("[StageMap] 드래그 카메라는 타깃이 없는 직교 카메라여야 합니다.");
            _cameraName = camera.gameObject.name;
        }

        internal bool HasHandle => _lease.HasHandle;

        /// <summary>
        /// Catalog의 마지막 씬 수집 뒤에 핸들을 한 번 빌립니다. 수집은 이전 핸들을 모두 무효로 만들므로
        /// 그 전에 빌리면 드래그 때 반환된 핸들을 쓰게 됩니다. 대여 전에 정한 위치는 대여할 때 적용합니다.
        /// </summary>
        internal void BindHandle()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StageMapCameraDrag));
            // 대여 중 끝난 연결의 늦게 받은 핸들은 대여 부품이 바로 반환합니다.
            _lease.Borrow(_catalog, _cameraName);
            if (_lease.HasHandle && _pending) Apply();
        }

        /// <summary>실제로 표시된 두 끝 노드에서 이동 범위를 구합니다. 끝에서는 해당 노드를 진행 축의 화면 중앙까지 볼 수 있습니다.</summary>
        internal void SetBounds(Vector3 start, Vector3 boss, Vector3 axis, Vector3 normal)
        {
            EndDrag();
            _axis = axis.normalized;
            var plane = new Plane(normal, start);
            var ray = new Ray(_framing.Position, _framing.Rotation * Vector3.forward);
            if (_axis.sqrMagnitude < 0.99f || !plane.Raycast(ray, out float distance))
                throw new InvalidOperationException("[StageMap] 카메라가 맵 평면을 향하지 않습니다.");
            Vector3 center = ray.GetPoint(distance);
            _start = start;
            _boss = boss;
            _normal = normal;
            _minimum = Vector3.Dot(start - center, _axis);
            _maximum = Vector3.Dot(boss - center, _axis);
            if (_maximum < _minimum)
                throw new InvalidOperationException("[StageMap] Boss는 시작 노드의 진행 방향에 있어야 합니다.");
            // 확정한 구도 자체도 허용합니다. 재생 시작 때 작성한 위치를 강제로 옮기지 않습니다.
            _minimum = Mathf.Min(0f, _minimum);
            _maximum = Mathf.Max(0f, _maximum);
            _hasBounds = true;
#if UNITY_EDITOR
            if (_previewSettings != null) return;
#endif
            SetOffset(_offset);
        }

        public void BeginDrag(float screenX, float screenY)
        {
            EndDrag();
            if (!_hasBounds || !_lease.HasHandle || _output == null) return;
            // 직교 투영에서 진행 축 1m의 화면 이동량입니다. 해상도·화면비가 바뀌어도 잡은 만큼 이동합니다.
            _screenAxis = (Vector2)(_output.WorldToScreenPoint(_start + _axis) - _output.WorldToScreenPoint(_start));
            if (_screenAxis.sqrMagnitude < 0.001f) return;
            _lastPointer = new Vector2(screenX, screenY);
            _dragging = true;
        }

        public void MoveDrag(float screenX, float screenY)
        {
            if (!_dragging || !_lease.HasHandle) return;
            Vector2 pointer = new Vector2(screenX, screenY);
            Vector2 delta = pointer - _lastPointer;
            _lastPointer = pointer;
            // 맵을 잡아 끄는 방향으로 보이도록 카메라는 반대 방향으로 움직입니다.
            SetOffset(_offset - Vector2.Dot(delta, _screenAxis) / _screenAxis.sqrMagnitude);
        }

        public void EndDrag()
        {
            _dragging = false;
        }

        public void Dispose()
        {
            _disposed = true;
            EndDrag();
            _catalog = null;
            _lease.Dispose();
        }

        private void SetOffset(float value)
        {
            float clamped = Mathf.Clamp(value, _minimum, _maximum);
#if UNITY_EDITOR
            if (_previewSettings != null)
            {
                // 일반 맵 드래그도 작업 좌표에 반영해 Inspector와 표시 위치를 일치시킵니다.
                bool dragging = _dragging;
                if (_previewSettings.TrySetPose(_framing.Position + _axis * clamped, _previewSettings.Angles))
                    PreviewFraming(_previewSettings);
                _dragging = dragging;
                return;
            }
#endif
            if (Mathf.Approximately(_offset, clamped)) return;
            _offset = clamped;
            if (!_lease.HasHandle)
            {
                _pending = true;
                return;
            }
            Apply();
        }

        private void Apply()
        {
            _pending = false;
            CameraCatalogData data = _framing;
            data.Position += _axis * _offset;
            _lease.Handle.Customize(data);
        }

#if UNITY_EDITOR
        /// <summary>에디터 작업 구도를 기존 핸들에 반영합니다. 씬의 확정값은 바꾸지 않습니다.</summary>
        internal void PreviewFraming(Editor.StageMapCameraFramingSettings settings)
        {
            if (_disposed || !_hasBounds) return;
            bool enabled = settings.IsOpen;
            bool changedMode = enabled != (_previewSettings != null);
            CameraCatalogData candidate = _initial;
            var confirmed = settings.Confirmed;
            if (confirmed != null)
            {
                candidate.Position = confirmed.position;
                candidate.Rotation = Quaternion.Euler(confirmed.angles);
                candidate.Lens.OrthographicSize = confirmed.size;
            }
            if (enabled)
            {
                if (!settings.IsValid(settings.Position, settings.Angles))
                    throw new InvalidOperationException("[StageMap] 조정한 카메라는 유한한 좌표로 맵 평면을 향해야 합니다.");
                candidate.Position = settings.Position;
                candidate.Rotation = Quaternion.Euler(settings.Angles);
                if (!Editor.StageMapCameraFramingSettings.IsValidSize(settings.Size))
                    throw new InvalidOperationException("[StageMap] 카메라 Size는 0.01 이상의 유한한 값이어야 합니다.");
                candidate.Lens.OrthographicSize = settings.Size;
            }
            if (!changedMode && candidate.Position == _framing.Position && candidate.Rotation == _framing.Rotation &&
                candidate.Lens.OrthographicSize == _framing.Lens.OrthographicSize) return;
            var plane = new Plane(_normal, _start);
            if (!plane.Raycast(new Ray(candidate.Position, candidate.Rotation * Vector3.forward), out _))
                throw new InvalidOperationException("[StageMap] 조정한 카메라는 맵 평면을 향해야 합니다.");
            _framing = candidate;
            _previewSettings = enabled ? settings : null;
            _offset = 0f;
            SetBounds(_start, _boss, _axis, _normal);
            // 구도만 바뀌고 offset이 같을 때도 반드시 적용합니다.
            if (_lease.HasHandle) Apply();
            else _pending = true;
        }
#endif
    }
}
