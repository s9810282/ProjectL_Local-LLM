using System;

namespace ProjectL.Tool.Editor
{
    internal static class CombatCharacterCatalog
    {
        public static bool TryOwner(CombatReferenceSheet characters, int key, out string owner)
        {
            owner = null;
            var row = characters.Find("Character_Key", key);
            if (row == null) return false;
            // CharacterMaster specification: 1 = allied, 2 = enemy. Never infer a default.
            switch (characters.Number(row, "Character_PositionType"))
            {
                case 1: owner = "Player"; return true;
                case 2: owner = "Enemy"; return true;
                default: return false;
            }
        }

        public static string OwnerLabel(CombatReferenceSheet characters, int key) =>
            TryOwner(characters, key, out var owner) ? owner == "Player" ? "아군" : "적군" : "DT 진형 미지정";

        public static int RefreshUnitOwners(CombatLayoutDocument document)
        {
            int changed = 0;
            foreach (var unit in document.units)
                if (TryOwner(document.characters, unit.characterKey, out var owner) && unit.owner != owner)
                { unit.owner = owner; changed++; }
            return changed;
        }

        public static void ValidateUnits(CombatLayoutDocument document)
        {
            foreach (var unit in document.units)
            {
                if (!TryOwner(document.characters, unit.characterKey, out var owner))
                    throw new InvalidOperationException($"Unit {unit.key}: Character {unit.characterKey}의 현재 DT와 진형(1=아군, 2=적군)을 확인해 주세요.");
                if (unit.owner != owner)
                    throw new InvalidOperationException($"Unit {unit.key}: 배치 소속이 DT 진형과 다릅니다. 유닛 엑셀을 다시 불러와 주세요.");
            }
        }
    }
}
