using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 아킴보 '난사'의 처치 연쇄 공격(KillChainAttack)을 확인한다.
    /// 기본 배치: 공격자 (1,3) 공격력 20, 공격 비용 2, AP 5. 주 대상은 (4,3). 메인 캐릭터 (7,7).
    /// </summary>
    public sealed class KillChainAttackTraitTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly TargetingMethodSpec SingleRadialSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);

        [Test]
        public void Contract_RoundTripsRegistryFactoryCodecAndStatus()
        {
            TraitRegistry registry = TraitRegistry.CreateDefault();
            TraitRegistration registration = registry.GetRequiredRegistration(TraitKey.KillChainAttack);
            Assert.That(registration.SpecType, Is.EqualTo(typeof(KillChainAttackTraitSpec)));
            Assert.That(registration.DefinitionType, Is.EqualTo(typeof(KillChainAttackTraitDefinition)));
            Assert.That(registration.RuntimeStateType, Is.EqualTo(typeof(KillChainAttackRuntimeState)));

            var catalog = new UnitCatalog(new[] { CreateSpec(new KillChainAttackTraitSpec()) }, registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Akimbo);
            Assert.That(definition.TraitDefinitions.Span[0], Is.TypeOf<KillChainAttackTraitDefinition>());

            var stateSet = new UnitTraitStateSet(new[]
            {
                registration.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]),
            });
            var state = stateSet.GetRequired<KillChainAttackRuntimeState>(TraitKey.KillChainAttack);
            var clone = (KillChainAttackRuntimeState)state.Clone();
            Assert.That(clone, Is.Not.SameAs(state));
            state.CopyFinalValuesFrom(clone);
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new KnockbackRuntimeState()));

            IEffectStateSnapshot captured = TraitEffectStateCodec.Capture(state);
            Assert.That(captured.ValueEquals(TraitEffectStateCodec.Capture(clone)), Is.True);
            Assert.That(captured.ValueEquals(StatelessEffectStateSnapshot.Instance), Is.False);
            Assert.That(TraitEffectStateCodec.Restore(captured), Is.TypeOf<KillChainAttackRuntimeState>());

            TraitStatusSnapshot[] statuses = new TraitEffectProvider(registry).CreateStatuses(definition, stateSet);
            var status = (KillChainAttackTraitStatusSnapshot)statuses.Single();
            Assert.That(status.Key, Is.EqualTo(TraitKey.KillChainAttack));
            Assert.That(status.SameValues(new KillChainAttackTraitStatusSnapshot()), Is.True);
            Assert.That(status.SameValues(new KnockbackTraitStatusSnapshot(1)), Is.False);
        }

        [Test]
        public void Engine_ReportsContributionAndRejectsMismatchedSnapshot()
        {
            var engine = new CombatEffectEngine();
            Assert.That(engine.EvaluateKillChainAttack(Array.Empty<EffectRuleBinding>()).HasContribution, Is.False);

            EffectRuleBinding binding = ChainBinding(new KillChainAttackEffectStateSnapshot());
            EffectKillChainAttackEvaluation evaluation = engine.EvaluateKillChainAttack(new[] { binding });
            Assert.That(evaluation.HasContribution, Is.True);
            Assert.That(evaluation.Contributions.Single().Origin, Is.EqualTo(binding.Origin));
            Assert.That(evaluation.Expectations.Single().Origin, Is.EqualTo(binding.Origin));

            Assert.Throws<ArgumentException>(() => engine.EvaluateKillChainAttack(new[]
            {
                ChainBinding(StatelessEffectStateSnapshot.Instance),
            }));
        }

        /// <summary>처치 1명 → 남은 적 하나에 기본 공격과 같은 피해로 추가 공격 1회. 기본 공격 결과와 AP는 바뀌지 않는다.</summary>
        [Test]
        public void Kill_TriggersOneExtraAttackWithBaseDamage_WithoutChangingAttackResultOrCost()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(Final(result, input, 1).Stats.CurrentActionCost, Is.EqualTo(3), "공격 비용 2만 낸다.");

            AttackResult attack = result.Attack.Value;
            Assert.That(attack.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(attack.TraitActivations.ToArray().Any(a => a.TraitKey == TraitKey.KillChainAttack), Is.False);

            TraitActivationSnapshot activation = ChainActivations(result).Single();
            Assert.That(activation.SourceUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(activation.TargetId, Is.EqualTo(new EntityId(3)));
            Assert.That(activation.Magnitude, Is.EqualTo(20));
        }

        /// <summary>발동 사건은 기본 공격 사건 뒤, 그 타격의 체력 사건 앞에 온다.</summary>
        [Test]
        public void ExtraAttack_RecordsActivationBeforeItsHealthEvent()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100));

            var events = CombatCommandCalculator.Calculate(input, NoRules).Events.ToList();

            int attackIndex = events.FindIndex(e => e is CombatAttackEvent);
            int activationIndex = events.FindIndex(IsChainActivation);
            int healthIndex = events.FindIndex(e => e is CombatCharacterEvent c &&
                c.Kind == CombatEventKind.HealthChanged && c.Final.EntityId == new EntityId(3));
            Assert.That(attackIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(activationIndex, Is.GreaterThan(attackIndex));
            Assert.That(healthIndex, Is.EqualTo(activationIndex + 1));
        }

        /// <summary>처치는 1회였지만 추가 공격의 처치가 다시 추가 공격을 만들어 B·C 모두 쓰러진다(순서와 무관).</summary>
        [Test]
        public void ExtraAttackKill_ContinuesChain()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 10),
                Enemy(4, new CellCoord(0, 7), 10));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(Final(result, input, 3).Health.IsDead, Is.True);
            Assert.That(Final(result, input, 4).Health.IsDead, Is.True);
            Assert.That(ChainActivations(result).Select(a => a.TargetId.Value).OrderBy(id => id),
                Is.EqualTo(new[] { new EntityId(3), new EntityId(4) }));
        }

        /// <summary>한 번에 2명 처치 → 추가 공격 2회. 대상은 랜덤이므로 남은 두 적의 피해 합으로 확인한다.</summary>
        [Test]
        public void TwoKillsAtOnce_GrantTwoExtraAttacks()
        {
            CombatCommandInput input = ChainInput(SingleRadialSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(4, 4), 5),
                Enemy(4, new CellCoord(7, 0), 100),
                Enemy(5, new CellCoord(0, 7), 100));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, input, 3).Health.IsDead, Is.True);
            Assert.That(ChainActivations(result), Has.Count.EqualTo(2));
            int lost = 200 - Final(result, input, 4).Health.CurrentHealth - Final(result, input, 5).Health.CurrentHealth;
            Assert.That(lost, Is.EqualTo(40));
        }

        [Test]
        public void NoKill_DoesNotTriggerExtraAttack()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 100),
                Enemy(3, new CellCoord(6, 6), 100));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(ChainActivations(result), Is.Empty);
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void WithoutTrait_KillDoesNotTriggerExtraAttack()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash, withTrait: false,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(ChainActivations(result), Is.Empty);
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        /// <summary>남은 적 캐릭터가 없으면 취소한다. 아군(메인)과 적 본부 구조물은 대상이 아니다.</summary>
        [Test]
        public void OnlyAlliesAndStructuresLeft_CancelsExtraAttack()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash, Enemy(2, new CellCoord(4, 3), 5));
            var headquarters = Headquarters(new CellCoord(0, 7), 100);
            input = CombatCommandTestData.With(input, state: WithStructures(input, headquarters));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ChainActivations(result), Is.Empty);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Final(result, input, 10).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CannonPermissions_DoNotBroadenKillChainHostileCharacterSelection(bool hostileRemains)
        {
            var input = ChainInput(SingleNoSplash, Enemy(2, new CellCoord(4, 3), 5));
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(0, 7), Owner.Player);
            var characters = new[] { actor, input.Characters[1], input.Characters[2], ally };
            if (hostileRemains) characters = characters.Concat(new[] { Enemy(4, new CellCoord(6, 6), 100) }).ToArray();
            var enemyStructure = Headquarters(new CellCoord(5, 0), 100);
            var allyStructure = new StructureCombatFacts(new EntityId(21), enemyStructure.Definition,
                new HealthStateSnapshot(100, 100), new CellCoord(0, 0), Rotation4.R0, new[] { CellCoord.Zero }, Owner.Player);
            input = CombatCommandTestData.With(input, characters: characters,
                state: WithStructures(input, enemyStructure, allyStructure),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Cannon,
                        new KillChainAttackTraitSpec()), SingleNoSplash) : p).ToArray());

            // 일반 공격은 아군과 구조물도 지정할 수 있지만 연쇄 공격 후보는 적 캐릭터로 제한된다.
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Has.Member(AttackAim.ForCell(ally.Anchor.Value)));
            Assert.That(range.AttackAims.ToArray(), Has.Member(AttackAim.ForCell(enemyStructure.Anchor)));
            Assert.That(range.AttackAims.ToArray(), Has.Member(AttackAim.ForCell(allyStructure.Anchor)));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(ChainActivations(result).Select(a => a.TargetId), Is.EqualTo(hostileRemains
                ? new[] { new EntityId(4) } : Array.Empty<EntityId>()));
            if (hostileRemains) Assert.That(Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Final(result, input, 10).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Final(result, input, 1).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.FinalState.Structures.Select(s => s.Health.CurrentHealth), Is.EqualTo(new[] { 100, 100 }));
            Assert.That(Final(result, input, 1).Stats.CurrentActionCost, Is.EqualTo(3));
        }

        /// <summary>기본 공격이 적 본부를 파괴해 전투가 끝나면, 처치가 있어도 추가 공격하지 않는다.</summary>
        [Test]
        public void BattleEndedByBaseAttack_StopsBeforeExtraAttack()
        {
            CombatCommandInput input = ChainInput(SingleRadialSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(0, 7), 100));
            input = CombatCommandTestData.With(input, state: WithStructures(input, Headquarters(new CellCoord(4, 4), 10)));

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(ChainActivations(result), Is.Empty);
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        /// <summary>공격자가 죽었거나 필드에 없으면 시작하지 않는다. 같은 준비에서 살아 있으면 1회 발동해 준비가 맞음을 함께 확인한다.</summary>
        [TestCase(true, 0)]
        [TestCase(false, 1)]
        public void Calculator_StopsWhenActorIsDead(bool actorDead, int expectedActivations)
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[new EntityId(2)] = CombatCommandCalculator.Hurt(projected[new EntityId(2)], 999);
            if (actorDead) projected[input.ActorId] = CombatCommandCalculator.Hurt(projected[input.ActorId], 999);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, projected, events);
            var bindings = CombatCommandEffects.Capture(input.Characters[0], input.Profiles[0]);

            KillChainAttackCalculator.Resolve(input, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                bindings, 20, projection, projected, new CommandAttackGroups(), new Dictionary<EntityId, CellCoord>());

            Assert.That(events.Count(IsChainActivation), Is.EqualTo(expectedActivations));
        }

        /// <summary>보유 방어(보안관 배지)가 피해를 모두 막으면 발동은 피해 0으로 남고, 방어 소모 사건이 뒤따르며 체력 사건은 없다.</summary>
        [Test]
        public void ExtraAttackBlockedByHeldBadge_RecordsZeroDamageActivationThenDefense()
        {
            var shielded = HeldEnvironmentEffects.WithHeldState(Enemy(3, new CellCoord(6, 6), 100),
                new CharacterHeldEnvironmentState(41, true));
            CombatCommandInput input = ChainInput(SingleNoSplash, Enemy(2, new CellCoord(4, 3), 5), shielded);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);
            var events = result.Events.ToList();

            int activationIndex = events.FindIndex(IsChainActivation);
            Assert.That(ChainActivations(result).Single().Magnitude, Is.Zero);
            Assert.That(events[activationIndex + 1], Is.TypeOf<CombatHeldEnvironmentEvent>());
            Assert.That(events.OfType<CombatCharacterEvent>().Any(e => e.Final.EntityId == new EntityId(3)), Is.False);
            var finalTarget = Final(result, input, 3);
            Assert.That(finalTarget.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(finalTarget.HeldEnvironment.BadgeAvailable, Is.False);
        }

        /// <summary>같은 입력은 같은 대상을 고르고, 후보가 둘 이상이면 전투 난수 상태가 진행된다.</summary>
        [Test]
        public void SameInput_ChoosesSameTargetAndAdvancesBattleRandomState()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100),
                Enemy(4, new CellCoord(0, 7), 100));

            CombatCommandTransition first = CombatCommandCalculator.Calculate(input, NoRules);
            CombatCommandTransition second = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(ChainActivations(first).Single().TargetId, Is.EqualTo(ChainActivations(second).Single().TargetId));
            Assert.That(first.FinalState.EnvironmentProgress.RandomState,
                Is.EqualTo(second.FinalState.EnvironmentProgress.RandomState));
            Assert.That(first.FinalState.EnvironmentProgress.RandomState,
                Is.Not.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [Test]
        public void Preview_DoesNotIncludeExtraAttack()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 100));

            AttackPreview preview = CombatCommandCalculator.Preview(input, NoRules, out CombatCommandTransition projection);

            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(projection.Events.Any(IsChainActivation), Is.False);
            Assert.That(Final(projection, input, 3).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(projection.FinalState.EnvironmentProgress.RandomState,
                Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        /// <summary>밤 사망 영혼은 기본 공격 처치와 추가 공격 처치에 각각 한 번씩만 생기고, 각자 사망 위치에 놓인다.</summary>
        [Test]
        public void NightDeaths_SpawnOneSoulPerDeathAtEachDeathCell()
        {
            CombatCommandInput input = ChainInput(SingleNoSplash,
                Enemy(2, new CellCoord(4, 3), 5),
                Enemy(3, new CellCoord(6, 6), 10));
            var night = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline);
            var loadout = new ArtifactLoadoutSnapshot(0, new[]
            {
                new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()),
            });
            input = CombatCommandTestData.With(input, state: night, artifacts: loadout);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, input, 3).Health.IsDead, Is.True);
            Assert.That(result.Field.Final.Entries.Select(s => s.Cell).OrderBy(c => c.X),
                Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(6, 6) }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KillChain_IsWallIndependentAndHostileOnly_ButRequiresAcceptedInitialAttack(bool blockedInitial)
        {
            var friendly = CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(0, 7), Owner.Player);
            var input = ChainInput(SingleNoSplash, Enemy(2, new CellCoord(4, 3), 5), Enemy(3, new CellCoord(6, 6), 100), friendly);
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71,
                blockedInitial ? new CellCoord(2, 3) : new CellCoord(5, 5)));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.EqualTo(!blockedInitial), result.Reason);
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(blockedInitial ? 100 : 80));
            Assert.That(Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(ChainActivations(result).Count, Is.EqualTo(blockedInitial ? 0 : 1));
            if (blockedInitial)
            {
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            }
            else
            {
                var walls = WallGeometryBuilder.Build(8, 8, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
                Assert.That(AttackOcclusionResolver.CheckDirect(new CellCoord(1, 3), new CellCoord(6, 6), walls,
                    AttackOcclusionSpec.Default), Is.Not.Null);
                Assert.That(result.BlockedContacts, Is.Empty);
            }
        }

        private static CombatCommandInput ChainInput(TargetingMethodSpec method, params CharacterCombatFacts[] enemies)
            => ChainInput(method, true, enemies);

        private static CombatCommandInput ChainInput(TargetingMethodSpec method, bool withTrait,
            params CharacterCombatFacts[] enemies)
        {
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(4, 3)), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], traits: withTrait
                ? new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() }
                : Array.Empty<TraitStatusSnapshot>());
            var chars = new[] { actor, input.Characters[1] }.Concat(enemies).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(withTrait
                ? CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Base, new KillChainAttackTraitSpec())
                : CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20), method);
            return CombatCommandTestData.With(input, chars, profiles: profiles);
        }

        private static CharacterCombatFacts Enemy(int id, CellCoord cell, int hp) =>
            CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy), hp: hp);

        private static StructureCombatFacts Headquarters(CellCoord cell, int hp) => new StructureCombatFacts(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, hp, false, 0),
            new HealthStateSnapshot(hp, hp), cell, Rotation4.R0, new[] { cell }, Owner.Enemy);

        private static CombatCommandState WithStructures(CombatCommandInput input, params StructureCombatFacts[] structures) =>
            new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                structures, input.State.ThornFields, input.State.Timeline);

        private static CharacterCombatFacts Final(CombatCommandTransition result, CombatCommandInput input, int id)
        {
            var entityId = new EntityId(id);
            return result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == entityId)?.Final ??
                input.Characters.Single(c => c.EntityId == entityId);
        }

        private static bool IsChainActivation(CombatEvent e) =>
            e is CombatEffectsEvent effects && effects.Traits.Any(t => t.TraitKey == TraitKey.KillChainAttack);

        private static List<TraitActivationSnapshot> ChainActivations(CombatCommandTransition result) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.KillChainAttack).ToList();

        private static EffectRuleBinding ChainBinding(IEffectStateSnapshot state) => new EffectRuleBinding(
            TraitEffectOrigin.Create(new EntityId(7), TraitKey.KillChainAttack),
            new KillChainAttackEffectRule(new KillChainAttackTraitDefinition(), new KillChainAttackTraitRule()),
            state);

        private static UnitSpec CreateSpec(TraitSpec trait)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2,
                30,
                35,
                new[] { trait });
        }
    }
}
