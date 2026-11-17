using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillEffectIntegrationTests
    {
        [TestCase(CombatDamageKind.OrdinaryAttack, true, true)]
        [TestCase(CombatDamageKind.ExtraAttack, true, true)]
        [TestCase(CombatDamageKind.ReactionAttack, true, false)]
        [TestCase(CombatDamageKind.PlayerSkillAttack, true, true)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false, false)]
        [TestCase(CombatDamageKind.ReflectedDamage, false, false)]
        [TestCase(CombatDamageKind.DamageOverTime, false, false)]
        public void ContactAndDust_KeepTheirOwnDamageFamilyConditions(CombatDamageKind kind, bool common, bool mark)
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var enemy = input.Characters.Single(c => c.EntityId.Value == 2);
            var origin = TraitEffectOrigin.Create(actor.EntityId, TraitKey.Bleeding);
            var bleeding = new BleedingTraitRule().Apply(new BleedingTraitDefinition(2), actor, enemy, kind, origin);
            Assert.That(bleeding.Entries.OfType<BleedingStatus>().Any(), Is.EqualTo(common));
            Assert.That(HuntersMarkTraitRule.CanApplyMark(actor, enemy, kind), Is.EqualTo(mark));
            Assert.That(new DustScreenTraitRule().Reduces(PlayerSkillAttackTestData.Dust(enemy), enemy, kind), Is.EqualTo(common));

            var lavaInput = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LavaCannonTraitSpec());
            var lavaActor = lavaInput.Characters.Single(c => c.EntityId == lavaInput.ActorId);
            var characters = lavaInput.Characters.ToDictionary(c => c.EntityId);
            LavaCannonApplication.ApplyCaptured(lavaActor,
                CombatCommandEffects.Capture(lavaActor, CombatCommandCalculator.RequiredProfile(lavaInput, lavaActor)),
                kind, lavaInput.State.Turn, characters, new List<CombatEvent>(),
                new CombatDamageOrigin(lavaActor.Anchor, lavaActor.EntityId, 1), new[] { enemy.EntityId });
            Assert.That(characters[enemy.EntityId].StatusEffects.Entries.OfType<FireStatus>().Any(), Is.EqualTo(common));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, true)]
        [TestCase(CombatDamageKind.ExtraAttack, false)]
        [TestCase(CombatDamageKind.ReactionAttack, false)]
        [TestCase(CombatDamageKind.PlayerSkillAttack, true)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false)]
        [TestCase(CombatDamageKind.ReflectedDamage, false)]
        [TestCase(CombatDamageKind.DamageOverTime, false)]
        public void IncomingItems_AddSkillsWithoutBroadeningExtraOrReactionHits(CombatDamageKind kind, bool eligible)
        {
            var input = CombatItemDirectDamageTests.Input(30, 100, 2);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var holder = input.Characters.Single(c => c.EntityId.Value == 2);
            var roket = new RoketItemRule().Evaluate(new ItemDefinition(2, new RoketItemSpec(20, 50)),
                new ItemDamageContext(kind, actor, holder, 30));
            Assert.That(roket.Activated, Is.EqualTo(eligible));
            if (eligible) Assert.That(roket.RemainingDamage, Is.EqualTo(15));

            var lantern = new LanternItemRule().Evaluate(new ItemDefinition(2, new LanternItemSpec(10)),
                new ItemAfterDamageContext(new ItemDamageContext(kind, actor, holder, 30),
                    holder.WithHealth(new HealthStateSnapshot(70, 100)), 30));
            Assert.That(lantern.Activated, Is.EqualTo(eligible));

            var whiskeyHolder = CombatItemDirectDamageTests.Equip(holder, 60, 1);
            var whiskey = new WhiskeyItemRule().Evaluate(new ItemDefinition(1, new WhiskeyItemSpec(50, 10)),
                new ItemAfterDamageContext(new ItemDamageContext(kind, actor, whiskeyHolder, 30),
                    whiskeyHolder.WithHealth(new HealthStateSnapshot(30, 100)), 30));
            Assert.That(whiskey.Activated, Is.True);
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, true)]
        [TestCase(CombatDamageKind.ExtraAttack, false)]
        [TestCase(CombatDamageKind.ReactionAttack, false)]
        [TestCase(CombatDamageKind.PlayerSkillAttack, true)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false)]
        [TestCase(CombatDamageKind.ReflectedDamage, false)]
        [TestCase(CombatDamageKind.DamageOverTime, false)]
        public void Reflection_UsesExistingHostilityAndOneScopeLimit(CombatDamageKind kind, bool eligible)
        {
            var input = ThornHeartTestData.Input(splash: true, hp: 20);
            var work = PlayerSkillAttackTestData.Prepare(input, kind, targets: new[] { 2, 3 });
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(eligible ? 17 : 20));
            Assert.That(work.World.Events.OfType<CombatArtifactEvent>().Count(), Is.EqualTo(eligible ? 1 : 0));
        }

        [Test]
        public void SkillReflection_IdentifiesTheActualTriggeringOccurrenceGroup()
        {
            var work = PlayerSkillAttackTestData.Prepare(ThornHeartTestData.Input(hp: 20),
                CombatDamageKind.PlayerSkillAttack, attackGroup: 5);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Events.OfType<CombatArtifactEvent>().Single().Reflection.TriggeringAttackGroup, Is.EqualTo(5));
        }

        [Test]
        public void SkillDustMitigation_IsAppliedOnceAndRecordsTheActualUse()
        {
            var input = CombatItemDirectDamageTests.Input(40, 100, null);
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var environment = new MovementEnvironmentFacts(new[] { PlayerSkillAttackTestData.Dust(target) });
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Characters[target.EntityId].Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(work.World.Damage.MovementEffectUses.Any(use => use.DamageKind == CombatDamageKind.PlayerSkillAttack), Is.True);
            Assert.That(input.Characters.Single(c => c.EntityId == target.EntityId).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, true, true)]
        [TestCase(CombatDamageKind.ExtraAttack, true, true)]
        [TestCase(CombatDamageKind.ReactionAttack, true, false)]
        [TestCase(CombatDamageKind.PlayerSkillAttack, true, true)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false, false)]
        [TestCase(CombatDamageKind.ReflectedDamage, false, false)]
        [TestCase(CombatDamageKind.DamageOverTime, false, false)]
        public void Sacrifice_UsesRecipientDefensesAndPreservesDamageFamilyConditions(CombatDamageKind kind, bool protects, bool mark)
        {
            var input = TraitCombatFixtures.Equip(SacrificeTraitTests.Input(), EntityKey.Unit_Akimbo, new BleedingTraitSpec(2));
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId ? c.With(stats: c.Stats.With(traitStatuses:
                    c.Stats.TraitStatuses.ToArray().Append(new HuntersMarkTraitStatusSnapshot(30, 2)).ToArray())) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo ? new CombatAttackProfile(
                    CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new BleedingTraitSpec(2),
                        new HuntersMarkTraitSpec(30, 2)), p.Method) : p).ToArray());
            var work = PlayerSkillAttackTestData.Prepare(input, kind);
            var delivered = new HashSet<EntityId>();
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, delivered);
            var protectedUnit = work.World.Characters[new EntityId(2)];
            var guardian = work.World.Characters[new EntityId(5)];
            Assert.That(protectedUnit.Health.CurrentHealth, Is.EqualTo(protects ? 5 : 0));
            Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(protects ? 80 : 100));
            Assert.That(delivered, Is.EquivalentTo(new[] { protects ? guardian.EntityId : protectedUnit.EntityId }));
            Assert.That(guardian.StatusEffects.Entries.OfType<BleedingStatus>().Any(), Is.EqualTo(protects));
            Assert.That(guardian.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Any(), Is.EqualTo(mark));
            Assert.That(protectedUnit.StatusEffects.Entries, Is.Empty);
            if (protects) Assert.That(work.Ledger.Contacts(guardian.EntityId).Single().OriginalTargetId, Is.EqualTo(protectedUnit.EntityId));
        }

        [Test]
        public void SkillCommonStages_DoNotSettleActionOrRunRopeFocusOrSweepCompletion()
        {
            var input = CombatItemDirectDamageTests.Input(20, 100, null);
            input = CombatItemDirectDamageTests.Items(ThornHeartTestData.Replace(input, 1,
                c => CombatItemDirectDamageTests.Equip(c, 100, 3)), new[] { new ItemDefinition(3, new RopeItemSpec(10)) });
            var profile = input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Akimbo);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId ? c.With(stats: c.Stats.With(traitStatuses:
                    new TraitStatusSnapshot[] { new FocusTraitStatusSnapshot(2, false, false), new DeadHandTraitStatusSnapshot() }),
                    actionUse: UnitActionUse.Attacked) : c).ToArray(),
                profiles: input.Profiles.Select(p => p == profile ? new CombatAttackProfile(
                    CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                        new FocusTraitSpec(3, 5), new DeadHandTraitSpec(300)), p.Method) : p).ToArray());
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            var arrival = AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            // Focus는 현재 등록표에서 보류된 효과다. 스킬도 기존 비활성 계약을 유지합니다.
            Assert.That(work.World.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(80));
            var actor = work.World.Characters[input.ActorId];
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(actor.Anchor, Is.EqualTo(input.Characters.Single(c => c.EntityId == input.ActorId).Anchor));
            Assert.That(actor.Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().GainScheduled, Is.False);
            Assert.That(work.World.ItemDirectHits, Is.Empty);
            Assert.That(work.World.Events.OfType<CombatItemEvent>(), Is.Empty);
            Assert.That(arrival.ItemRandomState, Is.EqualTo(input.State.ItemRandomState));
        }
    }

    internal static class PlayerSkillAttackTestData
    {
        internal static TraitTrailFact Dust(CharacterCombatFacts target) => new(90, target.Anchor.Value,
            TraitEffectOrigin.Create(target.EntityId, TraitKey.DustScreen), target.Owner, 0, 1, 50, 2, true,
            new[] { Owner.Player, Owner.Enemy });

        internal static (CommandAttackPlan Plan, CommandAttackProjection World, CommandAttackLedger Ledger) Prepare(
            CombatCommandInput input, CombatDamageKind kind, bool publicPreview = false, bool? completeConsequences = null,
            int[] targets = null, long attackGroup = CommandAttackGroups.Direct)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = publicPreview ? input.State.Environment.VisibleTo(actor.Owner) : input.State.Environment;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var ids = (targets ?? new[] { 2 }).Select(id => new EntityId(id)).ToArray();
            var cells = ids.SelectMany(id => input.Characters.FirstOrDefault(c => c.EntityId == id)?.OccupiedCells
                ?? input.State.Structures.Single(s => s.EntityId == id).OccupiedCells).Distinct().ToArray();
            var selectionCells = new AttackCellSelection(null, cells, Array.Empty<CellCoord>());
            var selection = new ResolvedAttackSelection(selectionCells, selectionCells,
                ids.Select(id => new KeyValuePair<EntityId, AttackTargetRole>(id, AttackTargetRole.Designated)).ToArray(),
                input.Characters, input.State.Structures, default);
            var scope = new AttackTraitScope(actor, capturedEnemyCount: AttackTraitScope.CountEnemies(actor, selection));
            int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, kind, traitScope: scope);
            var delivery = new ResolvedAttackDelivery(ids.Select(id => new DeliveredAttackHit(id, AttackTargetRole.Designated,
                damage, 100, actor.Anchor)).ToArray(), cells);
            var plan = new CommandAttackPlan(input, Array.Empty<IMovementTileRule>(), profile, actor, actor, environment, walls,
                selection, delivery, new CombatEffectEngine(), CombatCommandEffects.Capture(actor, profile),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), damage,
                Rotation4.R90, publicPreview, kind, attackGroup: attackGroup, traitScope: scope, completeConsequences: completeConsequences);
            return (plan, new CommandAttackProjection(input, environment, Array.Empty<AttackBlockedContact>()), new CommandAttackLedger());
        }
    }
}
