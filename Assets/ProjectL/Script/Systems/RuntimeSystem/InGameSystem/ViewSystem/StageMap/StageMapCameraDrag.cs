using System;
using Cinemachine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 진행 축으로만 카메라를 움직입니다. 카메라 설정 변경은 대여한 Catalog 핸들로 전달합니다.</summary>
    internal sealed class StageMapCameraDrag : IStageMapCameraDrag, IStageMapCameraTourControl,
        IStageMapCameraFocusControl, IDisposable
    {
        // Input System의 기본 정규화 휠 1단위마다 화면 높이의 10%만큼 이동합니다. 입력량을 그대로 곱합니다.
        private const float ScrollViewportFraction = 0.1f;
        private readonly Camera _output;
        // 씬에 저장된 맵 카메라 구도입니다. 이동은 이 구도에서 진행 축 offset만 더합니다.
        private readonly CameraCatalogData _framing;
        private readonly string _cameraName;
        private readonly ViewCameraHandleLease _lease = new("[StageMap]");
        private ICameraCatalogService _catalog;
        private Vector3 _axis;
        private Vector3 _start;
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
        private long _nextAutomaticGeneration;
        private long _tourGeneration;
        private long _focusGeneration;
        private float _focusStartOffset;
        private float _tourStartOffset;
        private float _tourBossOffset;

        /// <summary>카메라를 검증하고 초기 설정만 복사합니다. 핸들 대여는 <see cref="BindHandle"/>까지 미룹니다.</summary>
        internal StageMapCameraDrag(Camera output, CinemachineVirtualCamera camera, ICameraCatalogService catalog)
        {
            _output = output != null ? output : throw new ArgumentNullException(nameof(output));
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _framing = new CameraCatalogData(camera);
            if (camera.Follow != null || camera.LookAt != null)
                throw new InvalidOperationException("[StageMap] 드래그 카메라는 타깃이 없는 직교 카메라여야 합니다.");
            _cameraName = camera.gameObject.name;
        }

        internal bool HasHandle => _lease.HasHandle;
        public bool IsReady => !_disposed && _hasBounds && _lease.HasHandle;
        private bool IsAutomatic => _tourGeneration != 0 || _focusGeneration != 0;

        public StageMapCameraFocusFrame BeginFocus()
        {
            if (!IsReady || IsAutomatic || _output == null)
                throw new InvalidOperationException("[StageMapCameraDrag] 준비된 카메라의 배타 정렬 소유권이 필요합니다.");
            float aspect = _output.aspect;
            EndDrag();
            long generation = checked(++_nextAutomaticGeneration);
            _focusStartOffset = _offset;
            _focusGeneration = generation;
            return new StageMapCameraFocusFrame(generation, _framing, _axis, _minimum, _maximum,
                _offset, aspect);
        }

        public bool IsFocusCurrent(long generation) => generation > 0 && generation == _focusGeneration && IsReady;

        public bool TrySetFocusOffset(long generation, float offset)
        {
            if (!IsFocusCurrent(generation)) return false;
            if (!float.IsFinite(offset) || offset < _minimum || offset > _maximum)
                throw new ArgumentOutOfRangeException(nameof(offset), "카메라 정렬 offset이 유효한 이동 범위를 벗어났습니다.");
            _offset = offset;
            Apply();
            return IsFocusCurrent(generation);
        }

        public void EndFocus(long generation, bool restoreStart)
        {
            if (generation <= 0 || generation != _focusGeneration) return;
            _focusGeneration = 0;
            EndDrag();
            if (!restoreStart || !IsReady) return;
            _offset = _focusStartOffset;
            Apply();
        }

        public long BeginTour(Vector3 playerPosition, Vector3 bossPosition)
        {
            if (!IsReady || IsAutomatic)
                throw new InvalidOperationException("[StageMapCameraDrag] 준비된 카메라의 자동 소유권이 필요합니다.");
            EndDrag();
            float startOffset = _offset;
            if (!IsVisible(playerPosition, startOffset))
                startOffset = Mathf.Clamp(ProjectOffset(playerPosition), _minimum, _maximum);
            float bossOffset = ProjectOffset(bossPosition);
            if (!IsVisible(playerPosition, startOffset) || !IsVisible(bossPosition, bossOffset))
                throw new InvalidOperationException("[StageMapCameraDrag] 진행 축 이동만으로 플레이어와 보스를 화면에 넣을 수 없습니다. 시작 구도를 확인하세요.");
            _tourStartOffset = startOffset;
            _tourBossOffset = bossOffset;
            _offset = startOffset;
            long generation = checked(++_nextAutomaticGeneration);
            _tourGeneration = generation;
            try
            {
                Apply();
            }
            catch
            {
                if (_tourGeneration == generation) _tourGeneration = 0;
                throw;
            }
            return generation;
        }

        public void SetTourProgress(long generation, float progress)
        {
            if (!OwnsTour(generation)) return;
            if (!float.IsFinite(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
            // 투어 offset은 시작·Boss 사이 보간이라 이미 범위 안입니다. 끌기용 SetOffset의 자르기·같음 검사를 거치지 않습니다.
            _offset = Mathf.LerpUnclamped(_tourStartOffset, _tourBossOffset, Mathf.Clamp01(progress));
            Apply();
        }

        public void EndTour(long generation, bool restoreStart)
        {
            if (generation <= 0 || generation != _tourGeneration) return;
            _tourGeneration = 0;
            EndDrag();
            if (!restoreStart || !IsReady) return;
            _offset = _tourStartOffset;
            Apply();
        }

        private bool OwnsTour(long generation) => generation > 0 && generation == _tourGeneration && IsReady;

        private float ProjectOffset(Vector3 position)
        {
            var plane = new Plane(_normal, _start);
            var ray = new Ray(_framing.Position, _framing.Rotation * Vector3.forward);
            if (!plane.Raycast(ray, out float distance))
                throw new InvalidOperationException("[StageMapCameraDrag] 시작 구도가 맵 평면을 향해야 합니다.");
            float offset = Vector3.Dot(position - ray.GetPoint(distance), _axis);
            if (!float.IsFinite(offset)) throw new InvalidOperationException("[StageMapCameraDrag] 대상 위치가 유한하지 않습니다.");
            return Mathf.Clamp(offset, _minimum, _maximum);
        }

        private bool IsVisible(Vector3 position, float offset)
        {
            Quaternion rotation = _framing.Rotation * Quaternion.AngleAxis(_framing.Lens.Dutch, Vector3.forward);
            Vector3 local = Quaternion.Inverse(rotation) * (position - (_framing.Position + _axis * offset));
            float halfHeight = _framing.Lens.Orthographic ? _framing.Lens.OrthographicSize :
                local.z * Mathf.Tan(_framing.Lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
            float halfWidth = halfHeight * _output.aspect;
            return float.IsFinite(local.x) && float.IsFinite(local.y) && float.IsFinite(local.z) &&
                float.IsFinite(halfHeight) && float.IsFinite(halfWidth) && halfHeight > 0f && halfWidth > 0f &&
                local.z > Mathf.Max(0f, _framing.Lens.NearClipPlane) && local.z < _framing.Lens.FarClipPlane &&
                Mathf.Abs(local.x) <= halfWidth && Mathf.Abs(local.y) <= halfHeight;
        }

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
            if (IsAutomatic)
                throw new InvalidOperationException("[StageMapCameraDrag] 자동 연출 중에는 이동 범위를 바꿀 수 없습니다.");
            EndDrag();
            _axis = axis.normalized;
            var plane = new Plane(normal, start);
            var ray = new Ray(_framing.Position, _framing.Rotation * Vector3.forward);
            if (_axis.sqrMagnitude < 0.99f || !plane.Raycast(ray, out float distance))
                throw new InvalidOperationException("[StageMap] 카메라가 맵 평면을 향하지 않습니다.");
            Vector3 center = ray.GetPoint(distance);
            _start = start;
            _normal = normal;
            _minimum = Vector3.Dot(start - center, _axis);
            _maximum = Vector3.Dot(boss - center, _axis);
            if (_maximum < _minimum)
                throw new InvalidOperationException("[StageMap] Boss는 시작 노드의 진행 방향에 있어야 합니다.");
            // 확정한 구도 자체도 허용합니다. 재생 시작 때 작성한 위치를 강제로 옮기지 않습니다.
            _minimum = Mathf.Min(0f, _minimum);
            _maximum = Mathf.Max(0f, _maximum);
            _hasBounds = true;
            SetOffset(_offset);
        }

        public void BeginDrag(float screenX, float screenY)
        {
            EndDrag();
            if (_disposed || IsAutomatic || !_hasBounds || !_lease.HasHandle || _output == null) return;
            if (!TryGetScreenAxis(out _screenAxis)) return;
            _lastPointer = new Vector2(screenX, screenY);
            _dragging = true;
        }

        public void MoveDrag(float screenX, float screenY)
        {
            if (IsAutomatic || !_dragging || !_lease.HasHandle) return;
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

        public void Scroll(float scrollY, float screenX, float screenY)
        {
            if (_disposed || IsAutomatic || !_hasBounds || !_lease.HasHandle || _output == null || _dragging ||
                !float.IsFinite(scrollY) || scrollY == 0f ||
                !_output.pixelRect.Contains(new Vector2(screenX, screenY))) return;

            // 진행 축이 화면 위로 향하는지 아래로 향하는지도 반영해 휠과 카메라 이동 방향을 맞춥니다.
            if (!TryGetScreenAxis(out Vector2 screenAxis)) return;
            if (Mathf.Abs(screenAxis.y) < 0.001f) return;
            float distance = scrollY * _output.pixelHeight * ScrollViewportFraction / screenAxis.y;
            SetOffset(_offset + distance);
        }

        /// <summary>맵 중앙 시선 깊이를 기준으로 진행 축 1m의 화면 이동량을 구합니다. 이동 위치와 경계까지의 거리에 영향받지 않습니다.</summary>
        private bool TryGetScreenAxis(out Vector2 screenAxis)
        {
            screenAxis = default;
            float halfHeight;
            if (_framing.Lens.Orthographic)
            {
                halfHeight = _framing.Lens.OrthographicSize;
            }
            else
            {
                // 원근 카메라에서 고정된 시작 노드를 투영하면 이동할수록 감도가 달라집니다.
                // 기준 구도의 중앙 시선과 맵 평면 사이 깊이는 맵 진행 축으로 이동해도 일정합니다.
                var plane = new Plane(_normal, _start);
                var ray = new Ray(_framing.Position, _framing.Rotation * Vector3.forward);
                if (!plane.Raycast(ray, out float depth)) return false;
                halfHeight = depth * Mathf.Tan(_framing.Lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
            }
            if (!float.IsFinite(halfHeight) || halfHeight <= 0f || _output.pixelHeight <= 0) return false;

            Quaternion rotation = _framing.Rotation * Quaternion.AngleAxis(_framing.Lens.Dutch, Vector3.forward);
            Vector3 cameraAxis = Quaternion.Inverse(rotation) * _axis;
            screenAxis = new Vector2(cameraAxis.x, cameraAxis.y) * (_output.pixelHeight / (2f * halfHeight));
            return screenAxis.sqrMagnitude >= 0.001f;
        }

        public void Dispose()
        {
            _disposed = true;
            _tourGeneration = 0;
            _focusGeneration = 0;
            EndDrag();
            _catalog = null;
            _lease.Dispose();
        }

        private void SetOffset(float value)
        {
            // 감속·보간 없이 입력을 누적하고 범위를 벗어난 부분만 자릅니다. 경계 밖 입력은 쌓아 두지 않습니다.
            float clamped = Mathf.Clamp(value, _minimum, _maximum);
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
    }
}
