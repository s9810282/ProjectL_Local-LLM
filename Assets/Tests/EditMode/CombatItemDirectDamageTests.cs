using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
namespace ProjectL.Tests.Core
{
    public sealed class CombatItemDirectDamageTests
    {
        internal static readonly ItemDefinition[] Definitions = {
            new ItemDefinition(1, new WhiskeyItemSpec(50, 10)), new ItemDefinition(2, new RoketItemSpec(20, 50)) };
        internal static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(30, 85, true)]
        [TestCase(19, 81, false)]
        public void Roket_UsesFinalDamageAndReportsMitigation(int damage, int hp, bool consumed)
        {
            var input = Input(damage, 100, 2);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(Final(result, 2).Items.EquippedItemKey, Is.EqualTo(consumed ? null : (int?)2));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(100 - hp));
            Assert.That(result.Events.OfType<CombatItemEvent>().Count(e => e.Consumed), Is.EqualTo(consumed ? 1 : 0));
            if (consumed) Assert.That(result.Events.OfType<CombatItemEvent>().Single().AppliedMagnitude, Is.EqualTo(15));
        }

        [Test]
        public void HeldProtection_PrecedesRoketAndDoesNotSpendItem()
        {
            var input = Input(30, 100, 2);
            input = Items(CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray()));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Final(result, 2).Items.EquippedItemKey, Is.EqualTo(2));
            Assert.That(result.Events.OfType<CombatItemEvent>(), Is.Empty);
        }

        [Test]
        public void TwoHits_ObserveImmediateWhiskeyAndOneConsumption()
        {
            var input = Input(15, 60, 1, mainTarget: true);
            var (plan, world, ledger) = Prepare(input, duplicate: true);
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
            var final = world.Characters[new EntityId(10)];
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(40));
            Assert.That(final.Items.EquippedItemKey, Is.Null);
            AssertSequence(world.Events, 10, new[] { 45, 55, 40 });
            Assert.That(world.Events.OfType<CombatItemEvent>().Count(e => e.Consumed), Is.EqualTo(1));
            var completed = world.CreateArrivalProjection(plan).Complete();
            Assert.That(completed.Characters.Main.Final.CurrentHealth, Is.EqualTo(40));
        }

        [Test]
        public void Recovery_IsSeparateFromAttackDamageEvenWhenHealthIncreases()
        {
            var result = CombatCommandCalculator.Calculate(Input(1, 40, 1), NoRules);
            var hit = result.Attack.Value.Hits.Span[0];
            Assert.That(hit.TargetHealthBefore, Is.EqualTo(40));
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(49));
            Assert.That(hit.DamageDealt, Is.EqualTo(1));
            AssertSequence(result.Events, 2, new[] { 39, 49 });
            Assert.That(result.Events.OfType<CombatItemEvent>().Single().AppliedMagnitude, Is.EqualTo(10));
            Assert.That(result.InventoryChange, Is.Null);
            Assert.That(result.Input.ItemInventory.GetCount(1), Is.EqualTo(7));
        }

        [TestCase(100, 100, 100, 100, 100, 1)]
        [TestCase(9, 4, 50, 10, 3, 0)]
        public void Recovery_ReportsCappedActualAmountAndConsumesZeroHeal(int max, int hp,
            int threshold, int percent, int finalHp, int recovery)
        {
            var input = Input(1, hp, 1);
            var characters = input.Characters.Select(c => c.EntityId.Value == 2
                ? CharacterSettlementCalculator.Copy(c, new HealthStateSnapshot(hp, max), c.Stats) : c).ToArray();
            input = Items(CombatCommandTestData.With(input, characters), new[] {
                new ItemDefinition(1, new WhiskeyItemSpec(threshold, percent)), Definitions[1] });
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(Final(result, 2).Items.EquippedItemKey, Is.Null);
            Assert.That(result.Events.OfType<CombatItemEvent>().Single().AppliedMagnitude, Is.EqualTo(recovery));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(1));
        }

        [TestCase(1, 40, 40, 0, false)]
        [TestCase(2, 40, 60, 10, true)]
        [TestCase(2, 5, 100, 0, true)]
        public void LethalHits_SettleMitigationBeforeDeathAndDiscardUnusedSlot(int key, int hp, int damage, int finalHp, bool activates)
        {
            var result = CombatCommandCalculator.Calculate(Input(damage, hp, key), NoRules);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(Final(result, 2).Items.EquippedItemKey, Is.Null);
            Assert.That(result.Events.OfType<CombatItemEvent>().Count(e => e.Activated), Is.EqualTo(activates ? 1 : 0));
            if (!activates) Assert.That(result.Events.OfType<CombatItemEvent>().Single().Reason, Is.EqualTo(ItemEventReason.Death));
        }

        internal static CombatCommandInput Input(int damage, int hp, int? item, bool mainTarget = false)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var baseline = CombatCommandTestData.Input(method);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3),
                mainTarget ? Owner.Enemy : Owner.Player, damage);
            var characters = baseline.Characters.Select(c => c.EntityId.Value == 1 ? actor : c).ToArray();
            if (mainTarget)
                characters = new[] { actor, CombatCommandTestData.Character(10, EntityKey.Unit_MainCharacter,
                    new CellCoord(4, 3), Owner.Player, main: true) };
            int target = mainTarget ? 10 : 2;
            characters = characters.Select(c => c.EntityId.Value == target ? Equip(c, hp, item) : c).ToArray();
            var profiles = baseline.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, damage), method) : p).ToArray();
            var main = mainTarget ? baseline.Main.WithCurrentHealth(hp) : baseline.Main;
            return Items(CombatCommandTestData.With(baseline, characters, profiles: profiles, main: main));
        }

        internal static CharacterCombatFacts Equip(CharacterCombatFacts c, int hp, int? key) =>
            ItemCharacterChanges.WithItems(CharacterSettlementCalculator.Copy(c,
                new HealthStateSnapshot(hp, c.Health.MaxHealth), c.Stats),
                new CharacterItemState(key, Array.Empty<ItemTemporaryEffect>()));

        internal static CombatCommandInput Items(CombatCommandInput i, IReadOnlyList<ItemDefinition> definitions = null) => new CombatCommandInput(i.Number, i.Kind, i.ActorId,
            i.Aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
            i.TargetCell, i.Rotation, i.Battle, i.ProcessingKind, i.Path, i.MovementContext, i.BattleInstanceId,
            i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents, i.ItemKey,
            new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 7, [2] = 8 }), definitions ?? Definitions,
                expectedBattleInstanceId: i.ExpectedBattleInstanceId, expectedControllerTurnSequence: i.ExpectedControllerTurnSequence);

        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            result.Characters.Characters.FirstOrDefault(c => c.Final.EntityId.Value == id)?.Final
            ?? result.Input.Characters.Single(c => c.EntityId.Value == id);

        internal static void AssertSequence(IEnumerable<CombatEvent> events, int id, int[] health)
        {
            var changes = events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId.Value == id &&
                e.Expected.Health.CurrentHealth != e.Final.Health.CurrentHealth).ToArray();
            Assert.That(changes.Select(e => e.Final.Health.CurrentHealth), Is.EqualTo(health));
            foreach (var e in changes)
                Assert.That(e.DamageOrigins.Count, Is.EqualTo(e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth ? 1 : 0));
        }

        internal static (CommandAttackPlan Plan, CommandAttackProjection World, CommandAttackLedger Ledger) Prepare(
            CombatCommandInput input, bool duplicate = false)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment;
            var walls = WallGeometryBuilder.Build(8, 8, environment, input.EnvironmentBindings, input.State.WallBlocking);
            var selection = CombatAttackQueries.Selection(input, actor, profile, environment: environment, walls: walls);
            Assert.That(selection.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
            var delivery = CombatAttackDelivery.Resolve(input, actor, profile, selection.Selection, damage, walls, AttackElevation.From(environment, input.EnvironmentBindings));
            if (duplicate) delivery = new ResolvedAttackDelivery(new[] { delivery.Hits[0], delivery.Hits[0] }, delivery.AffectedCells);
            var plan = new CommandAttackPlan(input,NoRules,profile,actor,actor,environment,walls,                selection.Selection,delivery,engine,bindings,                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),damage,Rotation4.R0,false);
            return (plan, new CommandAttackProjection(input, environment, selection.BlockedContacts), new CommandAttackLedger());
        }
    }
}
