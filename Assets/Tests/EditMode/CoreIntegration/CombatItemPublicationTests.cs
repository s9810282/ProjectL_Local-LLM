using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatItemPublicationTests
    {
        private static readonly ItemDefinition[] Definitions =
        {
            new(1, new WhiskeyItemSpec(50, 10)), new(2, new LanternItemSpec(5))
        };

        [TestCase("Unchanged")]
        [TestCase("Inventory")]
        [TestCase("Slot")]
        [TestCase("Effect")]
        [TestCase("Random")]
        public void ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal(string changed)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ItemSource());
            var b = battle.Buffers;
            Assert.That(battle.Character(EntityKey.Unit_Demon).Items.EquippedItemKey, Is.EqualTo(1));
            // 입력 원본 검사에 실제 아이템 변경 제안을 포함하도록 피격자의 랜턴 상태를 기존 거래로 준비한다.
            var player = battle.Actor;
            b.BattleWriteSink.Apply(new CharacterChanges(new[]
            {
                new CharacterStateChange(player, ItemCharacterChanges.WithItems(player,
                    new CharacterItemState(2, player.Items.Effects)))
            }, Array.Empty<SoulCountChange>()));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            var enemy = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().EnemyStep();
            if (changed == "Random")
                input = input.WithWorld(input.State.With(itemRandomState: input.State.ItemRandomState + 1),
                    input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var proposal = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            Assert.That(proposal.Outcome.IsStageEnded, Is.False);
            Assert.That(proposal.Attack.Value.AttackerId, Is.EqualTo(enemy.EntityId));
            var proposedEnemy = proposal.Characters.Characters.Single(c => c.Final.EntityId == enemy.EntityId).Final;
            var proposedPlayer = proposal.Characters.Characters.Single(c => c.Final.EntityId == player.EntityId).Final;
            Assert.That(proposedEnemy.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(proposedEnemy.Items.Effects.Single().ItemKey, Is.EqualTo(2));
            Assert.That(proposedPlayer.Items.EquippedItemKey, Is.Null);
            Assert.That(proposedPlayer.Health.CurrentHealth, Is.LessThan(player.Health.CurrentHealth));

            string guard = null;
            if (changed == "Inventory")
            {
                var grant = ItemInventoryCalculator.Grant(b.ItemInventoryReadSink.Current, Definitions, 1, 1);
                Assert.That(grant.Accepted, Is.True, grant.Reason);
                b.ItemInventoryWriteSink.PublishGrant(grant.Change);
                guard = "아이템 재고의 기대값";
            }
            else if (changed == "Slot" || changed == "Effect")
            {
                var items = changed == "Slot" ? new CharacterItemState(2, enemy.Items.Effects)
                    : new CharacterItemState(enemy.Items.EquippedItemKey,
                        new[] { new ItemTemporaryEffect(2, ItemStatKind.PhysicalDefense, 3) });
                b.BattleWriteSink.Apply(new CharacterChanges(new[]
                {
                    new CharacterStateChange(enemy, ItemCharacterChanges.WithItems(enemy, items))
                }, Array.Empty<SoulCountChange>()));
                guard = "캐릭터 원본";
            }
            else if (changed == "Random")
            {
                guard = "전투 원본";
            }

            var before = new CoreIntegrationSnapshot(b);
            if (changed != "Unchanged")
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal)).Message, Does.Contain(guard));
                before.AssertUnchanged(b);
                return;
            }

            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            b.BattleWriteSink.Apply(proposal);
            Assert.That(battle.Character(EntityKey.Unit_Demon).SameValues(proposedEnemy), Is.True);
            Assert.That(battle.Actor.SameValues(proposedPlayer), Is.True);
            Assert.That(b.ItemInventoryReadSink.Current.SameValues(input.ItemInventory), Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(b.CombatStateReadSink.Current.CommandState.ItemRandomState, Is.EqualTo(proposal.FinalState.ItemRandomState));
        }

        private sealed class ItemSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var stage = new StageConfig(8, 6, 8, 2, 3, 42,
                    new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(0, 3), Rotation4.R180, startingItemKey: 1) },
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), source.Stage.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, source.MainAnchor, source.MainRotation, source.Units,
                    source.Structures, source.Categories, source.AttackProfiles, itemDefinitions: Definitions, clearRewards: source.ClearRewards);
            }
        }
    }
}
