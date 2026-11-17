using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementLifecycleTests
    {
        internal static CombatCommandInput Turn(int ends = 1)
        {
            var seed = TornadoEnvironmentTests.Input(completed: 0, sources: Array.Empty<MovementTileFact>());
            var actor = CombatDebuffApplication.WithDebuffs(seed.Characters[0], new CharacterDebuffState(
                new IntimidationDebuffState(20), defenseReduction: new DefenseReductionDebuffState(30, ends)));
            return CombatCommandTestData.With(seed, new[] { actor, seed.Characters[1] }).WithActor(actor.EntityId);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void DeferredTimers_DoNotTickAtPlayerEnd(int ends)
        {
            var input = Turn(ends);
            var result = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(input.Characters[0].Debuffs.DefenseReduction.RemainingOwnTurnEnds, Is.EqualTo(ends));
        }

        [Test]
        public void SideStart_ResetsEligibilityWithoutChangingRetainedTimerPayloads()
        {
            var input = Turn(2);
            var start = EnemyControllerPhaseTests.Input(input, CombatControllerPhase.StartingPlayer);
            var result = CombatTurnCalculator.BeginControllerTurn(start, Array.Empty<IMovementTileRule>());
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters[0].Debuffs.SameValues(input.Characters[0].Debuffs), Is.True);
            Assert.That(final.Characters[0].ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void PreviewAndRejectedCommand_DoNotConsumeStatus()
        {
            var input = Turn(2);
            var invalid = CombatCommandComposition.Intent(input, CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(99, 99)));
            CombatCommandCalculator.Preview(invalid, Array.Empty<IMovementTileRule>());
            var rejected = CombatCommandCalculator.Calculate(invalid, Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.SameValues(input.State), Is.True);
            Assert.That(input.Characters[0].Debuffs.DefenseReduction.RemainingOwnTurnEnds, Is.EqualTo(2));
        }
    }
}
