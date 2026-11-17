using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    internal static class PlatformTestData
    {
        internal static readonly StructureCombatFacts[] NoStructures = Array.Empty<StructureCombatFacts>();
        internal static readonly CharacterCombatFacts[] NoCharacters = Array.Empty<CharacterCombatFacts>();

        internal static MovingPlatformFact Platform(long id, CellCoord start, CellCoord end, int length = 1,
            PlatformAxis shape = PlatformAxis.Horizontal, CellCoord? anchor = null) =>
            new MovingPlatformFact(new MovingPlatformSpec(id, length, shape, start, end, anchor ?? start, 1));

        internal static StaticTerrainFact Terrain(long id, CellCoord cell, StaticTerrainKind kind = StaticTerrainKind.Cliff) =>
            new StaticTerrainFact(id, cell, kind, true, new[] { Owner.Player, Owner.Enemy });

        internal static EnvironmentSemanticBindings Bind(MovementEnvironmentFacts environment) =>
            new EnvironmentSemanticBindings(environment.Tiles.Select(source => source.Id).ToArray(),
                EnvironmentSemanticBindings.Empty.ObjectSourceIds, EnvironmentSemanticBindings.Empty.ObstacleEntityIds);

        internal static CombatCommandInput OnBoard(CombatCommandInput input, params MovementTileFact[] sources)
        {
            var environment = new MovementEnvironmentFacts(sources);
            var state = input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment));
            return input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, Bind(environment));
        }
    }
}
