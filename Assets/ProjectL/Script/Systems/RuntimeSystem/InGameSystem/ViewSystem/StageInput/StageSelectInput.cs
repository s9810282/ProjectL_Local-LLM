using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 배치·전투 Controller가 함께 쓰는 포인터와 선택 버튼 해석입니다.
    /// 선택 버튼은 에셋에서 Press(behavior=1)(ReleaseOnly)이며, 누름은 Started, 정상 뗌은 Performed로 옵니다.
    /// Canceled는 맵 해제·Layer 전환·종료로 선택이 강제로 끝난 것이므로 뗌이 아니라 취소로 전달합니다.
    /// </summary>
    internal sealed class StageSelectInput
    {
        private readonly IStageInputReceiver _receiver;

        private float _pointerX;
        private float _pointerY;

        /// <summary>직전에 선택 버튼이 눌려 있었는지. 같은 값이 반복되면 무시합니다.</summary>
        private bool _isPressed;

        internal StageSelectInput(IStageInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        /// <summary>포인터와 선택 버튼 바인딩을 등록합니다. Controller의 Configure 안에서만 부릅니다.</summary>
        internal void Configure(InputBindings input, string pointerAction, string selectAction)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Vector2 편의 바인딩은 Canceled의 (0, 0)도 전달하므로 Raw로 받아 Performed만 반영합니다.
            input.Raw(pointerAction, InputValueKind.Vector2, OnPointer);
            input.Raw(selectAction, InputValueKind.Button, OnSelect);
        }

        private void OnPointer(InputSnapshot snapshot)
        {
            if (snapshot.Phase != InputPhase.Performed)
                return;

            _pointerX = snapshot.Value.X;
            _pointerY = snapshot.Value.Y;
            _receiver.PointerMoved(_pointerX, _pointerY);
        }

        private void OnSelect(InputSnapshot snapshot)
        {
            if (snapshot.Phase == InputPhase.Canceled)
            {
                // 정상 뗌 뒤에 이어지는 Canceled는 누름 상태가 이미 풀려 있어 무시됩니다.
                if (!_isPressed)
                    return;

                _isPressed = false;
                _receiver.SelectCanceled();
                return;
            }

            bool isPressed = snapshot.Value.ButtonValue;
            if (isPressed == _isPressed)
                return;

            // 콜백 안에서 다시 입력이 들어와도 같은 전환을 두 번 전달하지 않도록 상태를 먼저 바꿉니다.
            _isPressed = isPressed;

            if (isPressed)
            {
                _receiver.SelectPressed(_pointerX, _pointerY);
                return;
            }

            if (snapshot.Phase == InputPhase.Performed)
                _receiver.SelectReleased(_pointerX, _pointerY);
            else
                _receiver.SelectCanceled();
        }
    }
}
