using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    public sealed class SacrificeUpgradeTests
    {
        [Test]
        public void OldConstructorKeepsRank1Behavior()
        {
            var spec = new SacrificeTraitSpec(2, 1);
            Assert.That((spec.PreventsLethalSacrificeDamage, spec.ShieldOnActivation), Is.EqualTo((false, 0)));
            Assert.That(spec.SameValues(new SacrificeTraitSpec(2, 1, true)), Is.False);
            Assert.That(spec.SameValues(new SacrificeTraitSpec(2, 1, shieldOnActivation: 10)), Is.False);
        }

        [Test]
        public void NegativeShieldAndWrongFlagKeyAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SacrificeTraitSpec(2, 1, shieldOnActivation: -1));
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.Sacrifice, -1));
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.Sacrifice, 10, 1));
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.Vanguard, 4, preventsLethalSacrificeDamage: true));
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.Sacrifice, 0, exemptsAlliedAttackRangePenalty: true));
        }

        [TestCase(5, 20, 0, 1, 4, TestName = "LethalRedirectLeavesOneThenTenShield")]
        [TestCase(30, 10, 0, 20, 10, TestName = "NonlethalRedirectDoesNotForceOneHP")]
        [TestCase(5, 10, 7, 2, 3, TestName = "ExistingShieldIsConsumedBeforeFloor")]
        [TestCase(1, 20, 0, 1, 0, TestName = "AlreadyAtOneGrantsOnlyOnce")]
        [TestCase(5, 20, 30, 5, 0, TestName = "FullyAbsorbedRedirectStillGrants")]
        public void RedirectSettlesDamageThenGrantsShield(int hp, int damage, int existing, int finalHp, int loss)
        {
            var input = Input(hp, damage);
            if (existing > 0)
                input = ThornHeartTestData.Replace(input, 5, c => c.WithStatusEffects(new StatusEffectSet(new[]
                {
                    new ShieldStatus(TraitEffectOrigin.Create(c.EntityId, TraitKey.PositionSwap), existing, true, 0)
                })));

            var result = Attack(input);
            var bearer = TraitCombatFixtures.Final(result, 5);
            Assert.That((bearer.Health.CurrentHealth, bearer.StageRecord.DamageTaken, Shield(bearer), Used(bearer)),
                Is.EqualTo((finalHp, loss, 10, 1)));
            Assert.That(Reaction(result).Magnitude, Is.EqualTo(loss));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 1).StageRecord.Kills, Is.Zero);
            Assert.That(Grants(result), Has.Length.EqualTo(1));
        }

        [Test]
        public void LethalRedirectWithoutSurvivalDoesNotGrant()
        {
            var result = Attack(Input(5, 20, survival: false));
            var bearer = TraitCombatFixtures.Final(result, 5);
            Assert.That(bearer.Health.IsDead, Is.True);
            Assert.That(Grants(result), Is.Empty);
            Assert.That(Reaction(result).Magnitude, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void AfterDamageHealStillAppliesBeforeShield()
        {
            var input = CombatItemDirectDamageTests.Items(ThornHeartTestData.Replace(Input(5, 20), 5,
                c => CombatItemDirectDamageTests.Equip(c, 5, 1)));
            var result = Attack(input);
            var bearer = TraitCombatFixtures.Final(result, 5);
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == bearer.EntityId);
            Assert.That((bearer.Health.CurrentHealth, hit.HealthRecovery, Shield(bearer)), Is.EqualTo((11, 10, 10)));
            Assert.That(Reaction(result).Magnitude, Is.EqualTo(4));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack)]
        [TestCase(CombatDamageKind.DamageOverTime)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        public void LaterIndependentHitHasNoHealthFloor(CombatDamageKind kind)
        {
            var input = Input(5, 20);
            var redirected = Attack(input);
            var current = CombatCommandComposition.Project(redirected);
            var facts = current.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(current, facts, new List<CombatEvent>());
            var bearerId = new EntityId(5);
            var hit = damage.Apply(bearerId, kind, new CombatDamageOrigin(null, current.ActorId, 2), 11);
            Assert.That(hit.ActualHealthLoss, Is.EqualTo(1));
            Assert.That(hit.After.Health.IsDead, Is.True);
        }

        [Test]
        public void SubsequentHitUsesGrantedShield()
        {
            var current = CombatCommandComposition.Project(Attack(Input(5, 20)));
            var facts = current.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(current, facts, new List<CombatEvent>());
            var hit = damage.Apply(new EntityId(5), CombatDamageKind.OrdinaryAttack,
                new CombatDamageOrigin(null, current.ActorId, 2), 6);
            Assert.That((hit.ActualHealthLoss, hit.After.Health.CurrentHealth, Shield(hit.After)), Is.EqualTo((0, 1, 4)));
        }

        [Test]
        public void NewEnemyTurnAddsToUnspentSacrificeShield()
        {
            var first = Attack(Input(5, 6));
            var second = Attack(CombatCommandTestData.NextEnemyTurn(CombatCommandComposition.Project(first)));
            var bearer = TraitCombatFixtures.Final(second, 5);
            Assert.That((bearer.Health.CurrentHealth, Shield(bearer), Used(bearer)), Is.EqualTo((1, 14, 1)));
            Assert.That(Grants(second), Has.Length.EqualTo(1));
        }

        [Test]
        public void FinishingTarget_IsRedirected_AndTheBearerTakesOnlyNumericDamage()
        {
            // 즉사가 걸릴 대상도 지킨다. 수호자는 즉사하지 않고 숫자 피해를 받으며, 생존 강화로 1에 남고 보호막을 받는다(2026-11-10 사용자 결정).
            var input = TraitCombatFixtures.Equip(Input(5, 100), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            var result = Attack(input);
            Assert.That(result.Attack.Value.TraitActivations.ToArray().Count(t => t.Interception != null), Is.EqualTo(1));
            Assert.That(State(result), Is.EqualTo((1, 10, 1, 5)));
            Assert.That(Grants(result), Has.Length.EqualTo(1));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.DamageCause == CombatDamageCause.InstantDefeat), Is.False);
        }

        [Test]
        public void PreviewAndExecutionAgreeWithUpgrade()
        {
            var input = Input(5, 20);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);
            var executed = Attack(input);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(5)).TargetHealthAfter, Is.EqualTo(1));
            Assert.That(State(projection), Is.EqualTo((1, 10, 1, 5)));
            Assert.That(State(executed), Is.EqualTo(State(projection)));
        }

        [Test]
        public void ExtraAttackUsesSameUpgradeSettlement()
        {
            var input = TraitCombatFixtures.Equip(Input(5, 20), EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec());
            input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, hp: 5));
            var progress = input.State.EnvironmentProgress;
            input = CombatCommandTestData.With(input, state: input.State.With(environmentProgress:
                new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 1)));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[new EntityId(3)] = CombatCommandCalculator.Hurt(characters[new EntityId(3)], 5);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, characters, events);
            var actor = characters[input.ActorId];
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor));
            KillChainAttackCalculator.Resolve(input, actor.EntityId, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                bindings, 20, projection, characters, new CommandAttackGroups(), new Dictionary<EntityId, CellCoord>(),
                new CombatHitCause(), new HashSet<EntityId>());
            var child = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Single(t => t.ChildAttack != null).ChildAttack;
            Assert.That(child.Reactions.Single().Interception.ProtectedId, Is.EqualTo(new EntityId(2)));
            var bearer = characters[new EntityId(5)];
            Assert.That((bearer.Health.CurrentHealth, Shield(bearer), Used(bearer), characters[new EntityId(2)].Health.CurrentHealth),
                Is.EqualTo(State(Attack(Input(5, 20)))));
            Assert.That(child.Hits.Single().TargetHealthAfter, Is.EqualTo(1));
        }

        [Test]
        public void ExtraAttackOnAFinishingTarget_IsRedirected_WithoutInstantDefeat()
        {
            // 처치 연쇄 추가 공격도 희생이 먼저다. 즉사가 걸릴 대상(2, HP 5 ≤ 공격력 100의 20%)을 수호자가 지키고 숫자 피해만 받는다(2026-11-10).
            var input = TraitCombatFixtures.Equip(Input(5, 100), EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec(), new FinishingShotTraitSpec(20));
            input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, hp: 5));
            var progress = input.State.EnvironmentProgress;
            input = CombatCommandTestData.With(input, state: input.State.With(environmentProgress:
                new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 1)));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[new EntityId(3)] = CombatCommandCalculator.Hurt(characters[new EntityId(3)], 5);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, characters, events);
            var actor = characters[input.ActorId];
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor));
            KillChainAttackCalculator.Resolve(input, actor.EntityId, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                bindings, 100, projection, characters, new CommandAttackGroups(), new Dictionary<EntityId, CellCoord>(),
                new CombatHitCause(), new HashSet<EntityId>());
            var child = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Single(t => t.ChildAttack != null).ChildAttack;
            Assert.That(child.Reactions.Single().Interception.ProtectedId, Is.EqualTo(new EntityId(2)));
            Assert.That(characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(characters[new EntityId(5)].Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Any(t => t.TraitActivationKind == TraitActivationKind.InstantDefeat), Is.False);
            Assert.That(events.OfType<CombatCharacterEvent>().Any(e => e.DamageCause == CombatDamageCause.InstantDefeat), Is.False);
        }

        [Test]
        public void CompetingProtectorsAndLaterDirectHitUseGrantedShield()
        {
            var input = Input(5, 20, shield: 20, splash: true);
            input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, hp: 5));
            var cell = new CellCoord(5, 5);
            var reserve = CombatCommandTestData.With(CombatCommandTestData.Character(6, EntityKey.Unit_Sniper, cell, Owner.Player),
                category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { reserve }).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Concat(new[]
            {
                new CombatAttackProfile(CombatCommandTestData.Spec(reserve.CharacterKey, 1, CategoryKey.Heavy),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))
            }).ToArray());
            input = TraitCombatFixtures.Equip(input, reserve.CharacterKey, new SacrificeTraitSpec(2, 1, true, 10));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projected);
            var executed = Attack(input);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(State(executed), Is.EqualTo((1, 6, 1, 5)));
            Assert.That(State(projected), Is.EqualTo(State(executed)));
            var reactions = executed.Attack.Value.TraitActivations.ToArray().Where(t => t.Interception != null);
            Assert.That(reactions.Select(t => (t.Interception.ProtectedId.Value, t.SourceUnitId.Value)),
                Is.EquivalentTo(new[] { (2, 5), (3, 6) }));
            var hit = executed.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(5));
            Assert.That(hit.Contacts.Select(c => c.OriginalTargetId.Value), Is.EquivalentTo(new[] { 2, 5 }));
            Assert.That(hit.DamageDealt, Is.EqualTo(4));
        }

        [Test]
        public void LethalReflectionStopsLaterHitsAfterRedirectAndShieldGrant()
        {
            var input = ThornHeartTestData.Replace(Input(5, 20), 1, c => CombatCommandTestData.With(c, hp: 1));
            var (plan, world, ledger) = CombatItemDirectDamageTests.Prepare(input, duplicate: true);
            var reflection = new ThornHeartAttackScope(ArtifactEffectCompiler.Compile(ThornHeartTestData.Heart()),
                actualOrdinaryAttack: true, inCombat: true);
            var planned = SacrificeDamagePlanner.Plan(plan, world, reflection);
            Assert.That(planned.Keys, Is.EqualTo(new[] { 0 }));
            Assert.That(planned[0], Is.EqualTo(new EntityId(5)));

            var delivered = new HashSet<EntityId>();
            Assert.That(AttackDirectDamageCalculator.Apply(plan, world, ledger, delivered, reflection), Is.True);
            Assert.That(delivered, Is.EquivalentTo(new[] { new EntityId(5) }));
            var bearer = world.Characters[new EntityId(5)];
            Assert.That((bearer.Health.CurrentHealth, Shield(bearer), Used(bearer)), Is.EqualTo((1, 10, 1)));
            Assert.That(world.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(world.Characters[input.ActorId].Health.IsDead, Is.True);
            Assert.That(ledger.Reactions.Single().Interception.DeliveryIndex, Is.Zero);
            Assert.That(world.Events.OfType<CombatArtifactEvent>().Count(e => e.Reflection != null), Is.EqualTo(1));
        }

        private static CombatCommandInput Input(int hp, int damage, bool survival = true, int shield = 10, bool splash = false)
        {
            var input = TraitCombatFixtures.Equip(SacrificeTraitTests.Input(splash: splash), EntityKey.Unit_Shielder,
                new SacrificeTraitSpec(2, 1, survival, shield));
            input = ThornHeartTestData.Replace(input, 5, c => CombatCommandTestData.With(c, hp: hp));
            return ThornHeartTestData.Replace(input, 1, c => c.With(stats: c.Stats.With(attackPower: damage)));
        }

        private static CombatCommandTransition Attack(CombatCommandInput input)
        {
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private static (int Hp, int Shield, int Activations, int AllyHp) State(CombatCommandTransition result)
        {
            var bearer = TraitCombatFixtures.Final(result, 5);
            return (bearer.Health.CurrentHealth, Shield(bearer), Used(bearer), TraitCombatFixtures.Final(result, 2).Health.CurrentHealth);
        }

        private static int Shield(CharacterCombatFacts bearer) => bearer.StatusEffects.Entries.OfType<ShieldStatus>()
            .SingleOrDefault(s => s.Origin == TraitEffectOrigin.Create(bearer.EntityId, TraitKey.Sacrifice))?.RemainingBudget ?? 0;

        private static int Used(CharacterCombatFacts bearer) =>
            bearer.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().ActivationsThisEnemyTurn;

        private static TraitActivationSnapshot Reaction(CombatCommandTransition result) =>
            result.Attack.Value.TraitActivations.ToArray().Single(t => t.Interception != null);

        private static TraitActivationSnapshot[] Grants(CombatCommandTransition result) => result.Events.OfType<CombatEffectsEvent>()
            .SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.Sacrifice &&
                t.TraitActivationKind == TraitActivationKind.ShieldGranted).ToArray();
    }
}
