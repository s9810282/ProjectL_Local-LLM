using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>초기 목록의 위치가 진입 순서를 정한다. 식별자와 순서 할당은 분리한다.</summary>
    public abstract class InitialObjectPlacementSpec
    {
        protected InitialObjectPlacementSpec() { }
        public abstract ObjectSourceFact CreateSource(long entryOrdinal);
    }

    public sealed class MinePlacementSpec : InitialObjectPlacementSpec
    {
        private readonly MineSourceFact _source;
        public MinePlacementSpec(long id, CellCoord cell, int damage, IReadOnlyList<Owner> knownTo)
            => _source = new MineSourceFact(id, cell, 1, damage, true, knownTo);
        public override ObjectSourceFact CreateSource(long entryOrdinal)
            => new MineSourceFact(_source.Id, _source.Cell, entryOrdinal, _source.Damage, true, _source.KnownTo);
    }

    public sealed class FlagPlacementSpec : InitialObjectPlacementSpec
    {
        private readonly FlagSourceFact _source;
        public FlagPlacementSpec(long id, CellCoord cell, int priorityBonus, IReadOnlyList<Owner> knownTo)
            => _source = new FlagSourceFact(id, cell, 1, priorityBonus, true, knownTo);
        public override ObjectSourceFact CreateSource(long entryOrdinal)
            => new FlagSourceFact(_source.Id, _source.Cell, entryOrdinal, _source.PriorityBonus, true, _source.KnownTo);
    }

    public sealed class SheriffBadgePlacementSpec : InitialObjectPlacementSpec
    {
        private readonly SheriffBadgeSourceFact _source;
        public SheriffBadgePlacementSpec(long id, CellCoord cell, IReadOnlyList<Owner> knownTo)
            => _source = new SheriffBadgeSourceFact(id, cell, 1, true, knownTo);
        public override ObjectSourceFact CreateSource(long entryOrdinal)
            => new SheriffBadgeSourceFact(_source.Id, _source.Cell, entryOrdinal, true, _source.KnownTo);
    }
}
