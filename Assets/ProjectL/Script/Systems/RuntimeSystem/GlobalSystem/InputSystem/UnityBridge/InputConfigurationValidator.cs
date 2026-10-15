using System;
using System.Collections.Generic;
using System.Text;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using UnityEngine.InputSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge
{
    /// <summary>
    /// 입력 연결을 시작하기 전에 액션 에셋과 Layer 목록·Controller 바인딩이 맞는지 한 번에 검사합니다.
    /// 발견한 문제를 모두 모아 하나의 예외로 알립니다. Layer가 없는 상태로 시작하므로 초기 Layer는 검사하지 않습니다.
    /// </summary>
    internal static class InputConfigurationValidator
    {
        internal static void ValidateOrThrow(
            InputActionAsset actions,
            IReadOnlyList<InputLayer> layers,
            InputConfiguration configuration)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var errors = new List<string>();
            var reachableMaps = new HashSet<string>(StringComparer.Ordinal);

            ValidateLayers(actions, layers, reachableMaps, errors);
            ValidateBindings(actions, configuration.Bindings, reachableMaps, errors);

            if (errors.Count > 0)
                throw new InvalidOperationException(BuildErrorMessage(errors));
        }

        private static void ValidateLayers(
            InputActionAsset actions,
            IReadOnlyList<InputLayer> layers,
            HashSet<string> reachableMaps,
            List<string> errors)
        {
            if (layers.Count == 0)
            {
                errors.Add("At least one Input Layer is required.");
                return;
            }

            var registeredNames = new HashSet<string>(StringComparer.Ordinal);

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                InputLayer layer = layers[layerIndex];

                if (layer.IsValid == false)
                {
                    errors.Add($"Input Layer is invalid. Index={layerIndex}");
                    continue;
                }

                if (registeredNames.Add(layer.Name) == false)
                {
                    errors.Add($"Input Layer name is duplicated: {layer.Name}");
                    continue;
                }

                IReadOnlyList<string> mapNames = layer.ActionMaps;
                for (int mapIndex = 0; mapIndex < mapNames.Count; mapIndex++)
                {
                    string mapName = mapNames[mapIndex];
                    reachableMaps.Add(mapName);

                    if (actions.FindActionMap(mapName, false) == null)
                        errors.Add($"Action Map was not found: {layer.Name}.{mapName}");
                }
            }
        }

        private static void ValidateBindings(
            InputActionAsset actions,
            IReadOnlyList<InputBindingDescriptor> bindings,
            HashSet<string> reachableMaps,
            List<string> errors)
        {
            for (int bindingIndex = 0; bindingIndex < bindings.Count; bindingIndex++)
            {
                InputBindingDescriptor binding = bindings[bindingIndex];

                if (binding.IsValid == false)
                {
                    errors.Add($"Input binding descriptor is invalid. Index={bindingIndex}");
                    continue;
                }

                if (reachableMaps.Contains(binding.ActionMap) == false)
                    errors.Add($"Controller Action Map cannot be activated by any registered Layer: {binding}");

                InputActionMap actionMap = actions.FindActionMap(binding.ActionMap, false);
                if (actionMap == null)
                {
                    errors.Add($"Controller Action Map was not found: {binding}");
                    continue;
                }

                InputAction action = actionMap.FindAction(binding.ActionName, false);
                if (action == null)
                {
                    errors.Add($"Input Action was not found: {binding}");
                    continue;
                }

                if (InputActionValueReader.TryGetValueKind(action, out InputValueKind actualKind) == false)
                {
                    errors.Add(
                        $"Input Action value type is unsupported: {binding.ActionMap}.{binding.ActionName}. " +
                        $"ActionType={action.type}, ExpectedControlType={action.expectedControlType}");
                }
                else if (actualKind != binding.ValueKind)
                {
                    errors.Add(
                        $"Input Action value type does not match the Controller binding: " +
                        $"{binding.ActionMap}.{binding.ActionName}. Expected={binding.ValueKind}, " +
                        $"Actual={actualKind}, Controller={binding.ControllerName}");
                }

                if (HasUsableBinding(action) == false)
                    errors.Add($"Input Action has no usable physical binding: {binding}");
            }
        }

        private static bool HasUsableBinding(InputAction action)
        {
            var bindings = action.bindings;

            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];

                if (binding.isComposite)
                    continue;

                if (string.IsNullOrWhiteSpace(binding.effectivePath) == false)
                    return true;
            }

            return false;
        }

        private static string BuildErrorMessage(List<string> errors)
        {
            var builder = new StringBuilder("Input configuration is invalid:");

            for (int i = 0; i < errors.Count; i++)
                builder.AppendLine().Append(" - ").Append(errors[i]);

            return builder.ToString();
        }
    }
}
