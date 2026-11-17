using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyEffectHarmScopeTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Origin = new CellCoord(1, 3);
        private static readonly CellCoord Endpoint = new CellCoord(2, 3);

        // 출혈·무장 해제는 공백을 선언하지 않아 적 평가에서 공백이 없다(R-1). 사냥꾼은 선언한 무작위 결과 공백을 유지한다.
        [TestCase(TraitKey.Hunter, CombatAssessmentGap.RandomOutcome)]
        [TestCase(TraitKey.Bleeding, CombatAssessmentGap.None)]
        [TestCase(TraitKey.Disarm, CombatAssessmentGap.None)]
        public void RemoteClassifiedEffectDoesNotExpandMoveOrAttackHarm(TraitKey trait, CombatAssessmentGap gap)
        {
            var input = TraitCombatFixtures.Equip(FourAllies(), EntityKey.Unit_MainCharacter, Spec(trait));
            var move = MovementTestData.Route(input, Origin, Endpoint);
            var attackHarm = MeasureHarm(input, gap, recordActual: true);
            var moveHarm = MeasureHarm(move, gap, recordActual: true);
            Assert.That((attackHarm, moveHarm), Is.EqualTo((0L, 0L)), "공격·이동의 아군 피해 상한");
        }

        [TestCase(TraitKey.Hunter)]
        [TestCase(TraitKey.Bleeding)]
        [TestCase(TraitKey.Disarm)]
        public void RemovedFalseBoundAllowsUsefulActionBeforeEnd(TraitKey trait)
        {
            var input = TraitCombatFixtures.Equip(EnemyUtilityDecisionTests.Approach(), EntityKey.Unit_MainCharacter, Spec(trait));
            string before = EnemyActionAssessmentTests.Encode(input);
            var actual = CombatCommandCalculator.AdvanceUtility(input, EnemyUtilityDecisionTests.Rules);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.Events.Any(e => e.Kind == CombatEventKind.Moved), Is.True,
                () => string.Join(", ", actual.Events.Select(e => e.Kind)));
            var mover = TraitCombatFixtures.Final(actual, (int)input.ActorId.Value);
            Assert.That(mover.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(mover.Anchor.Value.X, Is.GreaterThan(input.Characters[0].Anchor.Value.X));
        }

        [TestCase(TraitKey.Regeneration)]
        [TestCase(TraitKey.Leap)]
        [TestCase(TraitKey.HuntersMark)]
        [TestCase(TraitKey.Vanguard)]
        [TestCase(TraitKey.Acceleration)]
        [TestCase(TraitKey.StationaryFire)]
        public void ExistingSixExemptionsAddNoGapOrHarm(TraitKey trait)
        {
            // 여섯은 피해 범위만 선언하고 공백은 선언하지 않아 적 평가에서 공백도 피해 상한도 없다(R-1).
            var input = Equip(FourAllies(), EntityKey.Unit_MainCharacter, trait);
            Assert.That(MeasureHarm(input, CombatAssessmentGap.None), Is.Zero);
            Assert.That(MeasureHarm(MovementTestData.Route(input, Origin, Endpoint),
                CombatAssessmentGap.None), Is.Zero);
        }

        [TestCase(TraitKey.Knockback)]
        [TestCase(TraitKey.Pull)]
        public void OwnerAttackForcedMovementIsLeftToPrecisePreview(TraitKey trait)
        {
            var source = FourAllies();
            var owned = Equip(source, EntityKey.Unit_Akimbo, trait);
            var remote = Equip(source, EntityKey.Unit_MainCharacter, trait);
            if (trait == TraitKey.Pull)
                owned = ImpactBoltTests.Replace(owned, 1, unit => CombatCommandTestData.With(unit, appointRange: 2));
            // 행위자 공격의 강제 이동은 정밀 미리보기가 실제 계산으로 센다. 닿는 칸에 도착 원본이 없으면 공백도 아군 전원 상한도 없다
            // (사용자 결정 2026-11-10, 넉백 보유 적이 공격하지 못하던 문제의 원천 수정. 이전 기대값: 공백 + 상한 4000).
            Assert.That(MeasureHarm(owned, CombatAssessmentGap.None, recordActual: true), Is.Zero);
            // 행위자 공격이 아니면 이 후보에 피해가 미치지 않으므로 적 평가는 공백도 내지 않는다(R-1).
            Assert.That(MeasureHarm(MovementTestData.Route(owned, Origin, Endpoint),
                CombatAssessmentGap.None), Is.Zero);
            Assert.That(MeasureHarm(remote, CombatAssessmentGap.None), Is.Zero);
        }

        [Test]
        public void UndeclaredKnockbackCapabilityIsLeftToPrecisePreview()
        {
            // 선언 없는 넉백 계열(충격 볼트)도 적 평가에서는 정밀 미리보기에 맡긴다(이전 기대값: 공백 + 상한 2000). 실제 모드는 그대로 보수적이다.
            var input = ImpactBoltTests.Input();
            Assert.That(MeasureHarm(input, CombatAssessmentGap.None, recordActual: true), Is.Zero);
            var remoteOwner = input.WithActor(input.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter).EntityId);
            AssertNoUnboundedCoverage(remoteOwner);
            AssertNoUnboundedCoverage(input.WithIntent(CombatCommandKind.Move, default, default, null, null));
        }

        // 적 평가(R-1): 선언 없는 규칙·아이템은 무시, 공격이 맞히지 않는 장애물은 세지 않음, 포효·처치 연쇄는 선언한 공백만 남고
        // 피해 선언이 없어 아군 피해 상한을 내지 않는다.
        [TestCase("Unknown", CombatAssessmentGap.None)]
        [TestCase("Roar", CombatAssessmentGap.ActualOnlyReaction)]
        [TestCase("KillChain", CombatAssessmentGap.ActualOnlyReaction)]
        [TestCase("Item", CombatAssessmentGap.None)]
        [TestCase("Obstacle", CombatAssessmentGap.None)]
        public void IndependentSourcesAddNoHarmCeiling(string source, CombatAssessmentGap gap)
        {
            var input = TraitCombatFixtures.Equip(FourAllies(), EntityKey.Unit_MainCharacter, new HunterTraitSpec(2, 3));
            input = source switch
            {
                "Unknown" => Equip(input, EntityKey.Unit_Demon, TraitKey.Intimidation),
                "Roar" => Equip(input, EntityKey.Unit_Demon, TraitKey.Roar),
                "KillChain" => TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new KillChainAttackTraitSpec()),
                "Item" => ImpactBoltTests.Replace(input, 4, unit =>
                    unit.With(items: new CharacterItemState(1, Array.Empty<ItemTemporaryEffect>()))),
                "Obstacle" => WithObstacle(input),
                _ => throw new ArgumentOutOfRangeException(nameof(source)),
            };
            Assert.That(MeasureHarm(input, CombatAssessmentGap.RandomOutcome | gap), Is.Zero);
            Assert.That(MeasureHarm(MovementTestData.Route(input, Origin, Endpoint),
                CombatAssessmentGap.RandomOutcome | gap), Is.Zero);
        }

        private static long MeasureHarm(CombatCommandInput input, CombatAssessmentGap expectedGap, bool recordActual = false)
        {
            string before = EnemyActionAssessmentTests.Encode(input);
            var candidate = input.Kind == CombatCommandKind.Attack
                ? EnemyDecision.Attack(input.ActorId, input.Aim, default)
                : EnemyDecision.Move(input.ActorId, input.Path, default);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var assessment = CombatActionAssessment.Begin(input, candidate, NoRules, context, allowPrecise: false);
            while (!assessment.IsComplete) assessment.Step();
            var facts = assessment.Facts;
            var moved = assessment.MovedFacts;
            Assert.That(assessment.Admitted, Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment | expectedGap));
            Assert.That(context.PreviewCount, Is.Zero);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That(facts.GuaranteedTerminal, Is.False);
            Assert.That(moved, Is.Null);
            if (recordActual)
            {
                var actual = CombatCommandCalculator.Calculate(input, NoRules);
                Assert.That(actual.Accepted, Is.True, actual.Reason);
                using var sha = SHA256.Create();
                var projected = CombatCommandComposition.Project(actual);
                string snapshot = EnemyActionAssessmentTests.Encode(new object[] { projected, actual.Events, actual.Outcome });
                var digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot)));
                TestContext.WriteLine($"{input.Kind}: gaps={facts.Gaps}, previews={context.PreviewCount}, actual={digest}");
            }
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            return facts.PossibleAlliedHpLossPermille;
        }

        private static void AssertNoUnboundedCoverage(CombatCommandInput input)
        {
            bool unbounded = false;
            var gaps = CombatCommandEffects.AssessmentCoverageSteps(input, () => unbounded = true).ToArray();
            Assert.That(gaps, Does.Contain(CombatAssessmentGap.UnsupportedEffect));
            Assert.That(unbounded, Is.False);
        }

        private static CombatCommandInput FourAllies()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            return TraitCombatFixtures.Characters(input, input.Characters
                .Append(CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(0, 0), Owner.Player))
                .Append(CombatCommandTestData.Character(21, EntityKey.Unit_Akimbo, new CellCoord(0, 1), Owner.Player)).ToArray());
        }

        private static TraitSpec Spec(TraitKey trait) => trait switch
        {
            TraitKey.Hunter => new HunterTraitSpec(2, 3),
            TraitKey.Bleeding => new BleedingTraitSpec(2),
            TraitKey.Disarm => new DisarmTraitSpec(1),
            _ => throw new ArgumentOutOfRangeException(nameof(trait)),
        };

        private static CombatCommandInput Equip(CombatCommandInput input, EntityKey key, TraitKey trait)
        {
            (TraitSpec Spec, TraitStatusSnapshot Status) binding = trait switch
            {
                TraitKey.Regeneration => (new RegenerationTraitSpec(2), new RegenerationTraitStatusSnapshot(2)),
                TraitKey.Leap => (new LeapTraitSpec(), new LeapTraitStatusSnapshot()),
                TraitKey.HuntersMark => (new HuntersMarkTraitSpec(20, 1), new HuntersMarkTraitStatusSnapshot(20, 1)),
                TraitKey.Vanguard => (new VanguardTraitSpec(1, 1), new VanguardTraitStatusSnapshot()),
                TraitKey.Acceleration => (new AccelerationTraitSpec(1), new AccelerationTraitStatusSnapshot()),
                TraitKey.StationaryFire => (new StationaryFireTraitSpec(20), new StationaryFireTraitStatusSnapshot()),
                TraitKey.Knockback => (new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1)),
                TraitKey.Pull => (new PullTraitSpec(1), new PullTraitStatusSnapshot(1)),
                TraitKey.Intimidation => (new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20)),
                TraitKey.Roar => (new RoarTraitSpec(5), new RoarTraitStatusSnapshot(0, 5)),
                _ => throw new ArgumentOutOfRangeException(nameof(trait)),
            };
            var pullMethod = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            return CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.CharacterKey == key
                    ? c.With(stats: c.Stats.With(traitStatuses: new[] { binding.Status })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == key
                    ? new CombatAttackProfile(ImpactBoltTests.CopySpec(p.Definition, traits: new[] { binding.Spec }),
                        trait == TraitKey.Pull ? pullMethod : p.Method, p.OwnedId)
                    : p).ToArray());
        }

        private static CombatCommandInput WithObstacle(CombatCommandInput input)
        {
            var cell = new CellCoord(7, 6);
            var id = new EntityId(90);
            var obstacle = new StructureCombatFacts(id, new StructureSpec(EntityKey.Structure_PowderBarrel,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new ObstacleDurabilitySnapshot(3, 3), cell, Rotation4.R0, new[] { cell }, Owner.Neutral);
            var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), new[] { id });
            return input.WithWorld(input.State.With(structures: new[] { obstacle }), input.Characters,
                input.Main, input.SoulCounts, input.Souls, bindings);
        }
    }
}
