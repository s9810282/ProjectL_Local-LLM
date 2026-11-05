using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public enum TraitAreaFootprint
    {
        SquareNeighbors = 0,
        OrthogonalCross = 1
    }

    public sealed class TraitAreaSpec
    {
        public TraitAreaFootprint Footprint { get; }
        public int RadiusCells { get; }
        public bool IncludeCenter { get; }

        public TraitAreaSpec(TraitAreaFootprint footprint, int radiusCells, bool includeCenter = false)
        {
            if (!Enum.IsDefined(typeof(TraitAreaFootprint), footprint)) throw new ArgumentOutOfRangeException(nameof(footprint));
            if (radiusCells < 1) throw new ArgumentOutOfRangeException(nameof(radiusCells));
            Footprint = footprint;
            RadiusCells = radiusCells;
            IncludeCenter = includeCenter;
        }
    }
}
