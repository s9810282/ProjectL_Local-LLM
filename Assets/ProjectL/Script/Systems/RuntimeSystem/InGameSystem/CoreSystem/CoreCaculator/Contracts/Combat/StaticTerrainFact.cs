using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum StaticTerrainKind
    {
        HighGround = 1,
        Sandstorm = 2,
        Wall = 3
    }

    /// <summary>정적 지형의 종류를 식별하며 캐릭터 보정값이나 진입 효과 상태를 소유하지 않는다.</summary>
    public sealed class StaticTerrainFact : MovementTileFact
    {
        public const string Key = "static-terrain";
        public StaticTerrainKind Kind { get; }
        public override bool TriggersArrivalRule => false;

        public StaticTerrainFact(long id, CellCoord cell, StaticTerrainKind kind, bool active,
            IReadOnlyList<Owner> knownTo) : base(id, cell, Key, 0, active, knownTo)
        {
            if (!Enum.IsDefined(typeof(StaticTerrainKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
        }

        public override bool SameValues(MovementTileFact other) =>
            SameSourceValues(other) && other is StaticTerrainFact terrain && Kind == terrain.Kind;

        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new StaticTerrainFact(Id, Cell, Kind, Active, knownTo);
    }
}
