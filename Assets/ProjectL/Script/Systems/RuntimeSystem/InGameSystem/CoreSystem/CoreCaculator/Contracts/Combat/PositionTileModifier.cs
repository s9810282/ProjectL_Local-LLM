using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>현재 캐릭터에 적용된 위치 기여분이다. 타일 원본 및 영혼·Artifact 보너스와 구분한다.</summary>
    public sealed class PositionTileModifier
    {
        public static PositionTileModifier None { get; } =
            new PositionTileModifier(null, Array.Empty<long>(), 0, 0);

        public CellCoord? SourceCell { get; }
        public IReadOnlyList<long> SourceIds { get; }
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }

        public PositionTileModifier(CellCoord? sourceCell, IReadOnlyList<long> sourceIds,
            int attackBonus, int maxHealthBonus)
        {
            if (sourceIds == null) throw new ArgumentNullException(nameof(sourceIds));
            var copy = sourceIds.ToArray();
            if (copy.Any(id => id < 1) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("위치 보정 원본 ID는 양수이며 중복될 수 없습니다.", nameof(sourceIds));
            if (sourceCell.HasValue != (copy.Length > 0) ||
                (!sourceCell.HasValue && (attackBonus != 0 || maxHealthBonus != 0)))
                throw new ArgumentException("위치 기여분에는 원본 셀과 ID가 함께 필요합니다.");
            SourceCell = sourceCell;
            SourceIds = Array.AsReadOnly(copy.OrderBy(id => id).ToArray());
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
        }

        public bool SameValues(PositionTileModifier other) => other != null &&
            SourceCell == other.SourceCell && AttackBonus == other.AttackBonus &&
            MaxHealthBonus == other.MaxHealthBonus && SourceIds.SequenceEqual(other.SourceIds);
    }
}
