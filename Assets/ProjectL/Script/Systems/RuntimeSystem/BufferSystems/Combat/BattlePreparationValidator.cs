using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Combat
{
    /// <summary>게시할 저장값의 ID 범위, 점유와 드로우 연결 일관성을 확인한다.</summary>
    internal static class BattlePreparationValidator
    {
        internal static void Validate(BattlePreparation preparation)
        {
            var ids = new HashSet<EntityId>();
            var cells = new HashSet<CellCoord>();
            var slots = new HashSet<int>();
            var linked = new HashSet<CharacterInstanceId>();
            long maximumId = 0;
            CombatPreparation combat = preparation.Combat;
            combat.EnvironmentBindings.ValidateInitialReferences(preparation.CommandState.Environment,
                preparation.Structures);
            CellEnvironmentQuery.ValidateInitialPlacement(combat.BoardWidth, combat.BoardHeight, combat.Environment,
                preparation.Structures, preparation.Characters, combat.EnvironmentBindings, combat.WallBlocking, combat.RailCells);
            foreach (CharacterCombatFacts character in preparation.Characters)
            {
                AddId(character.EntityId);
                AddCells(character.OccupiedCells);
                if (character.ReserveSlotIndex.HasValue &&
                    (character.ReserveSlotIndex.Value >= combat.ReserveCapacity ||
                     slots.Add(character.ReserveSlotIndex.Value) == false))
                    throw new InvalidOperationException("손패 슬롯이 범위 밖이거나 중복되었습니다.");
                if (character.OwnedId.HasValue)
                    linked.Add(character.OwnedId.Value);
            }

            foreach (StructureCombatFacts structure in preparation.Structures)
            {
                AddId(structure.EntityId);
                AddCells(structure.OccupiedCells);
                // 시작 체력을 정의값으로 둘지는 Core 전투 준비가 정한다. 여기서는 체력 범위만 본다.
                if (structure.Health.MaxHealth < 1 ||
                    structure.Health.CurrentHealth <= 0 || structure.Health.CurrentHealth > structure.Health.MaxHealth)
                    throw new InvalidOperationException("구조물 체력이 유효한 범위가 아닙니다.");
            }

            if (preparation.NextEntityId != maximumId + 1 || preparation.NextEntityId < 1 ||
                preparation.NextEntityId > (long)int.MaxValue + 1)
                throw new InvalidOperationException("전투 개체 할당 범위가 게시할 개체와 맞지 않습니다.");

            for (int i = 0; i < combat.DrawOrder.Count; i++)
                if (linked.Contains(combat.DrawOrder[i]) != (i < combat.DrawCursor))
                    throw new InvalidOperationException("드로우 커서와 지급된 보유 연결이 맞지 않습니다.");

            var rails = new HashSet<CellCoord>();
            foreach (CellCoord cell in combat.RailCells)
                if (cell.X < 0 || cell.Y < 0 || cell.X >= combat.BoardWidth || cell.Y >= combat.BoardHeight ||
                    rails.Add(cell) == false)
                    throw new InvalidOperationException("철도 셀이 보드 범위 밖이거나 중복되었습니다.");

            foreach (var gimmick in combat.StageGimmicks)
                if (gimmick == null)
                    throw new InvalidOperationException("기믹 정의에는 null을 넣을 수 없습니다.");

            void AddId(EntityId id)
            {
                if (id.IsEmpty || ids.Add(id) == false)
                    throw new InvalidOperationException("전투 개체 ID는 비어 있거나 중복될 수 없습니다.");
                maximumId = Math.Max(maximumId, id.Value);
            }

            void AddCells(IReadOnlyList<CellCoord> occupiedCells)
            {
                foreach (CellCoord cell in occupiedCells)
                    if (cell.X < 0 || cell.Y < 0 || cell.X >= combat.BoardWidth || cell.Y >= combat.BoardHeight ||
                        cells.Add(cell) == false)
                        throw new InvalidOperationException("점유 셀이 보드 범위 밖이거나 중복되었습니다.");
            }
        }
    }
}
