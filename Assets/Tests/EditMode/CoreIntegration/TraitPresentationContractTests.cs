using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitPresentationContractTests
    {
        [Test]
        public void EffectiveDefense_UsesLivingNeighborsAndRetainsBaseline()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new CohesionSource());
            var before = Data(battle);
            Assert.That(before.EffectivePhysicalDefense, Is.EqualTo(before.Stats.PhysicalDefense + 5));
            var capturedDefense = before.StatusEffectSnapshots.OfType<DefenseIncreaseEffectSnapshot>().Single();
            Assert.That(capturedDefense.Amount, Is.EqualTo(5));
            Assert.That(before.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.DefenseIncrease), Is.False);
            int baseline = before.Stats.PhysicalDefense;
            battle.Move(battle.Actor, new CellCoord(0, 2));
            var after = Data(battle);
            Assert.That(after.Stats.PhysicalDefense, Is.EqualTo(baseline));
            Assert.That(after.EffectivePhysicalDefense, Is.EqualTo(baseline));
            Assert.That(after.StatusEffectSnapshots.Any(s => s.Key == StatusEffectKey.DefenseIncrease), Is.False);
            Assert.That(capturedDefense.Amount, Is.EqualTo(5));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var change = result.Events.OfType<CombatTraitStateEvent>().Single(e => e.Final.EntityId == battle.Actor.EntityId &&
                Bonus(e.Expected) != Bonus(e.Final));
            Assert.That(Bonus(change.Expected), Is.EqualTo(5));
            Assert.That(Bonus(change.Final), Is.Zero);
            Assert.That(Stat(change, "ExpectedStats", "PhysicalDefense"), Is.EqualTo(baseline + 5));
            Assert.That(Stat(change, "FinalStats", "PhysicalDefense"), Is.EqualTo(baseline));
            int activation = result.Events.ToList().FindIndex(e => e is CombatEffectsEvent f && f.Traits.Any(a =>
                a.TraitKey == TraitKey.Cohesion && a.TraitActivationKind == TraitActivationKind.DefenseChanged));
            Assert.That(activation, Is.GreaterThanOrEqualTo(0));
            Assert.That(activation, Is.LessThan(result.Events.ToList().IndexOf(change)));
        }
        [Test]
        public void PreparationAndQueries_ExposeDerivedValuesWithoutConsumerCalculation()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new CohesionSource(), startPlayerTurn: false);
            var data = Data(battle);
            Assert.That(data.EffectivePhysicalDefense, Is.EqualTo(data.Stats.PhysicalDefense + 5));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(Data(battle).EffectiveAttackPower, Is.EqualTo(data.EffectiveAttackPower));
                Assert.That(Data(battle).EffectivePhysicalDefense, Is.EqualTo(data.EffectivePhysicalDefense));
                Assert.That(Data(battle).StatusEffectSnapshots, Is.SameAs(data.StatusEffectSnapshots));
            }
            before.AssertUnchanged(battle.Buffers);
        }
        private static ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterData Data(ConfiguredMovementBattle battle) =>
            battle.Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Cowboy);
        private static int Bonus(CharacterCombatFacts facts)
        {
            var property = typeof(CharacterTraitEffects).GetProperty("CohesionDefenseBonus");
            Assert.That(property, Is.Not.Null);
            return (int)property.GetValue(facts.TraitEffects);
        }
        private static int Stat(CombatTraitStateEvent value, string property, string field)
        {
            var info = typeof(CombatTraitStateEvent).GetProperty(property);
            Assert.That(info, Is.Not.Null);
            var stats = info.GetValue(value);
            Assert.That(stats, Is.Not.Null);
            return (int)stats.GetType().GetProperty(field).GetValue(stats);
        }
        internal sealed class CohesionSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new CohesionTraitSpec(5, 10, 1, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
