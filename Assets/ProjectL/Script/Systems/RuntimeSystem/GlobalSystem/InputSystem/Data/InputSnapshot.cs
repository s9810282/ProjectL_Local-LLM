using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data
{
    public readonly struct InputValueData
    {
        private readonly bool _buttonValue;
        private readonly float _floatValue;
        private readonly float _x;
        private readonly float _y;

        public InputValueKind Kind { get; }

        public bool IsValid =>
            Kind == InputValueKind.Button ||
            Kind == InputValueKind.Float ||
            Kind == InputValueKind.Vector2;

        public bool ButtonValue
        {
            get
            {
                EnsureKind(InputValueKind.Button);
                return _buttonValue;
            }
        }

        public float FloatValue
        {
            get
            {
                EnsureKind(InputValueKind.Float);
                return _floatValue;
            }
        }

        public float X
        {
            get
            {
                EnsureKind(InputValueKind.Vector2);
                return _x;
            }
        }

        public float Y
        {
            get
            {
                EnsureKind(InputValueKind.Vector2);
                return _y;
            }
        }

        private InputValueData(
            InputValueKind kind,
            bool buttonValue,
            float floatValue,
            float x,
            float y)
        {
            Kind = kind;
            _buttonValue = buttonValue;
            _floatValue = floatValue;
            _x = x;
            _y = y;
        }

        public static InputValueData FromButton(bool value)
        {
            return new InputValueData(
                InputValueKind.Button,
                value,
                0f,
                0f,
                0f);
        }

        public static InputValueData FromFloat(float value)
        {
            return new InputValueData(
                InputValueKind.Float,
                false,
                value,
                0f,
                0f);
        }

        public static InputValueData FromVector2(
            float x,
            float y)
        {
            return new InputValueData(
                InputValueKind.Vector2,
                false,
                0f,
                x,
                y);
        }

        private void EnsureKind(
            InputValueKind expectedKind)
        {
            if (Kind != expectedKind)
            {
                throw new InvalidOperationException(
                    $"Input value kind is {Kind}, " +
                    $"not {expectedKind}.");
            }
        }
    }

    public readonly struct InputSnapshot
    {
        public InputLayer Layer { get; }

        public string ActionMap { get; }

        public string ActionName { get; }

        public InputPhase Phase { get; }

        public InputValueData Value { get; }

        public double Time { get; }

        internal InputSnapshot(
            InputLayer layer,
            string actionMap,
            string actionName,
            InputPhase phase,
            InputValueData value,
            double time)
        {
            Layer = layer;
            ActionMap = actionMap;
            ActionName = actionName;
            Phase = phase;
            Value = value;
            Time = time;
        }
    }
}
