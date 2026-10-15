using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration
{
    public readonly struct InputBindingDescriptor
    {
        #region Public API

        public string ControllerName { get; }

        public string ActionMap { get; }

        public string ActionName { get; }

        public InputValueKind ValueKind { get; }

        public bool IsValid =>
            string.IsNullOrWhiteSpace(
                ControllerName) == false &&
            string.IsNullOrWhiteSpace(
                ActionMap) == false &&
            string.IsNullOrWhiteSpace(
                ActionName) == false &&
            (
                ValueKind == InputValueKind.Button ||
                ValueKind == InputValueKind.Float ||
                ValueKind == InputValueKind.Vector2
            );

        public override string ToString()
        {
            return
                $"{ActionMap}.{ActionName} " +
                $"({ValueKind}, {ControllerName})";
        }

        #endregion

        #region Internal

        internal InputBindingDescriptor(
            string controllerName,
            string actionMap,
            string actionName,
            InputValueKind valueKind)
        {
            if (string.IsNullOrWhiteSpace(
                    controllerName))
            {
                throw new ArgumentException(
                    "Input Controller name is required.",
                    nameof(controllerName));
            }

            if (string.IsNullOrWhiteSpace(
                    actionMap))
            {
                throw new ArgumentException(
                    "Action Map name is required.",
                    nameof(actionMap));
            }

            if (string.IsNullOrWhiteSpace(
                    actionName))
            {
                throw new ArgumentException(
                    "Input action name is required.",
                    nameof(actionName));
            }

            if (valueKind != InputValueKind.Button &&
                valueKind != InputValueKind.Float &&
                valueKind != InputValueKind.Vector2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(valueKind),
                    valueKind,
                    "Unsupported input value kind.");
            }

            ControllerName = controllerName;
            ActionMap = actionMap;
            ActionName = actionName;
            ValueKind = valueKind;
        }

        #endregion
    }
}