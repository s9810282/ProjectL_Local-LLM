using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 포인터 입력 하나의 값입니다(View 2라운드 W5-1). 입력 기능 묶음이 한 번 만들어 소유자에게 <c>in</c>으로 넘깁니다(입력마다 할당하지 않음).
    /// 사실은 그 입력을 처리하려고 읽은 사본이며, 저장하지 않습니다.
    /// </summary>
    internal readonly struct CombatPointerInput
    {
        internal CombatPointerInput(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            Facts = facts;
            Characters = characters;
            ScreenX = screenX;
            ScreenY = screenY;
        }

        internal CombatInputFacts Facts { get; }

        internal IReadOnlyList<CharacterData> Characters { get; }

        internal float ScreenX { get; }

        internal float ScreenY { get; }
    }
}
