using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    internal static class CellEnvironmentTestData
    {
        internal static readonly CellCoord Cell = new(1, 1);

        internal static CombatCommandState State(MovementEnvironmentFacts environment,
            params StructureCombatFacts[] structures) =>
            new(4, 5, CombatFlowPhase.PlayerInput, EntityId.Empty, false, structures,
                Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>(),
                environment: environment);

        internal static MovementEnvironmentFacts Environment(params MovementTileFact[] sources) => new(sources);

        internal static DestinationTileModifierFact Terrain(long id = 11, CellCoord? cell = null,
            bool active = true) => new(id, cell ?? Cell, active, new[] { Owner.Player }, 2, 3);

        internal static MovementTileFact Generic(long id = 12, CellCoord? cell = null,
            bool active = true, bool isMine = false) => new GenericSource(id, cell ?? Cell, active, isMine);

        internal static CharacterCombatFacts Character(int id = 11, int hp = 10,
            CharacterRuntimeArea area = CharacterRuntimeArea.Field, params CellCoord[] cells)
        {
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 4, 1,
                1, 2, 0, 5, 1, 4, 5, 0, 0,
                Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Base));
            bool field = area == CharacterRuntimeArea.Field;
            var occupied = field ? (cells.Length == 0 ? new[] { Cell } : cells) : Array.Empty<CellCoord>();
            return new CharacterCombatFacts(new EntityId(id), null, EntityKey.Unit_Akimbo,
                area == CharacterRuntimeArea.EnemyWaiting ? Owner.Enemy : Owner.Player,
                null, area, new HealthStateSnapshot(hp, 10), stats,
                area == CharacterRuntimeArea.Hand ? 0 : null,
                field ? occupied[0] : null, field ? Rotation4.R0 : null,
                occupied, field ? occupied[0] : CellCoord.Zero, 0, 0);
        }

        internal static StructureCombatFacts Structure(int id = 21, int hp = 10,
            params CellCoord[] cells)
        {
            var occupied = cells.Length == 0 ? new[] { Cell } : cells;
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 10, false, 0);
            return new StructureCombatFacts(new EntityId(id), definition,
                new HealthStateSnapshot(hp, 10), occupied[0], Rotation4.R0, occupied);
        }

        private sealed class GenericSource : MovementTileFact
        {
            internal GenericSource(long id, CellCoord cell, bool active, bool isMine)
                : this(id, cell, active, isMine, Array.Empty<Owner>()) { }

            private GenericSource(long id, CellCoord cell, bool active, bool isMine,
                IReadOnlyList<Owner> knownTo)
                : base(id, cell, "fixture-mine", 99, active, knownTo, isMine) { }

            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);

            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
                new GenericSource(Id, Cell, Active, IsMine, knownTo);
        }
    }
}
