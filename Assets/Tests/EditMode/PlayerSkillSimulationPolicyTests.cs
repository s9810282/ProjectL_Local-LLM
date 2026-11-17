using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillSimulationPolicyTests
    {
        [TestCase(false, null, 1)]
        [TestCase(true, null, 0)]
        [TestCase(true, true, 1)]
        public void CompleteConsequences_ResolveKillChainAndLocalRandomIndependentlyOfObserver(
            bool publicPreview, bool? complete, int expectedChildren)
        {
            var input = ChainInput();
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack, publicPreview, complete);
            var arrival = AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            var children = work.World.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null).ToArray();
            Assert.That(children.Length, Is.EqualTo(expectedChildren));
            var local = arrival.CurrentInput();
            Assert.That(local.State.EnvironmentProgress.RandomState != input.State.EnvironmentProgress.RandomState,
                Is.EqualTo(expectedChildren > 0));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(false, null, 17)]
        [TestCase(true, null, 20)]
        [TestCase(true, true, 17)]
        public void CompleteConsequences_IncludeReflectionWithoutInstallingIt(bool publicPreview, bool? complete, int health)
        {
            var input = ThornHeartTestData.Input(hp: 20);
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack, publicPreview, complete);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(20));
        }

        [TestCase(false, null, 90)]
        [TestCase(true, null, 100)]
        [TestCase(true, true, 90)]
        public void CompleteConsequences_IncludeDeathEffects(bool publicPreview, bool? complete, int neighborHealth)
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId.Value == 2
                    ? CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, c.Anchor.Value,
                        Owner.Enemy, 20), hp: 5, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) }) : c).ToArray(),
                profiles: input.Profiles.Append(new CombatAttackProfile(
                    CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)),
                    input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Demon).Method)).ToArray());
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack, publicPreview, complete);
            var arrival = AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            arrival.Complete();
            Assert.That(work.World.Characters[new EntityId(3)].Health.CurrentHealth, Is.EqualTo(neighborHealth));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void CompleteSimulation_ConsumesIncomingItemOnlyInProjection()
        {
            var input = CombatItemDirectDamageTests.Input(30, 100, 2);
            for (int i = 0; i < 2; i++)
            {
                var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack, true, true);
                AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
                Assert.That(work.World.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(85));
                Assert.That(work.World.Characters[new EntityId(2)].Items.EquippedItemKey, Is.Null);
                Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Items.EquippedItemKey, Is.EqualTo(2));
            }
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void ExplicitKillSeeds_DoNotReplayOldDeathsOrDuplicateTheSameSeed(bool hasFreshKill, int expectedChildren)
        {
            var input = ChainInput();
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            work.World.Characters[new EntityId(2)] = CombatCommandCalculator.Hurt(work.World.Characters[new EntityId(2)], 999);
            work.World.Characters[new EntityId(3)] = CombatCommandCalculator.Hurt(work.World.Characters[new EntityId(3)], 999);
            var arrival = work.World.CreateArrivalProjection(work.Plan);
            var seeds = hasFreshKill ? new[] { new EntityId(3), new EntityId(3) } : Array.Empty<EntityId>();
            KillChainAttackCalculator.Resolve(input, input.ActorId, work.Plan.Engine, work.Plan.Pipeline, work.Plan.Bindings,
                work.Plan.CommonDamage, arrival, work.World.Characters, work.World.AttackGroups, work.World.DeathAnchors,
                work.World.HitCause, new HashSet<EntityId>(), work.Plan, work.World, initialKilledEnemyIds: seeds);
            Assert.That(work.World.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Count(t => t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null), Is.EqualTo(expectedChildren));
            Assert.That(work.World.Characters[new EntityId(4)].Health.CurrentHealth, Is.EqualTo(hasFreshKill ? 80 : 100));
        }

        internal static CombatCommandInput ChainInput()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec());
            return ThornHeartTestData.Replace(input, 2, c => c.WithHealth(new HealthStateSnapshot(5, 100)));
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void SkillExecutor_SeedsOnlyDeathsFromThisOccurrence(bool freshKill, int expectedChildren)
        {
            var input = ChainInput();
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack,
                targets: new[] { freshKill ? 2 : 3 });
            int oldDeath = freshKill ? 3 : 2;
            work.World.Characters[new EntityId(oldDeath)] = CombatCommandCalculator.Hurt(work.World.Characters[new EntityId(oldDeath)], 999);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Count(t => t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null), Is.EqualTo(expectedChildren));
        }

        [Test]
        public void CompleteObservedSimulation_DoesNotReadHiddenDustAgainAtDamageApplication()
        {
            var input = CombatCommandTestData.Input();
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var hidden = PlayerSkillAttackTestData.Dust(target).WithKnownTo(new[] { Owner.Enemy });
            var environment = new MovementEnvironmentFacts(new[] { hidden });
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack, true, true);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.Characters[target.EntityId].Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(work.World.Damage.MovementEffectUses.Where(u => u.Kind == MovementEffectUseKind.DustDefense), Is.Empty);
            Assert.That(input.State.Environment.Tiles.Single(), Is.SameAs(hidden));
        }

        [Test]
        public void ScopeNotifications_CountPrimaryAndChildWithoutOrdinaryActionSettlement()
        {
            var input = ChainInput();
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            TraitAttackNotifications.Apply(input, work.World, work.Plan.TraitScope);
            AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            Assert.That(work.World.TraitTurn.PlayerAttackActionCount, Is.EqualTo(2));
            Assert.That(work.World.Characters[input.ActorId].ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(input.State.TraitTurn.PlayerAttackActionCount, Is.Zero);
        }
    }
}
