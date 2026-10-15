using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AttackTargetResolver
    {
        /// <summary>호출자가 대상 자격과 맵 정책을 적용한 불변 사실만 받는다. 일반 공격의 개체별 역할을 반환한다.</summary>
        public KeyValuePair<EntityId, AttackTargetRole>[] CollectCandidates(
            AttackCellSelection selection, ReadOnlySpan<CharacterCombatFacts> eligibleCharacters,
            ReadOnlySpan<StructureCombatFacts> eligibleStructures)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));
            var designated = new HashSet<CellCoord>(selection.DesignatedCells.ToArray());
            var additional = new HashSet<CellCoord>(selection.AdditionalCells.ToArray());
            var roles = new Dictionary<EntityId, AttackTargetRole>();
            for (int i = 0; i < eligibleCharacters.Length; i++)
            {
                var facts = eligibleCharacters[i]
                    ?? throw new ArgumentException("대상 사실은 null일 수 없습니다.", nameof(eligibleCharacters));
                AddRole(roles, facts.EntityId, ResolveRole(selection.PrimaryCell, designated, additional, facts.OccupiedCells));
            }
            for (int i = 0; i < eligibleStructures.Length; i++)
            {
                var facts = eligibleStructures[i]
                    ?? throw new ArgumentException("대상 사실은 null일 수 없습니다.", nameof(eligibleStructures));
                AddRole(roles, facts.EntityId, ResolveRole(selection.PrimaryCell, designated, additional, facts.OccupiedCells));
            }
            var result = new KeyValuePair<EntityId, AttackTargetRole>[roles.Count];
            int index = 0;
            foreach (var entry in roles)
                result[index++] = entry;
            Array.Sort(result, (left, right) => left.Key.CompareTo(right.Key));
            return result;
        }

        public AttackTargetRole ResolveRole(AttackCellSelection selection, IReadOnlyList<CellCoord> occupiedCells)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));
            if (occupiedCells == null)
                throw new ArgumentNullException(nameof(occupiedCells));
            return ResolveRole(selection.PrimaryCell,
                new HashSet<CellCoord>(selection.DesignatedCells.ToArray()),
                new HashSet<CellCoord>(selection.AdditionalCells.ToArray()), occupiedCells);
        }

        private static AttackTargetRole ResolveRole(CellCoord? primary, HashSet<CellCoord> designated,
            HashSet<CellCoord> additional, IReadOnlyList<CellCoord> occupiedCells)
        {
            AttackTargetRole role = AttackTargetRole.None;
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                CellCoord cell = occupiedCells[i];
                if (primary.HasValue && cell == primary.Value)
                    return AttackTargetRole.Primary;
                if (designated.Contains(cell))
                    role = AttackTargetRole.Designated;
                else if (additional.Contains(cell) && role == AttackTargetRole.None)
                    role = AttackTargetRole.Additional;
            }
            return role;
        }

        private static void AddRole(Dictionary<EntityId, AttackTargetRole> roles, EntityId id, AttackTargetRole role)
        {
            if (role == AttackTargetRole.None)
                return;
            if (roles.ContainsKey(id) == false || role == AttackTargetRole.Primary)
                roles[id] = role;
        }
    }
}
