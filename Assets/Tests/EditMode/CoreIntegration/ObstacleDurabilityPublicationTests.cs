using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ObstacleDurabilityPublicationTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord BarrelCell = new CellCoord(0, 2);

        [Test]
        public void AuthoringRequiresAnExplicitHitCount()
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<ObstacleDefinitionDataSO>();
            Assert.Throws<InvalidOperationException>(() => asset.GetData());
            CoreAuthoringFixture.Set(asset, "requiredHits", 4);
            var data = (ObstacleDefinitionData)asset.GetData();
            var placement = data.CreatePlacement(BarrelCell, Rotation4.R0);
            Assert.That(placement.RequiredHits, Is.EqualTo(4));
            Assert.That(placement.Definition.MaxHealth, Is.EqualTo(1));
        }

        [TestCase("missing")]
        [TestCase("reordered")]
        [TestCase("forged-contact")]
        [TestCase("capacity")]
        [TestCase("numeric-liveness")]
        public void InvalidDurabilityProofLeavesBuffersUnchanged(string fault)
        {
            using var battle = Create();
            var parts = battle.Publication();
            var valid = CombatCommandCalculator.Calculate(parts.Attack(battle.Actor, BarrelCell), NoRules);
            Assert.That(valid.Accepted, Is.True, valid.Reason);
            var damage = valid.Events.OfType<CombatObstacleDurabilityEvent>().Single();
            var final = valid.FinalState;
            var events = valid.Events.ToList();
            int index = events.IndexOf(damage);
            switch (fault)
            {
                case "missing":
                    events.RemoveAt(index);
                    break;
                case "reordered":
                    (events[index], events[index + 1]) = (events[index + 1], events[index]);
                    break;
                case "forged-contact":
                    var forged = new CombatDamageOrigin(damage.Origin.PhysicalCell,
                        damage.Origin.CreditedCharacterId, damage.Origin.AttackGroup);
                    events[index] = new CombatObstacleDurabilityEvent(damage.Expected, damage.Final, damage.Cause, forged);
                    break;
                case "capacity":
                    var resized = new StructureCombatFacts(damage.Final.EntityId, damage.Final.Definition,
                        new ObstacleDurabilitySnapshot(2, 4), damage.Final.Anchor, damage.Final.Rotation,
                        damage.Final.OccupiedCells, damage.Final.Owner);
                    events[index] = new CombatObstacleDurabilityEvent(damage.Expected, resized, damage.Cause, damage.Origin);
                    final = final.With(structures: final.Structures.Select(s => s.EntityId == resized.EntityId ? resized : s).ToArray());
                    break;
                case "numeric-liveness":
                    var numeric = new StructureCombatFacts(damage.Final.EntityId, damage.Final.Definition,
                        new HealthStateSnapshot(1, 1), damage.Final.Anchor, damage.Final.Rotation, damage.Final.OccupiedCells,
                        damage.Final.Owner);
                    final = final.With(structures: final.Structures.Select(s => s.EntityId == numeric.EntityId ? numeric : s).ToArray());
                    break;
                default:
                    Assert.Fail("Unknown test case");
                    break;
            }

            var proposal = new CombatCommandTransition(valid.Input, valid.Accepted, valid.Reason, valid.Characters,
                valid.Field, final, valid.Outcome, valid.Attack, valid.ConsumedSoulIds, movement: valid.Movement,
                events: events, actionUseSettlement: valid.ActionUseSettlement, movementEffectUses: valid.MovementEffectUses,
                healthExecutions: valid.HealthExecutions, healthClaims: valid.HealthClaims,
                rewardChange: valid.RewardChange, rewardSettlement: valid.RewardSettlement);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var error = Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal));
            Assert.That(error.Message, Does.Contain(fault == "numeric-liveness" ? "구조물 체력" : "장애물 내구도 근거"));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(parts.Publisher.Publish(valid), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Structures.Single(s => s.EntityId == damage.ObstacleId)
                .ObstacleDurability, Is.EqualTo(new ObstacleDurabilitySnapshot(2, 3)));
        }

        [Test]
        public void ExplicitDetonationPublishesOnceAndRejectsStaleState()
        {
            using var battle = Create();
            var parts = battle.Publication();
            var input = parts.Attack(battle.Actor, BarrelCell);
            var nextNumber = parts.Attack(battle.Actor, BarrelCell);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var id = input.State.Structures.Single(s => s.ObstacleDurability != null).EntityId;
            var origin = new CombatDamageOrigin(battle.Actor.Anchor, battle.Actor.EntityId);
            var proposal = ObstacleCommandCalculator.DetonatePowderBarrel(input, id, origin, NoRules);
            var stale = ObstacleCommandCalculator.DetonatePowderBarrel(nextNumber, id, origin, NoRules);
            Assert.That(proposal.Accepted, Is.True);
            before.AssertUnchanged(battle.Buffers);
            Assert.That(parts.Publisher.Publish(proposal), Is.True);
            var store = battle.Buffers.CombatStateReadSink;
            Assert.That(store.Current.CommandState.Structures.Single(s => s.EntityId == id).ObstacleDurability,
                Is.EqualTo(new ObstacleDurabilitySnapshot(0, 3)));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatObstacleDurabilityEvent>().Single().Cause,
                Is.EqualTo(ObstacleDestructionCause.ExplicitDetonation));
            var after = new CoreIntegrationSnapshot(battle.Buffers);
            parts.Publisher.Publish(proposal);
            after.AssertUnchanged(battle.Buffers);
            Assert.That(Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(stale)).Message,
                Does.Contain("명령의 전투 원본"));
            after.AssertUnchanged(battle.Buffers);
        }

        private static ConfiguredMovementBattle Create()
        {
            var spec = new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1,
                new[] { CellCoord.Zero }, 1, false, 0);
            return new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(new PullTraitSpec(1), target: new CellCoord(7, 5))
                {
                    Obstacle = new InitialObstaclePlacementSpec(spec, ObstacleContentKind.PowderBarrel,
                        BarrelCell, Rotation4.R0, 3, 0)
                });
        }
    }
}
