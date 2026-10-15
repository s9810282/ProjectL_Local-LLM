using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 입력 해석에 쓰는 캐릭터 찾기입니다(구조 개선 VR3). 받은 목록만 직접 순회하며 저장하거나 할당하지 않습니다.
    /// 규칙 판정이 아니라 "누른 칸·손패에 누가 있는가"만 답합니다.
    /// </summary>
    internal static class CombatCharacterQueries
    {
        internal static CharacterData Find(IReadOnlyList<CharacterData> characters, EntityId id)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].EntityId == id)
                    return characters[i];
            }

            return null;
        }

        internal static CharacterData FindPlayerInArea(IReadOnlyList<CharacterData> characters, EntityId id, CharacterRuntimeArea area)
        {
            CharacterData character = Find(characters, id);
            return character != null && character.Owner == Owner.Player && character.Area == area && !character.Health.IsDead
                ? character
                : null;
        }

        internal static CharacterData FindPlayerAt(IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            CharacterData character = FindAnyAt(characters, cell);
            return character != null && character.Owner == Owner.Player ? character : null;
        }

        /// <summary>그 칸을 점유한 살아 있는 필드 캐릭터입니다.</summary>
        internal static CharacterData FindAnyAt(IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.Area != CharacterRuntimeArea.Field || character.Health.IsDead)
                    continue;

                IReadOnlyList<CellCoord> cells = character.OccupiedCells;
                if (cells != null && cells.Count > 0)
                {
                    for (int c = 0; c < cells.Count; c++)
                    {
                        if (cells[c] == cell)
                            return character;
                    }
                }
                else if (character.Anchor == cell)
                {
                    return character;
                }
            }

            return null;
        }
    }
}
