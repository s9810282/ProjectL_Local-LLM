using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PeriodicStatusTurnTests
    {
        internal static EffectOriginKey Origin(int id = 1) => new(EffectOwnerKind.Trait, id, 480, "bleeding");
        internal static StatusEffectSet Both(int fireTicks = 2, int bleedingTicks = 3) => new(new AppliedStatus[]
        {
            new FireStatus(Origin(), fireTicks),
            new BleedingStatus(new[] { new BleedingStack(Origin(), 1), new BleedingStack(Origin(10), 3) }, bleedingTicks)
        });

        [Test]
        public void FireTicksTwo_BleedingTicksThree_OnAffectedStarts()
        {
            var input = WithStatus(CombatCommandTestData.Input(), 2, Both());
            int[] health = { 94, 88, 84, 84 };
            for (int i = 0; i < health.Length; i++)
            {
                var other = Start(input, Owner.Player);
                Assert.That(Final(other, 2).Health.CurrentHealth, Is.EqualTo(input.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth));
                var tick = Start(CombatCommandComposition.Project(other), Owner.Enemy);
                var recipient = Final(tick, 2);
                Assert.That(recipient.Health.CurrentHealth, Is.EqualTo(health[i]));
                Assert.That(recipient.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault()?.RemainingAffectedSideStarts ?? 0,
                    Is.EqualTo(Math.Max(0, 1 - i)));
                Assert.That(recipient.StatusEffects.Entries.OfType<BleedingStatus>().SingleOrDefault()?.RemainingAffectedSideStarts ?? 0,
                    Is.EqualTo(Math.Max(0, 2 - i)));
                input = CombatCommandComposition.Project(tick);
            }
        }

        [Test]
        public void ShieldExpiryPrecedesTicks()
        {
            var shield = new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.PositionSwap, "shield"), 20, true, 0, Owner.Enemy);
            var states = new StatusEffectSet(Both().Entries.Append(shield).ToArray());
            var result = Start(WithStatus(CombatCommandTestData.Input(), 2, states), Owner.Enemy);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(Final(result, 2).StatusEffects.Entries.OfType<ShieldStatus>(), Is.Empty);
            var changes = result.Events.OfType<CombatTraitStateEvent>().Where(e => e.Final.EntityId.Value == 2).ToArray();
            Assert.That(changes[0].Expected.StatusEffects.Entries.OfType<ShieldStatus>().Count(), Is.EqualTo(1));
            Assert.That(changes[0].Final.StatusEffects.Entries.OfType<ShieldStatus>(), Is.Empty);
        }

        [Test]
        public void WholeBatchUsesExistingOutcomePriority()
        {
            var input = CombatCommandTestData.Input();
            var state = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(state.BoardWidth, state.BoardHeight,
                state.Phase, state.Structures, state.ThornFields, state.Turn, state.Environment,
                state.EnvironmentProgress, state.WallBlocking, BattleObjective.DesignatedBoss(new EntityId(2)), state.Allocation));
            input = WithStatus(WithStatus(input, 2, Both(), 1), 3, Both(), 1);
            var result = Start(input, Owner.Enemy);
            Assert.That(Final(result, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, 3).Health.IsDead, Is.True);
            var final = CombatCommandComposition.Project(result);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(BattleOutcomeCalculator.Resolve(final.Characters,
                final.State.Structures, final.State.Objective).Outcome));
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Died)
                .Select(e => e.Final.EntityId.Value), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void DeadRecipientsSkipLaterStacks()
        {
            var input = WithStatus(CombatCommandTestData.Input(), 2, Both(), 3);
            var result = Start(input, Owner.Enemy);
            var damage = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId.Value == 2 &&
                (e.Kind == CombatEventKind.HealthChanged || e.Kind == CombatEventKind.Died)).ToArray();
            Assert.That(damage.Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(1));
            Assert.That(damage.Last().DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(new EntityId(1)));
            Assert.That(damage.Any(e => e.DamageOrigins.Any(o => o.CreditedCharacterId == new EntityId(10))), Is.False);
            Assert.That(Final(result, 2).StatusEffects.Entries, Is.Empty);
        }

        [Test]
        public void SnapshotBatchDoesNotRetickNewStates()
        {
            var original = Both();
            var input = WithStatus(WithStatus(CombatCommandTestData.Input(), 2, original), 3, original);
            var result = Start(input, Owner.Enemy);
            foreach (int id in new[] { 2, 3 })
            {
                Assert.That(Final(result, id).Health.CurrentHealth, Is.EqualTo(94));
                Assert.That(Final(result, id).StatusEffects.Entries.OfType<BleedingStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            }
            Assert.That(original.Entries.OfType<BleedingStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(3));
            var orderedDamage = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.HealthChanged).ToArray();
            Assert.That(orderedDamage.Select(e => e.Final.EntityId.Value), Is.EqualTo(new[] { 2, 2, 2, 3, 3, 3 }));
            Assert.That(orderedDamage.Select(e => e.Expected.Health.CurrentHealth - e.Final.Health.CurrentHealth), Is.EqualTo(new[] { 2, 1, 3, 2, 1, 3 }));
        }

        [Test]
        public void TickDeathsDoNotOpenNexusCapacity()
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(), living: 5,
                phase: CombatControllerPhase.StartingEnemy);
            input = WithStatus(input, 40, Both(), 1);
            var result = CombatTurnCalculator.BeginControllerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(Final(result, 40).Health.IsDead, Is.True);
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.FinalState.Reinforcements.SameValues(input.State.Reinforcements), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrDeadSource_RetainsAttributionWithoutSubstitution(bool dead)
        {
            var input = CombatCommandTestData.Input();
            int sourceId = dead ? 1 : 999;
            if (dead) input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? CombatCommandCalculator.Hurt(c, 100) : c).ToArray());
            input = WithStatus(input, 2, new StatusEffectSet(new AppliedStatus[] { new FireStatus(Origin(sourceId), 2) }), 1);
            var result = Start(input, Owner.Enemy);
            var death = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died);
            Assert.That(death.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(new EntityId(sourceId)));
            Assert.That(Final(result, 2).Health.IsDead, Is.True);
        }

        [Test]
        public void PeriodicDamage_UsesExposureAndShieldAtTheSharedBoundary()
        {
            var states = new StatusEffectSet(new AppliedStatus[]
            {
                new BleedingStatus(new[] { new BleedingStack(Origin(), 3) }, 3),
                new WeaknessExposureStatus(Origin(), 100, 2),
                new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Item, 99, 1, "shield"), 2, true, 0)
            });
            var result = Start(WithStatus(CombatCommandTestData.Input(), 2, states), Owner.Enemy);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(96));
            Assert.That(Final(result, 2).StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.Zero);
        }

        [Test]
        public void TerminalEnemyBatch_DoesNotReinforceAfterTheObjectiveDies()
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(), living: 1,
                phase: CombatControllerPhase.StartingEnemy);
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.Structures, s.ThornFields, s.Turn, s.Environment, s.EnvironmentProgress,
                s.WallBlocking, BattleObjective.DesignatedBoss(new EntityId(40)), s.Allocation, s.ItemRandomState,
                s.Rewards, s.TraitTurn, reinforcements: s.Reinforcements));
            var result = CombatTurnCalculator.BeginControllerTurn(WithStatus(input, 40, Both(), 1), Array.Empty<IMovementTileRule>());
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.FinalState.Reinforcements.SameValues(s.Reinforcements), Is.True);
        }

        [Test]
        public void Slow_OwnTurnReaction_DoesNotExpireEarly()
        {
            var input = EnemyControllerPhaseTests.Input(CombatCommandTestData.Input(), CombatControllerPhase.Player);
            input = WithStatus(input, 1, new StatusEffectSet(new AppliedStatus[] { new MovementRangeDecreaseStatus(Origin(), 3) }));
            var firstEnd = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(Final(firstEnd, 1).StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().Count(), Is.EqualTo(1));
            var enemyStart = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(firstEnd), Array.Empty<IMovementTileRule>());
            var enemyEnd = CombatTurnCalculator.EndEnemyPhase(CombatCommandComposition.Project(enemyStart), Array.Empty<IMovementTileRule>());
            var playerStart = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(enemyEnd), Array.Empty<IMovementTileRule>());
            Assert.That(Final(playerStart, 1).StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().Count(), Is.EqualTo(1));
            var secondEnd = CombatTurnCalculator.EndPlayerTurn(CombatCommandTestData.FreshCommand(
                CombatCommandComposition.Project(playerStart), CombatCommandKind.EndPlayerTurn), Array.Empty<IMovementTileRule>());
            Assert.That(secondEnd.Accepted, Is.True, secondEnd.Reason);
            Assert.That(Final(secondEnd, 1).StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>(), Is.Empty);
            Assert.That(secondEnd.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId.Value == 1 &&
                !e.Final.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().Any()), Is.True);
        }

        internal static CombatCommandInput WithStatus(CombatCommandInput input, int id, StatusEffectSet states, int? hp = null) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == id
                ? c.WithHealth(new HealthStateSnapshot(hp ?? c.Health.CurrentHealth, c.Health.MaxHealth)).WithStatusEffects(states) : c).ToArray());

        internal static CombatCommandTransition Start(CombatCommandInput input, Owner side) => CombatTurnCalculator.BeginControllerTurn(
            EnemyControllerPhaseTests.Input(input, side == Owner.Player ? CombatControllerPhase.StartingPlayer : CombatControllerPhase.StartingEnemy), Array.Empty<IMovementTileRule>());

        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);
    }
}
