using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput
{
    /// <summary>
    /// 로비 액션을 시설 입력 수명으로 해석하며 씬과 Core를 참조하지 않습니다.
    /// 누름·뗌·강제 취소 해석은 공용 <see cref="PointerSelectInterpreter"/>가 맡습니다(로비 리팩토링 2차 설계 M-10).
    /// </summary>
    internal sealed class LobbyInputController : InputController
    {
        private readonly ILobbyInputReceiver _receiver;
        private readonly PointerSelectInterpreter _select = new();

        public override string ActionMap => LobbyInputNames.Map;

        internal LobbyInputController(ILobbyInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        protected override void Configure(InputBindings input)
        {
            input.Raw(LobbyInputNames.PointerPosition, InputValueKind.Vector2, OnPointer);
            input.Raw(LobbyInputNames.Select, InputValueKind.Button, OnSelect);
        }

        private void OnPointer(InputSnapshot snapshot)
        {
            if (_select.TryPointer(snapshot)) _receiver.OnPointer(_select.X, _select.Y);
        }

        private void OnSelect(InputSnapshot snapshot)
        {
            switch (_select.Select(snapshot))
            {
                case SelectEdge.Pressed:
                    _receiver.OnPointer(_select.X, _select.Y);
                    _receiver.OnSelectPressed();
                    break;
                case SelectEdge.Released:
                    _receiver.OnPointer(_select.X, _select.Y);
                    _receiver.OnSelectReleased();
                    break;
                case SelectEdge.Canceled:
                    _receiver.OnSelectCanceled();
                    break;
            }
        }
    }
}
