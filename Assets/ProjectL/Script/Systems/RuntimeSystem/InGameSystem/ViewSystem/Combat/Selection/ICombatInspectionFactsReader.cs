using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>선택과 정보창이 같은 표시 사실을 읽는 입구입니다.</summary>
    internal interface ICombatInspectionFactsReader
    {
        bool TryGet(EntityId id, out CombatInspectionFacts facts);
        bool TryPick(CellCoord cell, out EntityId id);
    }
    /// <summary>불변 표시 사실의 읽기 뷰입니다. 원본 목록과 변경 권한을 노출하지 않습니다.</summary>
    internal readonly struct CombatInspectionFacts
    {

        internal CombatInspectionFacts(CharacterCombatFacts character)
        {
            Character = character;
            StructureAttackPower = null;
            Id = character.EntityId;
            Key = character.CharacterKey;
            Owner = character.Owner;
            Health = character.Health.CurrentHealth;
            MaxHealth = character.Health.MaxHealth;
            Cells = character.OccupiedCells;
        }
        internal CombatInspectionFacts(StructureCombatFacts structure)
        {
            Character = null;
            StructureAttackPower = structure.Definition.AttackPower;
            Id = structure.EntityId;
            Key = structure.Definition.Key;
            Owner = structure.Owner;
            Health = structure.Health.CurrentHealth;
            MaxHealth = structure.Health.MaxHealth;
            Cells = structure.OccupiedCells;
        }
        internal EntityId Id { get; }
        internal EntityKey Key { get; }
        internal Owner Owner { get; }
        internal int Health { get; }
        internal int MaxHealth { get; }
        internal IReadOnlyList<CellCoord> Cells { get; }
        internal CharacterCombatFacts Character { get; }
        internal int? StructureAttackPower { get; }

        /// <summary>정보창과 HUD가 공유하는 구조물 표시 자격입니다.</summary>
        internal static bool CanInspectStructure(StructureCombatFacts structure) =>
            structure != null && !structure.Health.IsDead && structure.Owner == Owner.Enemy &&
            !ObstacleContentProfile.IsNativeObstacleKey(structure.Definition.Key);
    }
}
