using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PeriodicStatusPublicationTests
    {
        [Test]
        public void LethalTickPublishesDeathRewardsSoulsAndStatsOnce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new PeriodicRewardSource());
            var b = battle.Buffers;
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Actor, null, 1));
            var enemy = battle.Character(EntityKey.Unit_Demon);
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(enemy, null, 1));
            enemy = battle.Character(EntityKey.Unit_Demon);
            Install(battle, enemy, new StatusEffectSet(new AppliedStatus[] { new FireStatus(Origin(battle.Actor), 2) }));
            int kills = b.RunProgressReadSink.EnemiesDefeated;
            int gold = b.CurrencyReadSink.Amount;
            var priorActor = battle.Actor;
            battle.Submit(battle.EndPlayerTurn());
            var services = battle.Publication();
            var input = services.Assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.StartControllerTurn,
                EntityId.Empty, default, processingKind: CombatProcessingKind.Advance));
            var transition = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(services.Publisher.Publish(transition), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.IsDead, Is.True);
            Assert.That(battle.Actor.Health.CurrentHealth, Is.GreaterThan(priorActor.Health.CurrentHealth));
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(battle.Actor.StageRecord.DamageDealt, Is.EqualTo(priorActor.StageRecord.DamageDealt + 1));
            Assert.That(battle.Actor.StageRecord.Kills, Is.EqualTo(priorActor.StageRecord.Kills + 1));
            Assert.That(b.RunProgressReadSink.EnemiesDefeated, Is.EqualTo(kills + 1));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(gold + 2));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(1));
            // 기존 영혼 정산은 현재 생성이 보류되어 있으므로 같은 필드를 유지해야 합니다.
            Assert.That(transition.Field.Final.SameValues(transition.Field.Expected), Is.True);
            Assert.That(result.Events.OfType<CombatSoulEvent>(), Is.Empty);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died)
                .DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(battle.Actor.EntityId));
            var once = new CoreIntegrationSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            once.AssertUnchanged(b);
        }

        [Test]
        public void TerminalBatchDoesNotOpenInputOrReinforce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)), startPlayerTurn: false);
            var b = battle.Buffers;
            var main = battle.Main;
            Assert.That(b.CharacterReadSink.TryGetMainState(main.OwnedId.Value, out var persistent), Is.True);
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(main, persistent, 1));
            Install(battle, battle.Main, new StatusEffectSet(new AppliedStatus[] { new FireStatus(Origin(battle.Character(EntityKey.Unit_Demon)), 2) }));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(b.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatReinforcementEvent>(), Is.Empty);
            Assert.That(b.CombatStateReadSink.Completion, Is.Not.Null);
            var once = new CoreIntegrationSnapshot(b);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            once.AssertUnchanged(b);
        }

        [Test]
        public void PublishedSnapshotsRemainImmutable()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new TraitKillRewardPublicationTests.KillSource());
            var target = battle.Character(EntityKey.Unit_Demon);
            var origin = Origin(battle.Actor);
            var states = new StatusEffectSet(new AppliedStatus[]
            {
                new FireStatus(origin, 2),
                new BleedingStatus(new[] { new BleedingStack(origin, 1), new BleedingStack(Origin(battle.Main), 3) }, 3),
                new MovementRangeDecreaseStatus(origin, 2)
            });
            Install(battle, target, states);
            var prior = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == target.EntityId);
            Assert.That(prior.StatusEffectSnapshots.Select(s => s.Key), Is.EquivalentTo(new[] {
                StatusEffectKey.Fire, StatusEffectKey.Bleeding, StatusEffectKey.MovementRangeDecrease }));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var after = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == target.EntityId);
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(target.Health.CurrentHealth - 6));
            Assert.That(after.StatusEffects.Entries.OfType<FireStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(1));
            Assert.That(after.StatusEffects.Entries.OfType<BleedingStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(after.ToFacts().StatusEffects, Is.SameAs(after.StatusEffects));
            Assert.That(prior.StatusEffects.SameValues(states), Is.True);
            Assert.That(prior.StatusEffectSnapshots.Single(s => s.Key == StatusEffectKey.Fire).Sources[0].Status,
                Is.SameAs(states.Entries[0]));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatTraitStateEvent>().Last(e => e.Final.EntityId == target.EntityId)
                .Final.StatusEffects.SameValues(after.StatusEffects), Is.True);
        }

        internal static EffectOriginKey Origin(CharacterCombatFacts source) => new(EffectOwnerKind.Trait, source.EntityId.Value, 480, "bleeding");

        internal static void Install(ConfiguredMovementBattle battle, CharacterCombatFacts target, StatusEffectSet states) =>
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(target, target.WithStatusEffects(states)) },
                Array.Empty<SoulCountChange>()));

        private sealed class PeriodicRewardSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new TraitKillRewardPublicationTests.KillSource().Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => s.Key != EntityKey.Unit_Demon ? s : new UnitSpec(s.Key, s.Role,
                    s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod,
                    s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, new UnitKillRewardSpec(2, 0))).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures,
                    basis.Categories, basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key),
                        p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
