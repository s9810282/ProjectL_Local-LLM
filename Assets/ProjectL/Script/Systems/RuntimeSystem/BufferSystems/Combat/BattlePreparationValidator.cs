using System;
using System.Collections.Generic;
using System.Linq;
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
            // 참가 목록과 같은 레코드에 함께 설치되는 드로우 순서의 표현 불변식: 드로우 순서는 목록의 서브(메인 제외)를
            // 정확히 한 번씩 담은 순열이다. 전투 수명 동안 바뀌지 않는다. 전투 캐릭터 묶음은 캐릭터 Store가 같은 목록으로 검증한다.
            var membership = preparation.Membership
                ?? throw new InvalidOperationException("전투 준비에는 진입 때 포착한 참가 목록이 필요합니다.");
            if (!preparation.Combat.DrawOrder.OrderBy(i => i.Value)
                    .SequenceEqual(membership.OrderedIds.Where(id => id != membership.MainId).OrderBy(i => i.Value)))
                throw new InvalidOperationException("드로우 순서가 참가 목록의 서브 순열이 아닙니다.");
            var ids = new HashSet<EntityId>();
            var cells = new HashSet<CellCoord>();
            var slots = new HashSet<int>();
            var linked = new HashSet<CharacterInstanceId>();
            long maximumId = 0;
            CombatPreparation combat = preparation.Combat;
            CellEnvironmentQuery.ValidateInitialEnvironment(combat.BoardWidth, combat.BoardHeight, combat.Environment);
            combat.EnvironmentBindings.ValidateInitialReferences(preparation.CommandState.Environment,
                preparation.Structures);
            CellEnvironmentQuery.ValidateInitialPlacement(combat.BoardWidth, combat.BoardHeight, combat.Environment,
                preparation.Structures, preparation.Characters, combat.EnvironmentBindings, combat.WallBlocking, combat.RailCells,
                preparation.CommandState.Gimmicks);
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

            CombatAllocationFacts allocation = preparation.CommandState.Allocation;
            // 할당이 없는 전투는 생성 검사(CombatStateBuffer)가 모든 생성을 거절하므로 저장할 수 있다.
            if (allocation.IsAvailable && allocation.NextEntityId != maximumId + 1)
                throw new InvalidOperationException("전투 개체 할당 범위가 게시할 개체와 맞지 않습니다.");

            // 목표 결합 규칙은 Core 준비가 정한다. 여기서는 게시할 대상의 존재와 생존만 확인한다.
            BattleObjective objective = preparation.CommandState.Objective;
            bool targetValid = objective.Kind == BattleObjectiveKind.EnemyHeadquartersDestroyed
                ? objective.TargetEntityId.IsEmpty
                : preparation.Characters.Any(c => c.EntityId == objective.TargetEntityId &&
                    c.Owner == Owner.Enemy && !c.Health.IsDead);
            if (!targetValid)
                throw new InvalidOperationException("전투 목표의 대상이 게시할 캐릭터와 맞지 않습니다.");

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
