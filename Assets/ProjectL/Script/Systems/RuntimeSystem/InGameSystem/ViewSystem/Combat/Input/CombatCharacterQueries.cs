using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 입력 해석에 쓰는 캐릭터 찾기입니다(구조 개선 VR3). 받은 목록만 직접 순회하며 저장하거나 할당하지 않습니다.
    /// 포인터 입력과 유닛 순환이 선택할 대상을 찾습니다.
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

        /// <summary>행동이 남은 다음 병종을 찾습니다. 다른 병종에 후보가 없으면 같은 병종의 다른 유닛을 고릅니다.</summary>
        internal static EntityId FindNextPlayerByCategory(IReadOnlyList<CharacterData> characters, EntityId selectedActor)
        {
            CharacterData selected = FindPlayerInArea(characters, selectedActor, CharacterRuntimeArea.Field);
            int currentCategory = (int)(selected?.Stats.Category.Key ?? CategoryKey.None);
            var best = (Group: int.MaxValue, Category: 0, Actor: EntityId.Empty);

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData candidate = characters[i];
                if (candidate.EntityId == selectedActor || candidate.Owner != Owner.Player ||
                    candidate.Area != CharacterRuntimeArea.Field || candidate.Health.IsDead ||
                    !UnitActionUseRules.CanStartAttack(candidate.ActionUse))
                    continue;

                int category = (int)candidate.Stats.Category.Key;
                // 현재 병종 뒤부터 찾고 끝에 닿으면 처음부터 돕니다. 같은 병종만 남으면 유닛 ID 순으로 순환합니다.
                int group = category > currentCategory ? 0 : category < currentCategory ? 1
                    : candidate.EntityId.CompareTo(selectedActor) > 0 ? 2 : 3;
                var rank = (group, category, candidate.EntityId);
                if (rank.CompareTo(best) < 0)
                    best = rank;
            }

            return best.Actor;
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
