using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>포효 밤 피격 집계를 확인한다(계획 T5.1, D7·G14). fixture는 <see cref="BossRoarTests.Input"/>(임계값 5)이다.</summary>
    public sealed class BossHitCounterTests
    {
        private static readonly EntityId Boss = BossRoarTests.Boss;

        [Test]
        public void Record_CountsOncePerCause_OnlyAtNight_OnlyLivingRoarHolder()
        {
            var input = BossRoarTests.Input(0);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var cause = new CombatHitCause();

            Assert.That(RoarHitCounter.Record(characters, Boss, cause, inCombat: true), Is.True);
            Assert.That(RoarHitCounter.Record(characters, Boss, cause, inCombat: true), Is.False);
            Assert.That(RoarHitCounter.Record(characters, Boss, new CombatHitCause(), inCombat: false), Is.False);
            Assert.That(RoarHitCounter.Record(characters, Boss, new CombatHitCause(), inCombat: true), Is.True);
            // 포효 Trait가 없는 대상은 세지 않는다.
            Assert.That(RoarHitCounter.Record(characters, BossRoarTests.Player, new CombatHitCause(), inCombat: true), Is.False);
            Assert.That(RoarHitCounter.HitCount(characters[Boss]), Is.EqualTo(2));

            characters[Boss] = CombatCommandCalculator.Copy(characters[Boss], new HealthStateSnapshot(0, 100), characters[Boss].Stats);
            Assert.That(RoarHitCounter.Record(characters, Boss, new CombatHitCause(), inCombat: true), Is.False);
        }

        [Test]
        public void ZeroDamageAttack_StillCounts()
        {
            var input = WithPlayerAttack(BossRoarTests.Input(0), 0);

            var result = Calculate(input);

            var boss = BossRoarTests.Final(result, Boss);
            Assert.That(boss.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(RoarHitCounter.HitCount(boss), Is.EqualTo(1));
        }

        [Test]
        public void SplashWithDeferredThornTrait_CountsOnce()
        {
            // 가시밭 생성이 보류되어도 스플래시 피격은 보스 횟수에 한 번 반영된다.
            var input = BossRoarTests.Input(0);
            var characters = input.Characters.Select(c => c.EntityId == BossRoarTests.Player
                ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new ThornFieldTraitStatusSnapshot(5, 5) }) : c).ToArray();
            var splash = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new ThornFieldTraitSpec(5, 5)), splash)
                : p).ToArray();
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters, profiles: profiles), AttackAim.ForCell(new CellCoord(3, 4)));

            var result = Calculate(input);

            var bossHits = result.Events.OfType<CombatCharacterEvent>().Count(e => e.Final.EntityId == Boss &&
                e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth);
            Assert.That(bossHits, Is.EqualTo(1));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, Boss)), Is.EqualTo(1));
        }

        [Test]
        public void IndependentEnvironmentHits_CountSeparately_HealingDoesNot()
        {
            var input = BossRoarTests.Input(0);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);

            projection.ChangeHealth(new MovementHealthRequest(Boss, 3, new CombatDamageOrigin(new CellCoord(3, 2), null, projection.NextAttackGroup())), false);
            projection.ChangeHealth(new MovementHealthRequest(Boss, 0, new CombatDamageOrigin(new CellCoord(3, 2), null, projection.NextAttackGroup())), false);
            projection.ChangeHealth(new MovementHealthRequest(Boss, 2), true);

            var boss = projection.Character(Boss);
            Assert.That(boss.Health.CurrentHealth, Is.EqualTo(99));
            // 피해 요청마다 독립 원인이다. 0 피해도 세고 회복은 세지 않는다.
            Assert.That(RoarHitCounter.HitCount(boss), Is.EqualTo(2));
        }

        [Test]
        public void SharedCauseProjection_CountsOnce()
        {
            var input = BossRoarTests.Input(0);
            var events = new List<CombatEvent>();
            var projection = new MovementProjection(input, input.State,
                CombatDamageApplication.For(input, input.Characters.ToDictionary(c => c.EntityId), events),
                input.State.Structures.ToDictionary(s => s.EntityId), input.State.ThornFields.ToDictionary(f => f.Cell),
                input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls, new List<long>(),
                new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), hitCause: new CombatHitCause());

            projection.ChangeHealth(new MovementHealthRequest(Boss, 3, new CombatDamageOrigin(new CellCoord(3, 2), null, projection.NextAttackGroup())), false);
            projection.ChangeHealth(new MovementHealthRequest(Boss, 3, new CombatDamageOrigin(new CellCoord(3, 2), null, projection.NextAttackGroup())), false);

            Assert.That(RoarHitCounter.HitCount(projection.Character(Boss)), Is.EqualTo(1));
        }

        [Test]
        public void KillChainOnly_RecordsHit()
        {
            // 플레이어가 보스 아래 적(체력 1)을 처치하면 처치 연쇄가 남은 적인 보스를 때린다. 보스는 원래 공격에 맞지 않았다(G14).
            var input = BossRoarTests.Input(0);
            var characters = input.Characters.Select(c =>
                c.EntityId == BossRoarTests.Player ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() }) :
                c.EntityId == BossRoarTests.Ally ? CombatCommandTestData.With(c, hp: 1) : c).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new KillChainAttackTraitSpec()), p.Method)
                : p).ToArray();
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters, profiles: profiles), AttackAim.ForCell(new CellCoord(3, 4)));

            var result = Calculate(input);

            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { BossRoarTests.Ally }));
            var boss = BossRoarTests.Final(result, Boss);
            Assert.That(boss.Health.CurrentHealth, Is.LessThan(100));
            Assert.That(RoarHitCounter.HitCount(boss), Is.EqualTo(1));
        }

        [Test]
        public void WithoutRailwayDefinition_DoesNotRecordAHitOrDeath()
        {
            var input = BossRoarTests.Input(3);
            Assert.That(CombatGimmickCalculator.TryAdvance(input), Is.Null);
            var boss = input.Characters.Single(c => c.EntityId == Boss);
            Assert.That(boss.Health.IsDead, Is.False);
            Assert.That(RoarHitCounter.HitCount(boss), Is.EqualTo(3));
        }

        private static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private static CombatCommandInput WithPlayerAttack(CombatCommandInput input, int attack)
        {
            var characters = input.Characters.Select(c => c.EntityId == BossRoarTests.Player
                ? CombatCommandTestData.Character(c.EntityId.Value, c.CharacterKey, c.Anchor.Value, c.Owner, attack) : c).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attack), p.Method) : p).ToArray();
            return CombatCommandTestData.With(input, characters, profiles: profiles);
        }
    }
}
