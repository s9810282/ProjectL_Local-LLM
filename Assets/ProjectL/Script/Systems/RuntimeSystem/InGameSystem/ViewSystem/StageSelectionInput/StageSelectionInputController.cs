using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>맵 선택 입력을 해석합니다. 씬·DB·Buffer를 참조하지 않습니다.</summary>
    internal sealed class StageSelectionInputController : InputController
    {
        private readonly IStageSelectionInputReceiver _receiver;
        private float _pointerX;
        private float _pointerY;
        private bool _isPressed;

        public override string ActionMap => StageSelectionInputNames.Map;

        internal StageSelectionInputController(IStageSelectionInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        protected override void Configure(InputBindings input)
        {
            input.Raw(StageSelectionInputNames.PointerPosition, InputValueKind.Vector2, OnPointer);
            input.Raw(StageSelectionInputNames.Select, InputValueKind.Button, OnSelect);
            input.Press(StageSelectionInputNames.Confirm, _receiver.ConfirmRequested);
#if UNITY_EDITOR
            input.Raw(StageSelectionInputNames.CameraModifier, InputValueKind.Button, OnCameraModifier);
            input.Raw(StageSelectionInputNames.CameraDolly, InputValueKind.Button, OnCameraDolly);
            input.Raw(StageSelectionInputNames.CameraZoom, InputValueKind.Vector2, OnCameraZoom);
            input.Raw(StageSelectionInputNames.Cancel, InputValueKind.Button, OnCameraRotate);
#else
            input.Press(StageSelectionInputNames.Cancel, _receiver.CancelRequested);
#endif
        }

#if UNITY_EDITOR
        private bool _cameraRotatePressed;
        private bool _cameraDollyPressed;

        private void OnCameraZoom(InputSnapshot snapshot)
        {
            if (snapshot.Phase != InputPhase.Performed || snapshot.Value.Y == 0f) return;
            if (_receiver is IStageCameraFramingInputReceiver framing)
                framing.CameraZoomRequested(snapshot.Value.Y, _pointerX, _pointerY);
        }

        private void OnCameraDolly(InputSnapshot snapshot)
        {
            bool pressed = snapshot.Phase != InputPhase.Canceled && snapshot.Value.ButtonValue;
            if (pressed == _cameraDollyPressed) return;
            _cameraDollyPressed = pressed;
            if (_receiver is IStageCameraFramingInputReceiver framing)
                framing.CameraDollyChanged(pressed, _pointerX, _pointerY);
        }

        private void OnCameraModifier(InputSnapshot snapshot)
        {
            if (_receiver is IStageCameraFramingInputReceiver framing)
                framing.CameraModifierChanged(snapshot.Phase != InputPhase.Canceled && snapshot.Value.ButtonValue);
        }

        private void OnCameraRotate(InputSnapshot snapshot)
        {
            bool pressed = snapshot.Phase != InputPhase.Canceled && snapshot.Value.ButtonValue;
            if (pressed != _cameraRotatePressed)
            {
                _cameraRotatePressed = pressed;
                if (_receiver is IStageCameraFramingInputReceiver framing)
                    framing.CameraRotateChanged(pressed, _pointerX, _pointerY);
            }
            if (snapshot.Phase == InputPhase.Performed) _receiver.CancelRequested();
        }
#endif

        private void OnPointer(InputSnapshot snapshot)
        {
            // 입력 취소의 기본 좌표로 마지막 포인터 위치를 덮어쓰지 않습니다.
            if (snapshot.Phase != InputPhase.Performed)
                return;

            _pointerX = snapshot.Value.X;
            _pointerY = snapshot.Value.Y;
            _receiver.PointerMoved(_pointerX, _pointerY);
        }

        private void OnSelect(InputSnapshot snapshot)
        {
            // Select는 ReleaseOnly입니다. 정상 뗌은 Performed, 강제 종료는 Canceled입니다.
            if (snapshot.Phase == InputPhase.Canceled)
            {
                if (!_isPressed)
                    return;

                _isPressed = false;
                _receiver.SelectCanceled();
                return;
            }

            bool isPressed = snapshot.Value.ButtonValue;
            if (isPressed == _isPressed)
                return;

            // 재진입 콜백에서도 한 번만 전달되도록 상태를 먼저 변경합니다.
            _isPressed = isPressed;
            if (isPressed)
                _receiver.SelectPressed(_pointerX, _pointerY);
            else if (snapshot.Phase == InputPhase.Performed)
                _receiver.SelectReleased(_pointerX, _pointerY);
            else
                _receiver.SelectCanceled();
        }
    }
}
