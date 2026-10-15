using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge
{
    /// <summary>
    /// Unity Input System의 액션 종류·단계·값을 Framework 입력 값으로 바꿉니다.
    /// Canceled 단계는 버튼 false, 축 0, 벡터 (0, 0)으로 읽습니다.
    /// </summary>
    internal static class InputActionValueReader
    {
        private const string ButtonControlType = "Button";
        private const string AxisControlType = "Axis";
        private const string Vector2ControlType = "Vector2";

        #region Value Type

        internal static bool TryGetValueKind(InputAction action, out InputValueKind valueKind)
        {
            if (action == null)
            {
                valueKind = InputValueKind.None;
                return false;
            }

            if (action.type == InputActionType.Button ||
                string.Equals(action.expectedControlType, ButtonControlType, StringComparison.Ordinal))
            {
                valueKind = InputValueKind.Button;
                return true;
            }

            if (string.Equals(action.expectedControlType, AxisControlType, StringComparison.Ordinal))
            {
                valueKind = InputValueKind.Float;
                return true;
            }

            if (string.Equals(action.expectedControlType, Vector2ControlType, StringComparison.Ordinal))
            {
                valueKind = InputValueKind.Vector2;
                return true;
            }

            valueKind = InputValueKind.None;
            return false;
        }

        internal static InputValueData ReadValue(
            InputAction.CallbackContext context,
            InputPhase phase,
            InputValueKind valueKind)
        {
            switch (valueKind)
            {
                case InputValueKind.Button:
                    return InputValueData.FromButton(
                        phase != InputPhase.Canceled && context.ReadValueAsButton());

                case InputValueKind.Float:
                    return InputValueData.FromFloat(
                        phase == InputPhase.Canceled ? 0f : context.ReadValue<float>());

                case InputValueKind.Vector2:
                {
                    Vector2 value = phase == InputPhase.Canceled
                        ? Vector2.zero
                        : context.ReadValue<Vector2>();
                    return InputValueData.FromVector2(value.x, value.y);
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(valueKind), valueKind, "Unsupported input value kind.");
            }
        }

        #endregion

        #region Phase

        internal static bool TryConvertPhase(InputActionPhase unityPhase, out InputPhase phase)
        {
            switch (unityPhase)
            {
                case InputActionPhase.Started:
                    phase = InputPhase.Started;
                    return true;

                case InputActionPhase.Performed:
                    phase = InputPhase.Performed;
                    return true;

                case InputActionPhase.Canceled:
                    phase = InputPhase.Canceled;
                    return true;

                default:
                    phase = default;
                    return false;
            }
        }

        #endregion
    }
}
