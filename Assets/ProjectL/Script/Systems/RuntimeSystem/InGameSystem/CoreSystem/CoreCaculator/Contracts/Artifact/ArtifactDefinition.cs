using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    public sealed class ArtifactDefinition
    {
        private readonly CellCoord[] _cells;
        private readonly ArtifactEffectData[] _effects;

        public int ArtifactId { get; }
        public int InitialCount { get; }
        public IReadOnlyList<CellCoord> Cells { get; }
        public IReadOnlyList<ArtifactEffectData> Effects { get; }

        public ArtifactDefinition(
            int artifactId,
            int initialCount,
            IReadOnlyList<CellCoord> cells,
            IReadOnlyList<ArtifactEffectData> effects)
        {
            if (artifactId < 0) throw new ArgumentOutOfRangeException(nameof(artifactId));
            if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Count == 0) throw new ArgumentException("At least one cell is required.", nameof(cells));
            if (effects == null) throw new ArgumentNullException(nameof(effects));

            ArtifactId = artifactId;
            InitialCount = initialCount;
            _cells = new CellCoord[cells.Count];
            var unique = new HashSet<CellCoord>();
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            for (int i = 0; i < cells.Count; i++)
            {
                CellCoord cell = cells[i];
                if (cell.X < 0 || cell.Y < 0)
                    throw new ArgumentOutOfRangeException(nameof(cells), "Cell coordinates must be zero or greater.");
                if (!unique.Add(cell))
                    throw new ArgumentException("Duplicate cells are not allowed.", nameof(cells));
                _cells[i] = cell;
                if (cell.X < minX) minX = cell.X;
                if (cell.Y < minY) minY = cell.Y;
            }

            if (minX != 0 || minY != 0)
                throw new ArgumentException("Cells must be normalized so their minimum X and Y are zero.", nameof(cells));

            _effects = effects.Count == 0 ? Array.Empty<ArtifactEffectData>()
                : new ArtifactEffectData[effects.Count];

            for (int i = 0; i < _effects.Length; i++)
            {
                ArtifactEffectData effect = effects[i];
                if (effect.durationTurns < 0)
                    throw new ArgumentOutOfRangeException(nameof(effects), $"Effect at index {i} must have zero or greater duration turns.");

                if (effect.effect.type == ArtifactEffectType.ModifyStat && string.IsNullOrWhiteSpace(effect.effect.statId))
                    throw new ArgumentException($"Effect at index {i} requires a stat ID for ModifyStat.", nameof(effects));

                _effects[i] = effect;
            }

            Cells = Array.AsReadOnly(_cells);
            Effects = Array.AsReadOnly(_effects);
        }

        public ArtifactCreateSnapshot CreateSnapshot()
        {
            var copiedCells = new CellCoord[_cells.Length];
            Array.Copy(_cells, copiedCells, _cells.Length);
            return new ArtifactCreateSnapshot(ArtifactId, CellCoord.Zero, copiedCells);
        }
    }
}
