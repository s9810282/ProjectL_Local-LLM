using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    /// <summary>
    /// 아티팩트 입력을 해석합니다. 씬·DB·Buffer를 참조하지 않습니다.
    /// 누름·뗌·강제 취소 해석은 공용 <see cref="PointerSelectInterpreter"/>가 맡습니다(로비 리팩토링 2차 설계 M-10).
    /// </summary>
    internal sealed class ArtifactInputController : InputController
    {
        private readonly IArtifactInputReceiver _receiver;
        private readonly PointerSelectInterpreter _select = new();

        public override string ActionMap => ArtifactInputNames.Map;

        internal ArtifactInputController(IArtifactInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        protected override void Configure(InputBindings input)
        {
            input.Raw(ArtifactInputNames.PointerPosition, InputValueKind.Vector2, OnPointer);
            input.Raw(ArtifactInputNames.Select, InputValueKind.Button, OnSelect);
            input.Press(ArtifactInputNames.RotateLeft, _receiver.RotateLeft);
            input.Press(ArtifactInputNames.RotateRight, _receiver.RotateRight);
            input.Press(ArtifactInputNames.Cancel, _receiver.Cancel);
        }

        private void OnPointer(InputSnapshot snapshot)
        {
            if (_select.TryPointer(snapshot)) _receiver.PointerMoved(_select.X, _select.Y);
        }

        private void OnSelect(InputSnapshot snapshot)
        {
            switch (_select.Select(snapshot))
            {
                case SelectEdge.Pressed:
                    _receiver.SelectPressed(_select.X, _select.Y);
                    break;
                case SelectEdge.Released:
                    _receiver.SelectReleased(_select.X, _select.Y);
                    break;
                case SelectEdge.Canceled:
                    _receiver.SelectCanceled();
                    break;
            }
        }
    }
}
