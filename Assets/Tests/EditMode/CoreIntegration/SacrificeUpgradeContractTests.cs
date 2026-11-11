using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class SacrificeUpgradeContractTests
    {
        [TestCase(10, TestName = "Rank2ResolvesSurvivalAndTenShield")]
        [TestCase(20, TestName = "ShieldCanBeRetunedToTwenty")]
        [TestCase(0, TestName = "BooleanOnlyUpgradeIsNotNoOp")]
        public void Rank2ResolvesAuthoredFields(int shield)
        {
            using var fixture = new CoreAuthoringFixture();
            var definition = Definition(fixture, Rank(true, shield));
            Assert.That(definition.MaxRank, Is.EqualTo(2));
            AssertSettings(Resolve(definition, 1), false, 0);
            AssertSettings(Resolve(definition, 2), true, shield);
        }

        [Test]
        public void SplitUnlocksAtRanksTwoAndThree()
        {
            using var fixture = new CoreAuthoringFixture();
            var definition = Definition(fixture, Rank(true, 0), Rank(true, 10));
            Assert.That(definition.MaxRank, Is.EqualTo(3));
            AssertSettings(Resolve(definition, 2), true, 0);
            AssertSettings(Resolve(definition, 3), true, 10);
        }

        [Test]
        public void BooleanOnlyDefinitionChangeIsDetected()
        {
            using var fixture = new CoreAuthoringFixture();
            var disabled = Definition(fixture, Rank(false, 10));
            var enabled = Definition(fixture, Rank(true, 10));
            var withThirdRank = Definition(fixture, Rank(true, 10), Rank(true, 20));
            Assert.That(Snapshot(disabled).SameValues(Snapshot(enabled)), Is.False);
            Assert.That(Snapshot(enabled).SameValues(Snapshot(withThirdRank)), Is.False);
            Assert.That(Snapshot(enabled).SameValues(Snapshot(Definition(fixture, Rank(true, 10)))), Is.True);
        }


        [Test]
        public void RedirectPublishesShieldAfterHealthAndRecordsOnlyActualLoss()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new UpgradeBattleSource());
            var target = battle.Character(EntityKey.Unit_DynamiteHammer);
            var bearer = battle.Actor;
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(target, null, 1));
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(bearer, null, 5));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var attacker = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().EnemyStep().WithActor(attacker.EntityId)
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(target.Anchor.Value), default, null, null);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == bearer.EntityId).TargetHealthAfter, Is.EqualTo(1));
            before.AssertUnchanged(battle.Buffers);

            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var published), Is.True);
            var final = battle.Actor;
            Assert.That((final.Health.CurrentHealth, final.StageRecord.DamageTaken), Is.EqualTo((1, 4)));
            Assert.That(battle.Character(target.CharacterKey).Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(battle.Character(attacker.CharacterKey).StageRecord.Kills, Is.Zero);
            var shield = final.StatusEffects.Entries.OfType<ShieldStatus>().Single(s =>
                s.Origin.Value.Kind == EffectOwnerKind.Trait && s.Origin.Value.DefinitionKey == (int)TraitKey.Sacrifice);
            Assert.That((shield.Origin.Value.OwnerInstanceId, shield.RemainingBudget), Is.EqualTo((bearer.EntityId.Value, 10)));
            var attack = published.Attack.Value;
            Assert.That(attack.Hits.ToArray().Single(h => h.TargetId == bearer.EntityId).DamageDealt, Is.EqualTo(4));
            var redirect = attack.TraitActivations.ToArray().Single(t => t.Interception != null);
            Assert.That((redirect.Magnitude, redirect.Interception.ProtectedId), Is.EqualTo((4, target.EntityId)));
            var events = published.Events.ToList();
            int health = events.FindIndex(e => e is CombatCharacterEvent c && c.Final.EntityId == bearer.EntityId &&
                c.Final.Health.CurrentHealth == 1);
            int state = events.FindIndex(e => e is CombatTraitStateEvent t && t.Final.EntityId == bearer.EntityId &&
                t.Final.StatusEffects.Entries.OfType<ShieldStatus>().Any(s => s.SameValues(shield)));
            int grant = events.FindIndex(e => e is CombatEffectsEvent effects && effects.Traits.Any(t =>
                t.TraitKey == TraitKey.Sacrifice && t.TraitActivationKind == TraitActivationKind.ShieldGranted && t.Magnitude == 10));
            Assert.That(health, Is.GreaterThanOrEqualTo(0));
            Assert.That(state, Is.GreaterThan(health));
            Assert.That(grant, Is.GreaterThan(state));
        }

        private sealed class UpgradeBattleSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                    s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.Key == EntityKey.Unit_Demon ? 8 : s.AttackRange,
                    s.CategoryKey, s.Key == EntityKey.Unit_Demon ? 20 : s.AttackPower,
                    s.Key == EntityKey.Unit_Cowboy ? 0 : s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new SacrificeTraitSpec(2, 1, true, 10) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                var profiles = basis.AttackProfiles.Select(p =>
                    new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures,
                    basis.Categories, profiles);
            }
        }

        private static SacrificeRankAuthoring Rank(bool survival, int shield)
        {
            var rank = new SacrificeRankAuthoring();
            CoreAuthoringFixture.Set(rank, "preventsLethalSacrificeDamage", survival);
            CoreAuthoringFixture.Set(rank, "shieldOnActivation", shield);
            return rank;
        }

        private static TraitGrowthDefinition Definition(CoreAuthoringFixture fixture, SacrificeRankAuthoring rank2,
            SacrificeRankAuthoring rank3 = null)
        {
            var so = fixture.Create<TraitGrowthDefinitionDataSO>();
            var normal = new SacrificeTraitAuthoring();
            CoreAuthoringFixture.Set(normal, "radiusCells", 3);
            CoreAuthoringFixture.Set(normal, "maxTriggersPerEnemyTurn", 2);
            CoreAuthoringFixture.Set(so, "group", TraitGrowthGroup.Common);
            CoreAuthoringFixture.Set(so, "normal", normal);
            CoreAuthoringFixture.Set(so, "rank2", rank2);
            CoreAuthoringFixture.Set(so, "rank3", rank3);
            var row = (TraitGrowthDefinitionData)so.GetData();
            return new TraitGrowthDefinition(row.Group, TraitSpecAdapter.Read(row.Normal), null, row.Name, row.Description,
                row.EnhancedName, row.EnhancedDescription, row.Rank2, row.Rank3);
        }

        private static UnitSpec Unit(TraitSpec spec) =>
            new(EntityKey.Unit_Cowboy, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 20, MoveMethod.LiftAndPlace, 2,
                CategoryKey.Rider, 5, 0, 1, new[] { spec }, UnitOwnerEligibility.PlayerOnly);

        private static TraitGrowthCatalog Catalog(TraitGrowthDefinition definition) =>
            new(TraitGrowthCatalog.Disabled.Categories, new[] { definition });

        private static MainSpecializationDefinitionSnapshot Snapshot(TraitGrowthDefinition definition)
        {
            var unit = Unit(definition.Normal);
            var profile = new CombatAttackProfile(unit,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            return new MainSpecializationDefinitionSnapshot(profile, Catalog(definition), new CategoryConfigData(3, 4, 3, 2, 0),
                new[] { unit });
        }

        private static SacrificeTraitSpec Resolve(TraitGrowthDefinition definition, int rank)
        {
            var unit = Unit(definition.Normal);
            var growth = OwnedTraitGrowth.Empty;
            for (int i = 1; i < rank; i++)
                growth = growth.WithPurchase(definition.Key);
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), unit.Key, CharacterOwnershipRole.Sub, growth: growth);
            return OwnedUnitSpecResolver.Resolve(unit, owned, Catalog(definition)).Traits.ToArray().OfType<SacrificeTraitSpec>().Single();
        }

        private static void AssertSettings(SacrificeTraitSpec spec, bool survival, int shield)
        {
            Assert.That((spec.RadiusCells, spec.MaxTriggersPerEnemyTurn, spec.PreventsLethalSacrificeDamage, spec.ShieldOnActivation),
                Is.EqualTo((3, 2, survival, shield)));
        }
    }
}
