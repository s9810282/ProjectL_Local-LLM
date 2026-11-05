using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration
{
    /// <summary>조립 단계의 입력 등록 목록을 방어 복사해 읽기 전용으로 전달합니다.</summary>
    internal sealed class InputRegistrationSet
    {
        internal IReadOnlyList<IInputController> Controllers { get; }
        internal IReadOnlyList<InputLayer> Layers { get; }
        internal IReadOnlyList<KeyBindingDefinition> KeyBindings { get; }

        internal InputRegistrationSet(
            IReadOnlyList<IInputController> controllers,
            IReadOnlyList<InputLayer> layers,
            IReadOnlyList<KeyBindingDefinition> keyBindings = null)
        {
            Controllers = Copy(controllers, nameof(controllers));
            Layers = Copy(layers, nameof(layers));
            KeyBindings = Copy(keyBindings ?? Array.Empty<KeyBindingDefinition>(), nameof(keyBindings));
        }

        /// <summary>중복을 숨기지 않고 전달 순서대로 합성합니다. 실제 입력 설정은 실행하지 않습니다.</summary>
        internal static InputRegistrationSet Combine(params InputRegistrationSet[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            var controllers = new List<IInputController>();
            var layers = new List<InputLayer>();
            var keyBindings = new List<KeyBindingDefinition>();
            foreach (InputRegistrationSet part in parts)
            {
                if (part == null)
                {
                    throw new ArgumentNullException(nameof(parts));
                }

                controllers.AddRange(part.Controllers);
                layers.AddRange(part.Layers);
                keyBindings.AddRange(part.KeyBindings);
            }

            return new InputRegistrationSet(controllers, layers, keyBindings);
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[values.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = values[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
