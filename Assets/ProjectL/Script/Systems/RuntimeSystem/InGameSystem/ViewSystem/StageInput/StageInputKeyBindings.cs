using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>스테이지 입력의 키 설정 대상과 충돌 그룹입니다. 물리 키는 입력 에셋이 소유합니다.</summary>
    internal static class StageInputKeyBindings
    {
        internal static IReadOnlyList<KeyBindingDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            new KeyBindingDefinition(KeyBindingAction.EnterCombat, StageInputNames.Deployment.Map, StageInputNames.Deployment.EnterCombat, 0),
            new KeyBindingDefinition(KeyBindingAction.EndTurn, StageInputNames.Combat.Map, StageInputNames.Combat.EndTurn, 0),
            new KeyBindingDefinition(KeyBindingAction.AttackMode, StageInputNames.Combat.Map, StageInputNames.Combat.AttackMode, 0),
            new KeyBindingDefinition(KeyBindingAction.Return, StageInputNames.Combat.Map, StageInputNames.Combat.Return, 0),
            // 같은 그룹이라 되돌리기·턴 종료 등을 스킬 키로 바꾸면 겹침으로 막습니다(전투 스킬 View 설계 SK6·P16).
            new KeyBindingDefinition(KeyBindingAction.Skill, StageInputNames.Combat.Map, StageInputNames.Combat.Skill, 0)
        });
    }
}
