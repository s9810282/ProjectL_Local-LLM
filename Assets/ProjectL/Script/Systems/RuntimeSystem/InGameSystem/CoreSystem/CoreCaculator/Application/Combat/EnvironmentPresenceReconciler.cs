using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>영향받은 캐릭터의 위치 기여분만 교체하는 순수 변경 제안이다.</summary>
    internal static class EnvironmentPresenceReconciler
    {
        internal static CharacterChanges Calculate(IReadOnlyList<CharacterCombatFacts> characters,
            MovementEnvironmentFacts finalEnvironment, IReadOnlyList<CellCoord> affectedCells,
            MainCharacterPersistentState main)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (finalEnvironment == null) throw new ArgumentNullException(nameof(finalEnvironment));
            if (affectedCells == null) throw new ArgumentNullException(nameof(affectedCells));
            if (main == null) throw new ArgumentNullException(nameof(main));
            var ids = new HashSet<EntityId>();
            foreach (var character in characters)
                if (character == null || !ids.Add(character.EntityId))
                    throw new ArgumentException("캐릭터 사실은 null이거나 중복된 개체일 수 없습니다.", nameof(characters));

            var affected = new HashSet<CellCoord>(affectedCells);
            var changes = new List<CharacterStateChange>();
            MainCharacterPersistentChange mainChange = null;
            foreach (var character in characters)
            {
                if (character.Area != CharacterRuntimeArea.Field || character.Health.IsDead ||
                    !character.OccupiedCells.Any(affected.Contains)) continue;

                if (character.OccupiedCells.Count != 1)
                {
                    // 다중 셀 접촉 규칙이 정해질 때까지 위치 효과가 없는 기존 배치만 유지한다.
                    if (character.PositionModifier.SourceIds.Count > 0 || character.OccupiedCells.Any(cell =>
                        PositionTileModifierCalculator.ResolveAt(cell, finalEnvironment).SourceIds.Count > 0))
                        throw new NotSupportedException("다중 셀 캐릭터의 위치 효과에는 접촉 정책이 필요합니다.");
                    continue;
                }

                if (character.OwnershipRole == CharacterOwnershipRole.Main)
                    CharacterSettlementCalculator.ValidateMain(character, main);
                var modifier = PositionTileModifierCalculator.ResolveAt(character.OccupiedCells[0], finalEnvironment);
                var final = PositionTileModifierCalculator.Replace(character, modifier);
                if (ReferenceEquals(character, final)) continue;
                changes.Add(new CharacterStateChange(character, final));
                if (character.OwnershipRole == CharacterOwnershipRole.Main &&
                    character.Health.CurrentHealth != final.Health.CurrentHealth)
                    mainChange = new MainCharacterPersistentChange(main,
                        CharacterSettlementCalculator.WithHealth(main, final.Health.CurrentHealth));
            }

            return new CharacterChanges(changes, Array.Empty<SoulCountChange>(), mainChange);
        }
    }
}
