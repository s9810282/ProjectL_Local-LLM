using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemLifetimeTests
    {
        [Test]
        public void ClearRoundItems_IsIdempotentAndPreservesOtherFacts()
        {
            var original = CombatCommandTestData.Input().Characters[0];
            var marked = CombatItemTimelineTests.Mark(CombatItemDirectDamageTests.Equip(original, 80, 1));
            var cleared = ItemLifetimeCalculator.ClearRoundItems(marked);
            Assert.That(cleared.Items.SameValues(CharacterItemState.Empty), Is.True);
            Assert.That(cleared.Health, Is.EqualTo(marked.Health));
            Assert.That(cleared.Stats, Is.EqualTo(marked.Stats));
            Assert.That(cleared.Debuffs, Is.SameAs(marked.Debuffs));
            Assert.That(ItemLifetimeCalculator.ClearRoundItems(cleared), Is.SameAs(cleared));
        }

        [Test]
        public void TemporaryStateCleanup_ClearsDebuffsSlotAndEffects_AndKeepsReferenceWhenEmpty()
        {
            var original = CombatCommandTestData.Input().Characters[0];
            Assert.That(CombatTemporaryStateCleanup.Clear(original), Is.SameAs(original));

            var marked = CombatDebuffApplication.WithDebuffs(
                CombatItemTimelineTests.Mark(CombatItemDirectDamageTests.Equip(original, 80, 1)),
                new CharacterDebuffState(new IntimidationDebuffState(20)));
            var events = new List<CombatEvent>();
            var cleared = CombatTemporaryStateCleanup.Clear(marked, events, ItemEventReason.Death);

            Assert.That(cleared.Items.SameValues(CharacterItemState.Empty), Is.True);
            Assert.That(cleared.Debuffs.SameValues(CharacterDebuffState.Empty), Is.True);
            Assert.That(cleared.Health, Is.EqualTo(marked.Health));
            var discard = events.OfType<CombatItemEvent>().Single();
            Assert.That((discard.Stage, discard.Reason, discard.OldItemKey), Is.EqualTo((ItemEventStage.Lifecycle, ItemEventReason.Death, (int?)1)));
            Assert.That(CombatTemporaryStateCleanup.Clear(cleared), Is.SameAs(cleared));
        }

        [Test]
        public void BattleEndTerminalHealth_ClearsMainSlotAndEffectsInCore()
        {
            var input = CombatItemDirectDamageTests.Input(10, 80, 1, mainTarget: true);
            var main = CombatItemTimelineTests.Mark(input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main));
            var completion = new BattleCompletionCalculator(default).Abandon(main, input.Main,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            var final = completion.Characters.Characters.Single().Final;
            Assert.That(final.Items.SameValues(CharacterItemState.Empty), Is.True);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void OutgoingEnvironmentalDamage_ActivatesWhiskeyBeforeRoundExpiry()
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var actor = CombatItemDirectDamageTests.Equip(CombatMovementCalculator.MoveTo(seed.Characters[0], CellCoord.Zero), 51, 1);
            var main = CombatItemTimelineTests.Mark(CombatItemDirectDamageTests.Equip(
                CombatMovementCalculator.MoveTo(seed.Characters[1], new CellCoord(2, 0)), 100, 2));
            var enemy = CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy);
            var input = CombatItemDirectDamageTests.Items(TornadoEnvironmentTests.Input(completed: 8,
                sources: new MovementTileFact[] { new MineSourceFact(34, new CellCoord(1, 1), 1, 1, true, new[] { Owner.Player, Owner.Enemy }) },
                characters: new[] { enemy, actor, main }, width: 3, height: 2, phase: CombatControllerPhase.Enemy));
            input = MovementTestData.Route(input.WithActor(enemy.EntityId), new CellCoord(2, 1), new CellCoord(1, 1));
            var movement = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(movement.Accepted, Is.True, movement.Reason);
            var day = CombatTurnCalculator.EndEnemyPhase(CombatCommandComposition.Project(movement), TornadoEnvironmentTests.Rules);
            var result = CombatCommandComposition.Merge(input, new[] { movement, day });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            Assert.That(CombatItemDirectDamageTests.Final(result, 1).Health.CurrentHealth, Is.EqualTo(60));
            Assert.That(CombatItemDirectDamageTests.Final(result, 10).Items.SameValues(CharacterItemState.Empty), Is.True);
            var events = result.Events.ToList();
            int activation = events.FindIndex(e => e is CombatItemEvent item && item.Reason == ItemEventReason.Activated);
            int expiry = events.FindIndex(e => e is CombatItemEvent item && item.Reason == ItemEventReason.RoundEnded);
            Assert.That(activation, Is.GreaterThanOrEqualTo(0));
            Assert.That(expiry, Is.GreaterThan(activation));
            Assert.That(result.InventoryChange, Is.Null);
        }

        [Test]
        public void RoundEnd_ClearsPlayerAndEnemyItems_AndRedeploymentDoesNotRegrant()
        {
            var seed = CombatCommandTestData.Input();
            var characters = seed.Characters.Select(c => CombatItemTimelineTests.Mark(
                CombatItemDirectDamageTests.Equip(c, c.Health.CurrentHealth, c.Owner == Owner.Enemy ? 2 : 1))).ToArray();
            var input = CombatItemDirectDamageTests.Items(TornadoEnvironmentTests.Input(sources: Array.Empty<MovementTileFact>(),
                completed: 8, phase: CombatControllerPhase.Enemy, characters: characters));
            var origins = characters.Where(c => c.Owner == Owner.Enemy)
                .Select(c => new CombatEnemyOrigin(c.EntityId, c.Anchor.Value, c.Rotation.Value)).ToArray();
            var t = input.State.Turn;
            var headquarters = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                new CellCoord(7, 0), Rotation4.R0, new[] { new CellCoord(7, 0) });
            var initial = new CombatCommandState(8,8,input.State.Phase,true,                new[] { headquarters },input.State.ThornFields,t);
            input = CombatCommandTestData.With(input, state: initial.With(turn: new CombatTurnFacts(StagePhase.Combat,
                controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: t.ControllerTurnSequence,
                completedPlayerTurnCount: 8, enemyOrigins: origins)));
            var result = CombatTurnCalculator.EndEnemyPhase(input, CombatItemDirectDamageTests.NoRules);
            var day = CombatCommandComposition.Project(result);
            Assert.That(day.Characters.All(c => c.Items.SameValues(CharacterItemState.Empty)), Is.True);
            Assert.That(day.Characters.Where(c => c.Owner == Owner.Enemy).All(c => c.Area == CharacterRuntimeArea.EnemyWaiting), Is.True);
            Assert.That(result.InventoryChange, Is.Null);
            var night = CombatCommandCalculator.Calculate(Recommand(day, CombatCommandKind.EnterCombat), CombatItemDirectDamageTests.NoRules);
            Assert.That(night.Accepted, Is.True, night.Reason);
            Assert.That(CombatCommandComposition.Project(night).Characters.Where(c => c.Owner == Owner.Enemy)
                .All(c => c.Items.EquippedItemKey == null), Is.True);
        }

        [Test]
        public void FallDiscardsWithoutActivation_AndSourceDeathDoesNotRemoveOtherTargetsEffects()
        {
            var seed = CombatCommandTestData.Input();
            var input = CombatItemDirectDamageTests.Items(CombatCommandTestData.With(seed,
                seed.Characters.Select(c => c.EntityId.Value == 2 ? CombatItemDirectDamageTests.Equip(c, 100, 1)
                    : c.EntityId.Value == 3 ? CombatItemTimelineTests.Mark(c) : c).ToArray()));
            var current = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, current, events);
            projection.ApplyFall(new EntityId(2), 71, new CellCoord(4, 3));
            var result = projection.Complete();
            Assert.That(CombatItemDirectDamageTests.Final(result, 2).Items.EquippedItemKey, Is.Null);
            Assert.That(CombatItemDirectDamageTests.Final(result, 3).Items.Effects.Single().ItemKey, Is.EqualTo(3));
            Assert.That(result.Events.OfType<CombatItemEvent>().Single().Reason, Is.EqualTo(ItemEventReason.Death));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealTerminalFinish_ClearsEverySlotAndEffectWithoutRefund(bool defeat)
        {
            var input = IntimidationScheduleTests.Scheduled();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => CombatItemTimelineTests.Mark(
                CombatItemDirectDamageTests.Equip(c, c.Health.CurrentHealth, 1))).ToArray());
            var s = input.State;
            if (!defeat)
            {
                var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                    1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(0, 100),
                    new CellCoord(7, 0), Rotation4.R0, new[] { new CellCoord(7, 0) });
                input = CombatCommandTestData.With(input, state: new CombatCommandState(8,8,s.Phase,s.IsNight,                    new[] { structure },s.ThornFields,s.Turn));
            }
            var changed = input.Characters.Single(c => c.EntityId.Value == (defeat ? 10 : 1));
            if (defeat) changed = CombatCommandCalculator.Hurt(changed, 100);
            var result = IntimidationScheduleTests.Finish(input, changed);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(defeat ? StageOutcome.Defeat : StageOutcome.Victory));
            Assert.That(CombatCommandComposition.Project(result).Characters.All(c => c.Items.SameValues(CharacterItemState.Empty)), Is.True);
            Assert.That(result.InventoryChange, Is.Null);
        }

        private static CombatCommandInput Recommand(CombatCommandInput i, CombatCommandKind kind, CombatCommandState state = null) =>
            new CombatCommandInput(1, kind, i.ActorId, i.Aim, state ?? i.State, i.Characters, i.Profiles, i.Main,
                i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: new CombatBattleFacts(4, 42, 2, 2,
                    Array.Empty<CharacterInstanceId>(), 0), itemInventory: i.ItemInventory, itemDefinitions: i.ItemDefinitions,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state ?? i.State).Turn.ControllerTurnSequence);
    }
}
