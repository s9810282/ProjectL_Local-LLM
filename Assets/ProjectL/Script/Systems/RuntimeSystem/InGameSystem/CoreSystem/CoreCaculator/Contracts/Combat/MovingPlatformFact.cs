using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>플랫폼의 현재 기준 칸과 진행 부호다. 점유 셀은 정의와 기준 칸에서 파생한다.</summary>
    public sealed class MovingPlatformFact : MovementTileFact
    {
        public const string Key = "moving-platform";
        private static readonly Owner[] PublicSides = { Owner.Player, Owner.Enemy };
        public MovingPlatformSpec Spec { get; }
        public int TravelSign { get; }
        public IReadOnlyList<CellCoord> OccupiedCells => Spec.FootprintAt(Cell);
        public override bool TriggersArrivalRule => false;

        public MovingPlatformFact(MovingPlatformSpec spec)
            : this(spec ?? throw new ArgumentNullException(nameof(spec)), spec.InitialAnchor, spec.InitialSign)
        {
        }

        public MovingPlatformFact(MovingPlatformSpec spec, CellCoord anchor, int travelSign)
            : this(spec, anchor, travelSign, PublicSides)
        {
        }

        private MovingPlatformFact(MovingPlatformSpec spec, CellCoord anchor, int travelSign, IReadOnlyList<Owner> knownTo)
            : base((spec ?? throw new ArgumentNullException(nameof(spec))).SourceId, anchor, Key, 0, true, knownTo)
        {
            if (!spec.ContainsAnchor(anchor)) throw new ArgumentOutOfRangeException(nameof(anchor));
            if (travelSign is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(travelSign));
            Spec = spec;
            TravelSign = travelSign;
        }

        public bool Occupies(CellCoord cell)
        {
            for (int index = 0; index < Spec.Length; index++)
                if (Spec.CellAt(Cell, index) == cell) return true;
            return false;
        }

        public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
            other is MovingPlatformFact platform && TravelSign == platform.TravelSign && Spec.SameValues(platform.Spec);

        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new MovingPlatformFact(Spec, Cell, TravelSign, knownTo);
    }
}
