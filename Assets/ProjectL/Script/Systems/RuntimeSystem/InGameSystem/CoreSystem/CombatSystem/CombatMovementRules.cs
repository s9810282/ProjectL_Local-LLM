using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>전투 서비스가 공유하는 이동 타일 규칙 목록의 유효성 검사다.</summary>
    internal static class CombatMovementRules
    {
        /// <summary>키가 유효하고 중복되지 않는지 확인한 읽기 전용 사본을 돌려준다.</summary>
        internal static IReadOnlyList<IMovementTileRule> Validated(IReadOnlyList<IMovementTileRule> movementRules)
        {
            if (movementRules == null) throw new ArgumentNullException(nameof(movementRules));
            var copiedRules = movementRules.ToArray();
            if (copiedRules.Any(r => r == null || string.IsNullOrWhiteSpace(r.Key)) ||
                copiedRules.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != copiedRules.Length)
                throw new ArgumentException("이동 규칙의 키는 유효하며 중복될 수 없습니다.", nameof(movementRules));
            return Array.AsReadOnly(copiedRules);
        }
    }
}
