using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    /// <summary>아티팩트 회전의 키 설정 대상과 충돌 그룹입니다.</summary>
    internal static class ArtifactInputKeyBindings
    {
        internal static IReadOnlyList<KeyBindingDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            new KeyBindingDefinition(KeyBindingAction.RotateLeft, ArtifactInputNames.Map, ArtifactInputNames.RotateLeft, 1),
            new KeyBindingDefinition(KeyBindingAction.RotateRight, ArtifactInputNames.Map, ArtifactInputNames.RotateRight, 1)
        });
    }
}
