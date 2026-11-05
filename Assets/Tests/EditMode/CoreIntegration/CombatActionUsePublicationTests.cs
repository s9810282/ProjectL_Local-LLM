using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatActionUsePublicationTests
    {
        [TestCase(3, 4, 3, 2, 0)]
        [TestCase(5, 6, 7, 8, 1)]
        public void ConfiguredCategoryAuthoring_PreservesDistancesWithoutPrices(int main, int rider, int heavy, int cannon, int boss)
        {
            using var fixture = new CoreAuthoringFixture();
            var source = fixture.Create<CategoryConfigDataSO>();
            string[] categories = { "mainMovement", "riderMovement", "heavyMovement", "cannonMovement", "bossMovement" };
            int[] distances = { main, rider, heavy, cannon, boss };
            using var serialized = new SerializedObject(source);
            for (int i = 0; i < categories.Length; i++)
                serialized.FindProperty(categories[i]).FindPropertyRelative("movementAllowance").intValue = distances[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var config = (CategoryConfigData)source.GetData();
            Assert.DoesNotThrow(config.Validate);
            Assert.That(new[] { config.MainMovementAllowance, config.RiderMovementAllowance,
                config.HeavyMovementAllowance, config.CannonMovementAllowance, config.BossMovementAllowance },
                Is.EqualTo(distances));
            foreach (var category in categories)
                Assert.That(serialized.FindProperty(category).FindPropertyRelative("moveCost"), Is.Null);
        }

        [TestCase("missing")]
        [TestCase("actor")]
        [TestCase("before")]
        [TestCase("final")]
        [TestCase("source")]
        public void InvalidActionEvidence_RejectsEveryStore(string mutation)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var evidence = transition.ActionUseSettlement;
            evidence = mutation switch
            {
                "missing" => null,
                "actor" => new UnitActionUseSettlementSnapshot(battle.Main.EntityId, evidence.BeforeActionUse, evidence.FinalActionUse),
                "before" => new UnitActionUseSettlementSnapshot(evidence.ActorId, UnitActionUse.Moved, evidence.FinalActionUse),
                "final" => new UnitActionUseSettlementSnapshot(evidence.ActorId, evidence.BeforeActionUse, UnitActionUse.Moved),
                _ => evidence
            };
            if (mutation == "source") input = AsAdvance(input);
            var forged = new CombatCommandTransition(input, transition.Accepted, transition.Reason,
                transition.Characters, transition.Field, transition.FinalState, transition.Outcome,
                transition.Attack, transition.ConsumedSoulIds, transition.Completion, transition.Movement,
                transition.Events, transition.PathMovement, transition.EnvironmentBindings, transition.BlockedContacts,
                transition.InventoryChange, transition.RewardChange, transition.RewardSettlement, evidence);
            var original = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            original.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void RejectedRequest_CannotPublishActionUseChanges()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var actor = battle.Actor;
            var input = battle.Publication().Attack(actor, new CellCoord(7, 5));
            var rejected = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            var original = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, false, rejected.Reason,
                new CharacterChanges(new[] { new CharacterStateChange(actor, actor.With(actionUse: UnitActionUse.Attacked)) },
                    Array.Empty<SoulCountChange>()), rejected.Field, rejected.FinalState, rejected.Outcome));
            original.AssertUnchanged(battle.Buffers);
        }

        private static CombatCommandInput AsAdvance(CombatCommandInput i) => new CombatCommandInput(0, i.Kind,
            i.ActorId, i.Aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
            i.TargetCell, i.Rotation, i.Battle, CombatProcessingKind.Advance, i.Path, i.MovementContext,
            i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents, i.ItemKey,
            i.ItemInventory, i.ItemDefinitions, i.GoldBalance, i.FameBalance, i.Membership,
            i.ExpectedBattleInstanceId, i.ExpectedControllerTurnSequence);
    }
}
