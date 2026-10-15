using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>정적 지형의 작성 위치와 종류다. 전투 중 상태와 캐릭터 능력치는 보관하지 않는다.</summary>
    public sealed class StaticTerrainPlacementData
    {
        public long SourceId { get; }
        public CellCoord Cell { get; }
        public StaticTerrainKind Kind { get; }
        public IReadOnlyList<Owner> KnownTo { get; }

        public StaticTerrainPlacementData(long sourceId, CellCoord cell, StaticTerrainKind kind, IReadOnlyList<Owner> knownTo)
        {
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (!System.Enum.IsDefined(typeof(StaticTerrainKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (knownTo == null) throw new ArgumentNullException(nameof(knownTo));
            var visibility = knownTo.ToArray();
            if (visibility.Any(owner => owner != Owner.Player && owner != Owner.Enemy) ||
                visibility.Distinct().Count() != visibility.Length)
                throw new ArgumentException("가시성 소유자는 Player 또는 Enemy이며 중복될 수 없습니다.", nameof(knownTo));
            SourceId = sourceId; Cell = cell; Kind = kind; KnownTo = Array.AsReadOnly(visibility);
        }
    }
}
