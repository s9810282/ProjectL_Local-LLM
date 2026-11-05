using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    internal static class MovementUndoTestData
    {
        internal static CombatCommandInput Route(int priorDistance = 0) => WithPrior(MovementTestData.WithMovement(
            MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1), new CellCoord(4, 1)), 4), priorDistance);

        private static CombatCommandInput WithPrior(CombatCommandInput input, int prior) => CombatCommandTestData.With(input,
            characters: input.Characters.Select(c => c.EntityId == input.ActorId ? c.With(stageRecord: CharacterStageRecord.Empty.Add(2, 7, prior, 1)) : c).ToArray());

        internal static CombatCommandTransition Run(CombatCommandInput input, params IMovementTileRule[] rules) => CombatCommandCalculator.Calculate(input, rules);

        internal static CombatCommandInput WithEnemy(CombatCommandInput input) => CombatCommandTestData.With(input,
            characters: input.Characters.Append(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy)).ToArray());

        internal static CombatCommandInput After(CombatCommandTransition transition)
        {
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            return CombatCommandComposition.Project(transition);
        }

        internal static CombatCommandInput Command(CombatCommandInput source, CombatCommandKind kind, int token = 0,
            EntityId? actor = null, MovementPath path = null, AttackAim? aim = null, int? number = null,
            long? expectedBattle = null, long? expectedTurn = null) => new CombatCommandInput(number ?? source.Number + 1, kind,
                actor ?? source.ActorId, aim ?? source.Aim, source.State, source.Characters, source.Profiles, source.Main,
                source.Owned, source.SoulCounts, source.Souls, source.Artifacts, path?.Endpoint ?? default, source.Rotation,
                source.Battle, CombatProcessingKind.Command, path, null, source.BattleInstanceId, source.ArtifactItems,
                source.EnvironmentBindings, source.ObstacleContents, source.ItemKey, source.ItemInventory, source.ItemDefinitions,
                source.GoldBalance, source.FameBalance, source.Membership, expectedBattle ?? source.BattleInstanceId,
                expectedTurn ?? source.State.Turn.ControllerTurnSequence, token);

        internal static CombatCommandInput Undo(CombatCommandTransition movement) => Command(After(movement), CombatCommandKind.UndoMove, movement.Input.Number);
    }
}
