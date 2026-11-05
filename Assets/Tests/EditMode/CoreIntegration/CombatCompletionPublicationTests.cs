using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatCompletionPublicationTests
    {
        [TestCase("Command")]
        [TestCase("Advance")]
        [TestCase("Abandon")]
        public void FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication(string operation)
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            IBattleInitializationSource source = operation == "Advance" ? new LethalEnemySource()
                : new ConfiguredMovementBattle.EncounterSource(terminalHeadquarters: true);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, source, database: database);
            var b = battle.Buffers;
            if (operation == "Command")
            {
                var input = battle.Publication().Attack(battle.Actor, new CellCoord(1, 3));
                var beforeCalculation = new CoreIntegrationSnapshot(b);
                var proposal = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
                Assert.That(proposal.Accepted, Is.True, proposal.Reason);
                Assert.That(proposal.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
                Assert.That(proposal.Attack.HasValue, Is.True);
                beforeCalculation.AssertUnchanged(b);
            }
            else if (operation == "Advance")
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
                var beforeCalculation = new CoreIntegrationSnapshot(b);
                var proposal = CombatCommandCalculator.Advance(battle.Publication().EnemyStep(), Array.Empty<IMovementTileRule>());
                Assert.That(proposal.Accepted, Is.True, proposal.Reason);
                Assert.That(proposal.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
                Assert.That(proposal.Attack.HasValue, Is.True);
                Assert.That(proposal.Attack.Value.AttackerId, Is.EqualTo(battle.Character(EntityKey.Unit_Demon).EntityId));
                beforeCalculation.AssertUnchanged(b);
            }

            var beforeLookup = new CoreIntegrationSnapshot(b);
            CoreIntegrationSnapshot afterRunEnd = null;
            int lookups = 0;
            database.BeforeStageRead = key =>
            {
                lookups++;
                Assert.That(key, Is.EqualTo(1101));
                beforeLookup.AssertUnchanged(b);
                battle.EndRun();
                afterRunEnd = new CoreIntegrationSnapshot(b);
            };

            bool result = operation switch
            {
                "Command" => battle.Core.CombatCommands.ProcessNextCommand(),
                "Advance" => battle.Core.CombatAdvance.AdvanceCombat(),
                _ => battle.Core.BattleCompletion.AbandonBattle() == BattleAbandonResult.Abandoned
            };

            Assert.That(result, Is.False);
            Assert.That(lookups, Is.EqualTo(1));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(b.RunProgressReadSink.Completion, Is.Not.Null);
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(afterRunEnd, Is.Not.Null);
            afterRunEnd.AssertUnchanged(b);
        }

        private sealed class LethalEnemySource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = source.Units.Select(s => s.Key == EntityKey.Unit_Demon
                    ? new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                        s.MaxHealth, s.MoveMethod, 3, s.CategoryKey, 100, s.PhysicalDefense, s.Priority, s.Traits.ToArray(),
                        s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: s.KillReward) : s).ToArray();
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, units,
                    source.Structures, source.Categories, units.Select(u => new CombatAttackProfile(u, source.AttackProfiles.First().Method)).ToArray(),
                    clearRewards: source.ClearRewards);
            }
        }
    }
}
