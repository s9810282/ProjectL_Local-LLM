using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class NewTraitCombinedIntegrationTests
    {
        private static readonly CellCoord Primary = new(0, 3);
        private static readonly CellCoord Splash = new(1, 3);
        private static readonly CellCoord Headquarters = new(3, 2);

        [Test]
        public void MixedBleedingLavaRampageRoundTrip()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new CombinedSource());
            var buffers = battle.Buffers;
            var primary = buffers.CharacterReadSink.Characters.Single(c => c.ToFacts().Anchor == Primary);
            var splash = buffers.CharacterReadSink.Characters.Single(c => c.ToFacts().Anchor == Splash);
            buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Actor, null, 20));
            // 메인의 지속 공격력 5와 광란 중 카우보이 공격력 3을 뺀 뒤 지속 피해 6이 남습니다.
            buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(primary.ToFacts(), null, 14));
            int priorKills = buffers.RunProgressReadSink.EnemiesDefeated;
            int priorGold = buffers.CurrencyReadSink.Amount;

            battle.Submit(battle.Attack(battle.Main, Primary));
            battle.Submit(battle.Attack(battle.Actor, Primary));

            var applied = buffers.CharacterReadSink.Characters.Single(c => c.EntityId == primary.EntityId);
            var bleeding = applied.StatusEffectSnapshots.OfType<BleedingEffectSnapshot>().Single();
            var fire = applied.StatusEffectSnapshots.OfType<FireEffectSnapshot>().Single();
            var slowed = buffers.CharacterReadSink.Characters.Single(c => c.EntityId == splash.EntityId);
            var slow = slowed.StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>().Single();
            Assert.That(bleeding.Stacks.Select(s => s.Damage), Is.EqualTo(new[] { 3, 1 }));
            Assert.That(bleeding.Stacks.Select(s => s.Origin.OwnerInstanceId), Is.EqualTo(new[] { battle.Main.EntityId.Value, battle.Actor.EntityId.Value }));
            Assert.That(applied.ToFacts().Health.CurrentHealth, Is.EqualTo(6));
            Assert.That((fire.Damage, fire.RemainingAffectedSideStarts, bleeding.RemainingAffectedSideStarts),
                Is.EqualTo((2, 2, 3)));
            Assert.That((slow.Magnitude, slow.ExpiresAtCompletedControllerTurnCount), Is.EqualTo((1, 2L)));

            battle.Submit(battle.EndPlayerTurn());
            Assert.That(buffers.CharacterReadSink.Characters.Single(c => c.EntityId == splash.EntityId)
                .StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>().Count(), Is.EqualTo(1));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var tick), Is.True);
            var death = tick.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died);
            Assert.That(death.Final.EntityId, Is.EqualTo(primary.EntityId));
            Assert.That(death.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(battle.Actor.EntityId));
            var reward = tick.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.Rampage && t.TraitActivationKind == TraitActivationKind.AttackGranted);
            Assert.That(reward.Magnitude, Is.EqualTo(8));
            Assert.That(buffers.RunProgressReadSink.EnemiesDefeated, Is.EqualTo(priorKills + 1));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(priorGold + 2));
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(40));
            Assert.That(battle.Actor.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().Single().KillAttackBonus, Is.Zero);
            Assert.That(StatusEffectReadModel.Capture(battle.Actor).OfType<RampageEffectSnapshot>(), Is.Empty);
            Assert.That(buffers.CharacterReadSink.Characters.Single(c => c.EntityId == splash.EntityId)
                .StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>().Count(), Is.EqualTo(1));

            battle.AdvanceToPlayerOrDay();
            var afterEnemy = buffers.CharacterReadSink.Characters.Single(c => c.EntityId == splash.EntityId);
            Assert.That(afterEnemy.StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>(), Is.Empty);
            Assert.That(afterEnemy.StatusEffectSnapshots.OfType<FireEffectSnapshot>().Single().RemainingAffectedSideStarts, Is.EqualTo(1));

            battle.Submit(battle.Attack(battle.Actor, Headquarters));
            Assert.That(buffers.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var completion), Is.True);
            Assert.That(completion.Events.OfType<CombatOutcomeEvent>().Single().Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(applied.ToFacts().Health.CurrentHealth, Is.EqualTo(6));
            Assert.That(applied.StatusEffectSnapshots.OfType<BleedingEffectSnapshot>().Single(), Is.SameAs(bleeding));
            Assert.That(applied.StatusEffectSnapshots.OfType<FireEffectSnapshot>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            Assert.That(slowed.StatusEffectSnapshots.OfType<MovementRangeDecreaseEffectSnapshot>().Single(), Is.SameAs(slow));
        }

        private sealed class CombinedSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(Primary, terminalHeadquarters: true)
                    .Read(database, chapterKey, stageKey);
                var stage = new StageConfig(8, 6, 8, 2, 3, 42, new[]
                {
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, Primary, Rotation4.R180),
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, Splash, Rotation4.R180)
                }, Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), new[]
                {
                    new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, Headquarters, Rotation4.R180)
                });
                var units = basis.Units.Select(unit => unit.Key switch
                {
                    EntityKey.Unit_Akimbo => Configure(unit, 5, new BleedingTraitSpec(3)),
                    EntityKey.Unit_Cowboy => Configure(unit, 1, new RampageTraitSpec(2, 0, 1), new LavaCannonTraitSpec(),
                        new BleedingTraitSpec(1), new CallOfBloodTraitSpec(20)),
                    EntityKey.Unit_Demon => Configure(unit, 8),
                    _ => unit
                }).ToArray();
                var profiles = basis.AttackProfiles.Select(profile =>
                {
                    var unit = units.Single(u => u.Key == profile.Definition.Key);
                    var method = unit.Key switch
                    {
                        EntityKey.Unit_Akimbo => new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0),
                        EntityKey.Unit_Cowboy => new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 100),
                        _ => profile.Method
                    };
                    return new CombatAttackProfile(unit, method);
                }).ToArray();
                return new BattleInitialization(stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures,
                    basis.Categories, profiles, clearRewards: basis.ClearRewards);
            }

            private static UnitSpec Configure(UnitSpec unit, int attack, params TraitSpec[] traits) => new(
                unit.Key, unit.Role, unit.FootprintWidth, unit.FootprintHeight, unit.FootprintLocalCells.ToArray(),
                100, unit.MoveMethod, 4, unit.CategoryKey, attack, 0, unit.Priority, traits, unit.OwnerEligibility,
                unit.Occlusion, unit.Behavior,
                unit.Key == EntityKey.Unit_Demon ? new UnitKillRewardSpec(2, 0) : unit.KillReward);
        }
    }
}
