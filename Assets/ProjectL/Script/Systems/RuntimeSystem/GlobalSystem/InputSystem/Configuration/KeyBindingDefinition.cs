using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration
{
    /// <summary>각 화면이 정의한 키 설정 대상 액션과 중복 검사 그룹을 전달합니다.</summary>
    internal readonly struct KeyBindingDefinition
    {
        internal KeyBindingAction Action { get; }
        internal string Map { get; }
        internal string Name { get; }
        internal int ConflictGroup { get; }

        internal KeyBindingDefinition(KeyBindingAction action, string map, string name, int conflictGroup)
        {
            Action = action;
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ConflictGroup = conflictGroup;
        }
    }
}
