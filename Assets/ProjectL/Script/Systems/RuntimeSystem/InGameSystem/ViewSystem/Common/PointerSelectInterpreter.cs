using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>선택 버튼 입력 하나를 해석한 결과입니다.</summary>
    internal enum SelectEdge
    {
        /// <summary>전달할 것이 없습니다(같은 상태 반복, 누르지 않은 채 강제 종료).</summary>
        None = 0,
        Pressed = 1,
        /// <summary>정상 뗌(ReleaseOnly의 Performed)입니다.</summary>
        Released = 2,
        /// <summary>누른 채 강제 종료(Canceled)되었거나 Performed가 아닌 뗌입니다.</summary>
        Canceled = 3,
    }

    /// <summary>
    /// 포인터 좌표와 선택 버튼의 누름·뗌·강제 취소 해석을 한 곳에 둡니다. 로비·아티팩트 입력 Controller가 함께 씁니다
    /// (로비 리팩토링 2차 설계 M-10). 수신자에게 무엇을 보낼지는 각 Controller가 정합니다.
    /// </summary>
    internal sealed class PointerSelectInterpreter
    {
        private bool _isPressed;

        internal float X { get; private set; }
        internal float Y { get; private set; }

        /// <summary>
        /// 포인터 입력을 받습니다. 맵 비활성화·입력 취소가 보내는 기본 좌표로 마지막 유효 위치를 덮어쓰지 않으려고
        /// Performed만 받아들입니다. 받아들였으면 true입니다.
        /// </summary>
        internal bool TryPointer(InputSnapshot snapshot)
        {
            if (snapshot.Phase != InputPhase.Performed) return false;
            X = snapshot.Value.X;
            Y = snapshot.Value.Y;
            return true;
        }

        /// <summary>
        /// 선택 버튼 입력을 해석합니다. 콜백 재진입에서 같은 누름·뗌이 두 번 나가지 않도록 상태를 먼저 바꾼 뒤 결과를 돌려줍니다.
        /// </summary>
        internal SelectEdge Select(InputSnapshot snapshot)
        {
            // ReleaseOnly의 정상 뗌은 Performed이며 강제 종료 Canceled와 구분합니다.
            if (snapshot.Phase == InputPhase.Canceled)
            {
                if (!_isPressed) return SelectEdge.None;
                _isPressed = false;
                return SelectEdge.Canceled;
            }

            bool isPressed = snapshot.Value.ButtonValue;
            if (isPressed == _isPressed) return SelectEdge.None;
            _isPressed = isPressed;
            if (isPressed) return SelectEdge.Pressed;
            return snapshot.Phase == InputPhase.Performed ? SelectEdge.Released : SelectEdge.Canceled;
        }
    }
}
