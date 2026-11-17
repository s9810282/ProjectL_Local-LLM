using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    internal static class PillarTestData
    {
        internal static (StructureCombatFacts Structure, ObstacleContentProfile Profile) Pillar(int id = 51,
            CellCoord? cell = null, int hits = 1, int length = 3)
        {
            var anchor = cell ?? new CellCoord(4, 3);
            var definition = new StructureSpec(EntityKey.Structure_Pillar, 1, 1, new[] { CellCoord.Zero }, 1, false, 0);
            return (new StructureCombatFacts(new EntityId(id), definition, new ObstacleDurabilitySnapshot(hits, hits),
                anchor, Rotation4.R0, new[] { anchor }), new ObstacleContentProfile(new EntityId(id), ObstacleContentKind.Pillar, 0, fallLength: length));
        }

        internal static CombatCommandInput Input(params MovementTileFact[] tiles)
        {
            var input = ObstacleReactionTests.Barrels(Pillar());
            return WithTerrain(input, tiles);
        }

        internal static CombatCommandInput WithTerrain(CombatCommandInput input, params MovementTileFact[] tiles)
        {
            var environment = new MovementEnvironmentFacts(tiles);
            return input.WithWorld(input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)), input.Characters, input.Main,
                input.SoulCounts, input.Souls, new EnvironmentSemanticBindings(tiles.Select(t => t.Id).ToArray(),
                    Array.Empty<long>(), input.EnvironmentBindings.ObstacleEntityIds));
        }

        internal static CombatCommandTransition Attack(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            CombatChangeEvidenceReader.ReadAll(result);
            return result;
        }
    }
}
