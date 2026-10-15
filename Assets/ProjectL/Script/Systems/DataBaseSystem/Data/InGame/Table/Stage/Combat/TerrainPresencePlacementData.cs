using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>기존 위치 보정이 지원하는 공격력과 최대 체력의 불변 저작 값이다.</summary>
    public sealed class TerrainPresenceDefinitionData
    {
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }
        public TerrainPresenceDefinitionData(int attackBonus, int maxHealthBonus)
        {
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
        }
    }

    /// <summary>스테이지의 원본 ID와 셀을 정의 값에 연결하며 런타임 상태는 보관하지 않는다.</summary>
    public sealed class TerrainPresencePlacementData
    {
        public long SourceId { get; }
        public CellCoord Cell { get; }
        public TerrainPresenceDefinitionData Definition { get; }
        public IReadOnlyList<Owner> KnownTo { get; }
        public TerrainPresencePlacementData(long sourceId, CellCoord cell,
            TerrainPresenceDefinitionData definition, IReadOnlyList<Owner> knownTo)
        {
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (knownTo == null) throw new ArgumentNullException(nameof(knownTo));
            var visibility = knownTo.ToArray();
            if (visibility.Any(owner => owner != Owner.Player && owner != Owner.Enemy) ||
                visibility.Distinct().Count() != visibility.Length)
                throw new ArgumentException("가시성 소유자는 Player 또는 Enemy이며 중복될 수 없습니다.", nameof(knownTo));

            SourceId = sourceId;
            Cell = cell;
            Definition = definition;
            KnownTo = Array.AsReadOnly(visibility);
        }
    }
}
