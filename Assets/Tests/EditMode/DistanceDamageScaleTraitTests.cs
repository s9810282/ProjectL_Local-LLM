using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
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
    /// <summary>샷건 '산탄'(거리별 피해 배율) 특성의 계약과 공격 계산을 확인한다.</summary>
    public sealed class DistanceDamageScaleTraitTests
    {
        private static readonly int[] ShotgunPercents = { 150, 50 };

        [Test]
        public void Contract_RoundTripsRegistryFactoryCodecAndStatus()
        {
            var source = (int[])ShotgunPercents.Clone();
            var spec = new DistanceDamageScaleTraitSpec(source);
            source[0] = 999;
            Assert.That(spec.PercentByDistance, Is.EqualTo(ShotgunPercents), "입력 배열을 복사해 보관한다.");
            Assert.Throws<ArgumentNullException>(() => new DistanceDamageScaleTraitSpec(null));
            Assert.Throws<ArgumentException>(() => new DistanceDamageScaleTraitSpec(Array.Empty<int>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DistanceDamageScaleTraitSpec(new[] { 150, -1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DistanceDamageScaleTraitDefinition(new[] { -1 }));
            Assert.Throws<ArgumentException>(() => new DistanceDamageScaleTraitStatusSnapshot(Array.Empty<int>()));

            TraitRegistry registry = TraitRegistry.CreateDefault();
            TraitRegistration registration = registry.GetRequiredRegistration(TraitKey.DistanceDamageScale);
            Assert.That(registration.SpecType, Is.EqualTo(typeof(DistanceDamageScaleTraitSpec)));
            Assert.That(registration.DefinitionType, Is.EqualTo(typeof(DistanceDamageScaleTraitDefinition)));
            Assert.That(registration.RuntimeStateType, Is.EqualTo(typeof(DistanceDamageScaleRuntimeState)));

            var catalog = new UnitCatalog(new[] { CreateSpec(spec) }, registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_ShotGun);
            var traitDefinition = (DistanceDamageScaleTraitDefinition)definition.TraitDefinitions.Span[0];
            Assert.That(traitDefinition.PercentByDistance, Is.EqualTo(ShotgunPercents));

            var stateSet = new UnitTraitStateSet(new[]
            {
                registration.TraitRuntimeStateFactory.BuildRuntimeState(traitDefinition),
            });
            var state = stateSet.GetRequired<DistanceDamageScaleRuntimeState>(TraitKey.DistanceDamageScale);
            var clone = (DistanceDamageScaleRuntimeState)state.Clone();
            Assert.That(clone, Is.Not.SameAs(state));
            state.CopyFinalValuesFrom(clone);
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new KnockbackRuntimeState()));

            IEffectStateSnapshot captured = TraitEffectStateCodec.Capture(state);
            Assert.That(captured.ValueEquals(TraitEffectStateCodec.Capture(clone)), Is.True);
            Assert.That(captured.ValueEquals(StatelessEffectStateSnapshot.Instance), Is.False);
            Assert.That(TraitEffectStateCodec.Restore(captured), Is.TypeOf<DistanceDamageScaleRuntimeState>());

            TraitStatusSnapshot[] statuses = new TraitEffectProvider(registry).CreateStatuses(definition, stateSet);
            var status = (DistanceDamageScaleTraitStatusSnapshot)statuses.Single();
            Assert.That(status.Key, Is.EqualTo(TraitKey.DistanceDamageScale));
            Assert.That(status.PercentByDistance, Is.EqualTo(ShotgunPercents));
            Assert.That(status.SameValues(new DistanceDamageScaleTraitStatusSnapshot(new[] { 150, 50 })), Is.True);
            Assert.That(status.SameValues(new DistanceDamageScaleTraitStatusSnapshot(new[] { 150 })), Is.False);
        }

        [TestCase(1, 150)]
        [TestCase(2, 50)]
        public void Engine_ReturnsPercentForDistance(int distance, int expectedPercent)
        {
            EffectRuleBinding binding = ShotgunBinding();

            EffectTargetDistanceDamageEvaluation evaluation = new CombatEffectEngine().EvaluateTargetDistanceDamage(
                new[] { binding }, new TargetDistanceDamageEffectContext(distance));

            Assert.That(evaluation.HasContribution, Is.True);
            Assert.That(evaluation.Numerator, Is.EqualTo(expectedPercent));
            Assert.That(evaluation.Denominator, Is.EqualTo(100));
            Assert.That(evaluation.Expectations.Single().Origin, Is.EqualTo(binding.Origin));
        }

        [Test]
        public void Engine_DistanceOutsideListIsConfigurationError()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new CombatEffectEngine().EvaluateTargetDistanceDamage(
                    new[] { ShotgunBinding() }, new TargetDistanceDamageEffectContext(3)));
            StringAssert.Contains("거리 3", exception.Message);
        }

        [Test]
        public void ConeAttack_TargetBeyondListThrowsInsteadOfDefaulting()
        {
            CombatCommandInput input = ConeInput(20, withTrait: true, percents: new[] { 150 });

            Assert.Throws<InvalidOperationException>(() =>
                CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
            Assert.Throws<InvalidOperationException>(() =>
                CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()));
        }

        [Test]
        public void Engine_WithoutContributionIsIdentityAndRejectsMismatchedSnapshot()
        {
            var engine = new CombatEffectEngine();
            EffectTargetDistanceDamageEvaluation empty = engine.EvaluateTargetDistanceDamage(
                Array.Empty<EffectRuleBinding>(), new TargetDistanceDamageEffectContext(1));
            Assert.That(empty.HasContribution, Is.False);
            Assert.That(empty.Numerator, Is.EqualTo(1));
            Assert.That(empty.Denominator, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TargetDistanceDamageEffectContext(0));

            EffectRuleBinding valid = ShotgunBinding();
            Assert.Throws<ArgumentException>(() => engine.EvaluateTargetDistanceDamage(new[]
            {
                new EffectRuleBinding(valid.Origin, valid.Rule, StatelessEffectStateSnapshot.Instance),
            }, new TargetDistanceDamageEffectContext(1)));
        }

        /// <summary>ForwardCone 사거리 2: 앞 1칸은 150%, 뒤 가로 3칸은 50%이며 기존 배율과 같은 half-up으로 반올림한다.</summary>
        [TestCase(1, 2, 1)]
        [TestCase(2, 3, 1)]
        [TestCase(3, 5, 2)]
        [TestCase(20, 30, 10)]
        public void ConeAttack_ScalesFrontAndBackRowsAndPreviewMatches(int attack, int front, int back)
        {
            CombatCommandInput input = ConeInput(attack, withTrait: true);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var dealt = result.Attack.Value.Hits.ToArray().ToDictionary(h => h.TargetId, h => h.DamageDealt);
            Assert.That(dealt[new EntityId(2)], Is.EqualTo(front));
            Assert.That(dealt[new EntityId(3)], Is.EqualTo(back));
            Assert.That(dealt[new EntityId(4)], Is.EqualTo(back));
            Assert.That(dealt[new EntityId(5)], Is.EqualTo(back));

            AttackPreview preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            var expected = preview.Targets.ToArray().ToDictionary(t => t.TargetId, t => t.ExpectedDamage);
            Assert.That(expected, Is.EquivalentTo(dealt));
        }

        [Test]
        public void ConeAttack_WithoutTraitKeepsCommonDamage()
        {
            CombatCommandTransition result = CombatCommandCalculator.Calculate(
                ConeInput(1, withTrait: false), Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.All.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(4));
        }

        /// <summary>K7: 부채꼴 뒤 칸의 구조물도 거리 배율을 받는다. 구조물은 상성이 없어 배율 결과가 그대로 피해다.</summary>
        [Test]
        public void ConeAttack_ScalesStructureByDistance()
        {
            CombatCommandInput input = ConeInput(20, withTrait: true);
            var chars = input.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray();
            var headquarters = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), new CellCoord(3, 3), Rotation4.R0, new[] { new CellCoord(3, 3) }, Owner.Enemy);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { headquarters }, input.State.ThornFields, input.State.Timeline);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(
                CombatCommandTestData.With(input, chars, state), Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var dealt = result.Attack.Value.Hits.ToArray().ToDictionary(h => h.TargetId, h => h.DamageDealt);
            Assert.That(dealt[new EntityId(2)], Is.EqualTo(30));
            Assert.That(dealt[headquarters.EntityId], Is.EqualTo(10));
        }

        /// <summary>
        /// 스플래시 대상은 거리 배율(half-up)을 먼저 받고 그다음 스플래시 비율(버림)을 받는다.
        /// 공격력 7이면 순서가 반대일 때와 결과가 달라 순서를 구분한다(반대 순서라면 거리 1 추가 대상은 6).
        /// </summary>
        [Test]
        public void SplashAttack_AppliesDistanceScaleBeforeSplashTruncation()
        {
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(2, 3)), empty: true);
            var actor = CombatCommandTestData.With(
                CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 7),
                traits: new TraitStatusSnapshot[] { new DistanceDamageScaleTraitStatusSnapshot(ShotgunPercents) });
            var chars = new[]
            {
                actor,
                input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 4), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
            };
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 7, CategoryKey.Base,
                new DistanceDamageScaleTraitSpec(ShotgunPercents)), profiles[0].Method);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(
                CombatCommandTestData.With(input, chars, profiles: profiles), Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var dealt = result.Attack.Value.Hits.ToArray().ToDictionary(h => h.TargetId, h => h.DamageDealt);
            Assert.That(dealt[new EntityId(2)], Is.EqualTo(11), "주 대상 거리 1: 7×150% = 10.5 → 11");
            Assert.That(dealt[new EntityId(3)], Is.EqualTo(7), "추가 대상 거리 1: 11 → 70% = 7.7 → 7");
            Assert.That(dealt[new EntityId(4)], Is.EqualTo(2), "추가 대상 거리 2: 7×50% = 3.5 → 4 → 70% = 2.8 → 2");
        }

        /// <summary>
        /// 관통 대상은 관통 감쇠를 먼저, 거리 배율을 나중에 받는다. 둘 다 half-up이다.
        /// 공격력 3, 거리 3(두 번째 만남)에서 순서가 반대라면 4가 나온다.
        /// </summary>
        [Test]
        public void PierceAttack_AppliesFalloffBeforeDistanceScale()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            int[] percents = { 100, 100, 150, 100 };
            var actor = CombatCommandTestData.With(
                CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 3),
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[]
                {
                    new PiercingFalloffTraitStatusSnapshot(30),
                    new DistanceDamageScaleTraitStatusSnapshot(percents),
                });
            var chars = new[]
            {
                actor,
                input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy),
            };
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 3, CategoryKey.Cannon,
                new PiercingFalloffTraitSpec(30, 99), new DistanceDamageScaleTraitSpec(percents)), method);

            CombatCommandTransition result = CombatCommandCalculator.Calculate(
                CombatCommandTestData.With(input, chars, profiles: profiles), Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var dealt = result.Attack.Value.Hits.ToArray().ToDictionary(h => h.TargetId, h => h.DamageDealt);
            Assert.That(dealt[new EntityId(2)], Is.EqualTo(3), "거리 2, 감쇠 100%, 배율 100%");
            Assert.That(dealt[new EntityId(3)], Is.EqualTo(3), "거리 3: 3×70% = 2.1 → 2 → 150% = 3");
            Assert.That(dealt[new EntityId(4)], Is.EqualTo(1), "거리 4: 3×40% = 1.2 → 1 → 100% = 1");
        }

        private static EffectRuleBinding ShotgunBinding() => new EffectRuleBinding(
            TraitEffectOrigin.Create(new EntityId(7), TraitKey.DistanceDamageScale),
            new DistanceDamageScaleEffectRule(
                new DistanceDamageScaleTraitDefinition(ShotgunPercents),
                new DistanceDamageScaleTraitRule()),
            new DistanceDamageScaleEffectStateSnapshot());

        /// <summary>공격자 (1,3)이 오른쪽을 보면 앞 1칸 (2,3), 뒤 가로 3칸 (3,2)·(3,3)·(3,4)다.</summary>
        private static CombatCommandInput ConeInput(int attack, bool withTrait, int[] percents = null)
        {
            percents ??= ShotgunPercents;
            var method = new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            TraitStatusSnapshot[] statuses = withTrait
                ? new TraitStatusSnapshot[] { new DistanceDamageScaleTraitStatusSnapshot(percents) }
                : Array.Empty<TraitStatusSnapshot>();
            TraitSpec[] traits = withTrait
                ? new TraitSpec[] { new DistanceDamageScaleTraitSpec(percents) }
                : Array.Empty<TraitSpec>();
            var actor = CombatCommandTestData.With(
                CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, attack),
                traits: statuses, appointRange: 2);
            var chars = new[]
            {
                actor,
                input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(3, 4), Owner.Enemy),
            };
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attack, CategoryKey.Base, traits), method);
            return CombatCommandTestData.With(input, chars, profiles: profiles);
        }

        private static UnitSpec CreateSpec(TraitSpec trait)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_ShotGun,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                3,
                1,
                4,
                1,
                3,
                1,
                10,
                10,
                new[] { trait });
        }
    }
}
