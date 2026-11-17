using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class MovementUndoOriginFacts
    {
        private readonly IReadOnlyList<MeteorFlameArea> _meteorFlames;

        public CellCoord Cell { get; }
        public IReadOnlyList<MovementTileFact> Tiles { get; }
        public IReadOnlyList<long> TerrainSourceIds { get; }
        public IReadOnlyList<long> ObjectSourceIds { get; }
        public IReadOnlyList<SoulFieldFacts> Souls { get; }

        private MovementUndoOriginFacts(CellCoord cell, CombatCommandState state,
            EnvironmentSemanticBindings bindings, SoulFieldSnapshot souls)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (souls == null) throw new ArgumentNullException(nameof(souls));
            Cell = cell;
            Tiles = Array.AsReadOnly(state.Environment.Tiles.Where(t => t is not TraitTrailFact &&
                (t is MovingPlatformFact platform ? platform.Occupies(cell) : t.Cell == cell)).OrderBy(t => t.Id).ToArray());
            var sourceIds = Tiles.Select(t => t.Id).ToHashSet();
            TerrainSourceIds = Array.AsReadOnly(bindings.TerrainSourceIds.Where(sourceIds.Contains).OrderBy(id => id).ToArray());
            ObjectSourceIds = Array.AsReadOnly(bindings.ObjectSourceIds.Where(sourceIds.Contains).OrderBy(id => id).ToArray());
            Souls = Array.AsReadOnly(souls.Entries.Where(s => s.Cell == cell).OrderBy(s => s.Id).ToArray());
            var meteor = state.Gimmicks.Entries.OfType<MeteorGimmickState>().SingleOrDefault();
            _meteorFlames = meteor == null ? Array.Empty<MeteorFlameArea>() :
                Array.AsReadOnly(meteor.Flames.Where(f => f.Footprint.Cells.Contains(cell)).ToArray());
        }

        public static MovementUndoOriginFacts Capture(CellCoord cell, CombatCommandState state,
            EnvironmentSemanticBindings bindings, SoulFieldSnapshot souls) => new MovementUndoOriginFacts(cell, state, bindings, souls);

        public bool SameValues(MovementUndoOriginFacts other) => other != null && Cell == other.Cell &&
            Tiles.Count == other.Tiles.Count && Tiles.Zip(other.Tiles, (a, b) => a.SameValues(b)).All(same => same) &&
            TerrainSourceIds.SequenceEqual(other.TerrainSourceIds) && ObjectSourceIds.SequenceEqual(other.ObjectSourceIds) &&
            Souls.Count == other.Souls.Count &&
            Souls.Zip(other.Souls, (a, b) => a.SameValues(b)).All(same => same) &&
            _meteorFlames.Count == other._meteorFlames.Count &&
            _meteorFlames.Zip(other._meteorFlames, (a, b) => a.SameValues(b)).All(same => same);
    }
}
