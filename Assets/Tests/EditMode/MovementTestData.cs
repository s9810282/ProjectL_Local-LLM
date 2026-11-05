using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    internal static class MovementTestData
    {
        internal static CombatCommandInput WithMovement(CombatCommandInput input,int allowance) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: c.Stats.With(moveRange: allowance)) : c).ToArray());

        internal static CombatCommandInput SafeRoute(params CellCoord[] cells)
        {
            var input = CombatCommandTestData.Input(empty: true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            actor = actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, cells[0], actor.Rotation, new[] { cells[0] });
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                state: input.State.With());
            return Route(input, cells);
        }

        internal static CharacterCombatFacts Actor(CombatCommandTransition transition) =>
            transition.Characters.Characters.FirstOrDefault(c => c.Final.EntityId == transition.Input.ActorId)?.Final
            ?? transition.Input.Characters.Single(c => c.EntityId == transition.Input.ActorId);

        internal static CombatCommandInput After(CombatCommandTransition transition) => CombatCommandComposition.Project(transition);

        internal static CombatCommandInput Route(CombatCommandInput source, params CellCoord[] cells)
        {
            var path = new MovementPath(cells);
            return source.WithIntent(CombatCommandKind.Move, default, path.Endpoint, path, null);
        }
    }
}
