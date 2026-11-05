#if UNITY_EDITOR
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Editor
{
    /// <summary>조정 중인 Alt 입력만 소비하고 나머지는 기존 맵 입력에 전달합니다.</summary>
    internal sealed class StageMapCameraFramingReceiver : IStageSelectionInputReceiver, IStageCameraFramingInputReceiver
    {
        private const float DegreesPerPixel = 0.15f;
        private readonly IStageSelectionInputReceiver _inner;
        private readonly StageMapCameraFramingSettings _settings;
        private readonly Camera _output;
        private readonly StageMapPointerBlocker _blocker = new();
        private bool _modifier;
        private bool _suppressSelect;
        private DragMode _mode;
        private Vector2 _pointer;

        internal StageMapCameraFramingReceiver(IStageSelectionInputReceiver inner,
            StageMapCameraFramingSettings settings, Camera output)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _output = output != null ? output : throw new ArgumentNullException(nameof(output));
        }

        private bool ConsumesInput => _modifier && _settings.IsOpen;
        private bool CanUse => ConsumesInput && _output != null &&
            EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().Name == "GameView";

        internal void Tick()
        {
            if (_mode != DragMode.None && !CanUse)
            {
                _mode = DragMode.None;
                _modifier = false;
                _inner.SelectCanceled();
            }
        }

        public void CameraModifierChanged(bool pressed)
        {
            if (_modifier == pressed) return;
            _modifier = pressed;
            if (ConsumesInput)
            {
                _inner.SelectCanceled();
                _suppressSelect = true;
            }
            if (!pressed) _mode = DragMode.None;
        }

        public void CameraRotateChanged(bool pressed, float screenX, float screenY)
        {
            if (!pressed)
            {
                if (_mode == DragMode.Rotate) _mode = DragMode.None;
                return;
            }
            if (!ConsumesInput) return;
            _inner.SelectCanceled();
            _suppressSelect = true;
            Begin(DragMode.Rotate, screenX, screenY);
        }

        public void CameraDollyChanged(bool pressed, float screenX, float screenY)
        {
            if (!pressed)
            {
                if (_mode == DragMode.Dolly) _mode = DragMode.None;
                return;
            }
            if (!ConsumesInput)
            {
                _settings.ReportDolly($"입력 수신, 시작 차단: Alt={_modifier}, 잠금해제={_settings.IsOpen}");
                return;
            }
            _inner.SelectCanceled();
            _suppressSelect = true;
            Begin(DragMode.Dolly, screenX, screenY);
        }

        public void CameraZoomRequested(float scrollY, float screenX, float screenY)
        {
            if (!CanUse || !float.IsFinite(scrollY) || scrollY == 0f || _mode != DragMode.None ||
                !_output.pixelRect.Contains(new Vector2(screenX, screenY)) ||
                _blocker.IsBlocked(screenX, screenY)) return;
            // 장치별 휠 단위 차이와 무관하게 이벤트당 일정 비율로 확대·축소합니다.
            float factor = scrollY > 0f ? 0.9f : 1f / 0.9f;
            _settings.TrySetSize(Mathf.Max(StageMapCameraFramingSettings.MinimumSize, _settings.Size * factor));
        }

        public void PointerMoved(float screenX, float screenY)
        {
            Tick();
            if (_mode != DragMode.None)
            {
                Vector2 next = new(screenX, screenY);
                Vector2 delta = next - _pointer;
                _pointer = next;
                Vector3 position = _settings.Position;
                Vector3 angles = _settings.Angles;
                if (_mode == DragMode.Pan || _mode == DragMode.Dolly)
                {
                    float unitsPerPixel = 2f * _output.orthographicSize / Mathf.Max(1, _output.pixelHeight);
                    Quaternion rotation = Quaternion.Euler(angles);
                    if (_mode == DragMode.Pan)
                        position -= rotation * new Vector3(delta.x, delta.y, 0f) * unitsPerPixel;
                    else
                        // 위로 끌면 시선 방향으로 전진합니다. 렌즈와 회전은 변경하지 않습니다.
                        position += rotation * Vector3.forward * (delta.y * unitsPerPixel);
                }
                else
                {
                    angles.x -= delta.y * DegreesPerPixel;
                    angles.y += delta.x * DegreesPerPixel;
                }
                // 맵 평면을 벗어나는 구도는 마지막 유효한 작업값을 유지합니다.
                bool accepted = _settings.TrySetPose(position, angles);
                if (_mode == DragMode.Dolly)
                    _settings.ReportDolly(accepted
                        ? $"위치 반영: {position.ToString("F3")}, 세로 이동={delta.y:F1}px"
                        : $"위치 거절: 카메라가 맵 평면을 바라보지 않음. 후보={position.ToString("F3")}");
                return;
            }
            if (!ConsumesInput) _inner.PointerMoved(screenX, screenY);
        }

        public void SelectPressed(float screenX, float screenY)
        {
            _suppressSelect = ConsumesInput;
            if (_suppressSelect)
            {
                _inner.SelectCanceled();
                Begin(DragMode.Pan, screenX, screenY);
                return;
            }
            _inner.SelectPressed(screenX, screenY);
        }

        public void SelectReleased(float screenX, float screenY)
        {
            if (_suppressSelect)
            {
                _suppressSelect = false;
                if (_mode == DragMode.Pan) _mode = DragMode.None;
                return;
            }
            _inner.SelectReleased(screenX, screenY);
        }

        public void SelectCanceled()
        {
            _mode = DragMode.None;
            _suppressSelect = false;
            _inner.SelectCanceled();
        }

        public void ConfirmRequested()
        {
            if (!ConsumesInput) _inner.ConfirmRequested();
        }

        public void CancelRequested()
        {
            if (!ConsumesInput) _inner.CancelRequested();
        }

        private void Begin(DragMode mode, float screenX, float screenY)
        {
            string rejection = null;
            if (!CanUse)
                rejection = "조정 모드 또는 Game 뷰 포커스 없음";
            else if (_mode != DragMode.None)
                rejection = $"다른 드래그 진행 중: {_mode}";
            else if (!_output.pixelRect.Contains(new Vector2(screenX, screenY)))
                rejection = $"Game 화면 밖 좌표: ({screenX:F1}, {screenY:F1})";
            else if (_blocker.IsBlocked(screenX, screenY))
                rejection = "포인터 아래 UI가 입력을 차단함";
            if (rejection != null)
            {
                if (mode == DragMode.Dolly) _settings.ReportDolly("입력 수신, 시작 차단: " + rejection);
                return;
            }
            _pointer = new Vector2(screenX, screenY);
            _mode = mode;
            if (mode == DragMode.Dolly) _settings.ReportDolly("드래그 시작, 포인터 이동 대기");
        }

        private enum DragMode
        {
            None,
            Pan,
            Rotate,
            Dolly,
        }
    }
}
#endif
