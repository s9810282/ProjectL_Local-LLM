using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class EightTurnNightTests
    {
        [TestCase(0)]
        [TestCase(8)]
        public void EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay(long baseline)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var onlyMain = source.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Main).ToArray();
            for (int count = 1; count <= 8; count++)
            {
                var player = EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Player,
                    baseline + count - 1, baseline, characters: onlyMain);
                var ended = CombatTurnCalculator.EndPlayerTurn(player, Array.Empty<IMovementTileRule>());
                Assert.That(ended.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(baseline + count));
                Assert.That(ended.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Combat));
                Assert.That(ended.Events.Any(e => e.Kind == CombatEventKind.NightEnded), Is.False);
                var enemy = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(ended));
                var response = CombatTurnCalculator.EndEnemyPhase(CombatCommandComposition.Project(enemy), Array.Empty<IMovementTileRule>());
                Assert.That(response.Accepted, Is.True, response.Reason);
                Assert.That(response.FinalState.Turn.StagePhase,
                    Is.EqualTo(count == 8 ? StagePhase.Deployment : StagePhase.Combat));
                Assert.That(response.FinalState.Turn.PreparationIndex, Is.EqualTo(count == 8 ? 2 : 1));
                Assert.That(response.Events.Count(e => e.Kind == CombatEventKind.NightEnded), Is.EqualTo(count == 8 ? 1 : 0));
                Assert.That(response.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(baseline + count));
            }
        }

        [Test]
        public void EighthResponseExpiresItemsAndSuppliesOnMain_BeforeDay()
        {
            var source = CombatCommandTestData.Input(empty: true);
            var main = source.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            main = HeldEnvironmentEffects.GrantSupplies(main, 5);
            main = ItemCharacterChanges.WithItems(main, new CharacterItemState(81,
                new[] { new ItemTemporaryEffect(80, ItemStatKind.PhysicalDefense, 10, 1) }));
            var input = EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Enemy, 8, characters: new[] { main });
            var result = CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            var final = CombatCommandComposition.Project(result).Characters.Single();
            Assert.That(final.Items.SameValues(CharacterItemState.Empty), Is.True);
            Assert.That(final.HeldEnvironment.SuppliesAttackBonus, Is.Zero);
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(result.FinalState.IsNight, Is.False);
            Assert.That(result.FinalState.Turn.PreparationIndex, Is.EqualTo(2));
        }

        [Test]
        public void TerminalEighthResponseCannotPublishDayOrStart()
        {
            var source = EnemyControllerPhaseTests.Input(CombatCommandTestData.Input(), CombatControllerPhase.Enemy, 8);
            var ended = source.WithWorld(source.State.With(phase: CombatFlowPhase.Finished), source.Characters,
                source.Main, source.SoulCounts, source.Souls, source.EnvironmentBindings);
            var day = CombatTurnCalculator.EndEnemyPhase(ended, Array.Empty<IMovementTileRule>());
            Assert.That(day.Accepted, Is.False);
            Assert.That(day.Events, Is.Empty);
            Assert.That(CombatTurnCalculator.BeginControllerTurn(ended).Accepted, Is.False);
        }
    }
}
