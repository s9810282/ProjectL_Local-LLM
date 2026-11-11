using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyFollowupEstimateTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;
        private delegate EnemyScoreFacts Position(CharacterCombatFacts actor, EnemyScoreFacts facts, MovementPath path,
            StageHazardForecast hazards, EnemyThreatField threat, long progress, long followup, long entryLoss, CombatAssessmentGap entryGaps,
            long nextTurnStrike);
        private static readonly Position PositionFacts = (Position)typeof(EnemyDecisionSession)
            .GetMethod("PositionFacts", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(typeof(Position));
        private static readonly MethodInfo FollowupMethod = typeof(EnemyDecisionSession)
            .GetMethod("FollowupSteps", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestCase(false, 1)]
        [TestCase(true, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 2)]
        [TestCase(false, 31)]
        [TestCase(true, 31)]
        public void RegenerationKeepsNonlethalMoveAttackValue(bool remote, int partition)
        {
            foreach (bool regeneration in new[] { false, true })
            {
                var input = Input(regeneration, remote);
                string before = EnemyActionAssessmentTests.Encode(input);
                using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
                EnemyUtilityDecisionTests.Drain(decision, partition);
                Assert.That(decision.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move), regeneration ? "재생 있음" : "재생 없음");
                var result = CombatCommandCalculator.AdvancePrepared(input, decision.Decision, Rules);
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(result.PathMovement, Is.Not.Null);
                Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void FixedMoveAndAttackExecutionIsUnchanged(bool regeneration, bool remote)
        {
            var input = Input(regeneration, remote);
            string before = EnemyActionAssessmentTests.Encode(input);
            var move = CombatCommandCalculator.AdvancePrepared(input, Move(input), Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var moved = CombatCommandComposition.Project(move);
            var attack = CombatCommandCalculator.AdvancePrepared(moved,
                EnemyDecision.Attack(input.ActorId, AttackAim.ForCell(new CellCoord(4, 2)), default), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var snapshot = EnemyActionAssessmentTests.Encode(new object[] { moved, move.Events, move.Outcome,
                CombatCommandComposition.Project(attack), attack.Events, attack.Outcome });
            using var sha = SHA256.Create();
            TestContext.WriteLine("fixed-intent=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot))));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RegenerationMoveIsPreciseWithExactNonlethalScore(bool remote)
        {
            // 재생은 공백을 선언하지 않아 적 평가에서 무시된다(R-1). 이동은 조건부 투영이 아니라 정밀 평가로 간다.
            var input = Input(remote: remote);
            var holder = input.Characters.Single(unit => unit.CharacterKey == (remote ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Demon));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(unit => unit.EntityId == holder.EntityId
                ? unit.WithHealth(new HealthStateSnapshot(80, 100)) : unit).ToArray());
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var move = AssessMove(input, context);
            Assert.That(move.Admitted, Is.True);
            Assert.That(move.Facts.IsPrecise, Is.True);
            Assert.That(move.Facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(move.Facts.PossibleAlliedHpLossPermille, Is.Zero);
            Assert.That(move.MovedFacts, Is.Not.Null);
            Assert.That(move.MovedFacts.Characters.Single(unit => unit.EntityId == holder.EntityId).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(Actor(move.MovedFacts).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(move.MovedFacts.State.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            long benefit = Followup(move.MovedFacts, context, false);
            Assert.That(benefit, Is.EqualTo(400));
            var score = Score(input, move.Facts, benefit);
            Assert.That(score.EngagementProgress, Is.EqualTo(3));
            Assert.That(score.PlayerThreatPermille, Is.EqualTo(150));
            Assert.That(EnemyUtilityScorer.Score(score), Is.EqualTo(385), "후속 400 × 1 + 진척 60 − 위협 150 / 2(R-3 교환 모형)");
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.That(score.GuaranteedTerminal, Is.False);
        }

        [TestCase("Unknown")]
        [TestCase("UndeclaredHarmless")]
        [TestCase("Held")]
        [TestCase("Item")]
        [TestCase("Artifact")]
        [TestCase("Obstacle")]
        public void MixedSourcesFollowEnemyAssessmentPolicy(string source)
        {
            // 실제 모드는 그대로 막고, 적 평가 모드는 이 출처들을 무시하거나(장애물은 건드리지 않는 후보라서) 정밀 이동 평가로 간다(R-1).
            var input = Input(remote: true);
            input = source switch
            {
                "Unknown" => AddTrait(input, EntityKey.Unit_Demon, new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20)),
                "UndeclaredHarmless" => TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new LeapTraitSpec()),
                "Held" => ReplaceActor(input, actor => actor.With(heldEnvironment: new CharacterHeldEnvironmentState(suppliesAttackBonus: 1))),
                "Item" => ReplaceActor(input, actor => actor.With(items: new CharacterItemState(1, Array.Empty<ItemTemporaryEffect>()))),
                "Artifact" => CombatCommandTestData.With(input, artifacts: new ArtifactLoadoutSnapshot(0,
                    new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) })),
                "Obstacle" => WithObstacle(input),
                _ => throw new ArgumentOutOfRangeException(nameof(source)),
            };
            string before = EnemyActionAssessmentTests.Encode(input);
            foreach (bool reverse in new[] { false, true })
            {
                var current = reverse ? CombatCommandTestData.With(input, characters: input.Characters.Reverse().ToArray()) : input;
                bool blocked = false, harm = false;
                var intent = current.WithIntent(CombatCommandKind.Move, default, default, null, null);
                var oldGaps = CombatCommandEffects.AssessmentCoverageSteps(intent).ToArray();
                var gaps = CombatCommandEffects.AssessmentCoverageSteps(intent, () => harm = true, () => blocked = true).ToArray();
                Assert.That(gaps, Is.EqualTo(oldGaps));
                Assert.That(blocked, Is.True);
                Assert.That(harm, Is.EqualTo(source != "UndeclaredHarmless"));
                var budget = new EnemyDecisionContext(current, EnemyDecisionSettings.Default);
                using var work = AssessMove(current, budget);
                Assert.That(work.Admitted, Is.True);
                Assert.That(work.MovedFacts, Is.Not.Null);
                Assert.That(budget.PreviewCount, Is.EqualTo(1));
                Assert.That(work.Facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.False);
            }
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase("Mine")]
        [TestCase("Unknown")]
        [TestCase("Tornado")]
        public void RouteUncertaintyCannotCreateFollowupWorld(string source)
        {
            var input = Input();
            var cell = new CellCoord(1, 2);
            MovementTileFact tile = source switch
            {
                "Mine" => new MineSourceFact(71, cell, 1, 10, true, new[] { Owner.Enemy }),
                "Unknown" => new UnknownArrival(cell),
                "Tornado" => TornadoEnvironmentTests.Source(cell: cell),
                _ => throw new ArgumentOutOfRangeException(nameof(source)),
            };
            input = CliffFallTests.WithEnvironment(input, tile);
            if (source == "Tornado") input = TornadoEnvironmentTests.WithStorms(input);
            using var work = AssessMove(input, new EnemyDecisionContext(input, EnemyDecisionSettings.Default));
            Assert.That(work.Admitted, Is.True);
            Assert.That(work.Facts.IsPrecise, Is.False);
            Assert.That(work.MovedFacts, Is.Null);
        }

        [Test]
        public void UnsupportedPassengerArrivalStaysBlocked()
        {
            // 경로 행위자(아군 쪽)가 보는 도착 원본이어야 경로 공백이 승객 도착을 잡는다. 행위자가 모르는 원본은 숨은 환경 공백뿐이다.
            var input = RegenerationTurnTests.Regeneration(PositionSwapMovementTests.Route(), EntityKey.Unit_Akimbo, 10, 100, 100);
            input = CliffFallTests.WithEnvironment(input, new UnknownArrival(input.Path.Origin, Owner.Player));
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = CombatActionAssessment.Begin(input, EnemyDecision.Move(input.ActorId, input.Path, default), Rules, budget);
            Drain(work);
            Assert.That(work.Admitted, Is.True);
            Assert.That(work.MovedFacts, Is.Null);
            Assert.That(budget.PreviewCount, Is.Zero);
        }

        [TestCase(7)]
        [TestCase(100)]
        public void NoDoubleChargeForPreciseMoveAndNoFollowupAfterDeath(int damage)
        {
            // 재생 무시(R-1)로 이 이동은 정밀 평가다. 가시 피해는 정밀 사실에 한 번만 들어가고 진입 추정은 더하지 않는다.
            var input = Input();
            input = CombatCommandTestData.With(input, state: input.State.With(thornFields:
                new[] { new ThornFieldCellSnapshot(new CellCoord(1, 2), 2, damage) }));
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = AssessMove(input, budget);
            Assert.That(work.Admitted, Is.True);
            Assert.That(work.Facts.IsPrecise, Is.True);
            Assert.That(work.Facts.AlliedHpLossPermille, Is.EqualTo(damage * 10));
            if (damage == 100) Assert.That(work.MovedFacts, Is.Null);
            else
            {
                Assert.That(Actor(work.MovedFacts).Health.CurrentHealth, Is.EqualTo(93));
                var score = Score(input, work.Facts, Followup(work.MovedFacts, budget, false));
                Assert.That(score.AlliedHpLossPermille, Is.EqualTo(70));
                Assert.That(Actor(input).Health.CurrentHealth, Is.EqualTo(100));
            }
        }

        [Test]
        public void MovedWorldOwnsDamageModifiers()
        {
            var input = CliffFallTests.WithEnvironment(Input(), new DestinationTileModifierFact(71, new CellCoord(3, 2), true,
                new[] { Owner.Enemy }, 10, 0));
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = AssessMove(input, budget);
            Assert.That(work.MovedFacts, Is.Not.Null);
            Assert.That(Followup(work.MovedFacts, budget, false), Is.EqualTo(500));
            Assert.That(Actor(input).PositionModifier.AttackBonus, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExistingMovedInputCanUseCoarseAttackWhenPreviewIsExhausted(bool preciseMove)
        {
            var input = Input(false);
            var budget = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            using var work = AssessMove(input, budget);
            Assert.That(work.Facts.IsPrecise, Is.True);
            Assert.That(budget.PreviewCount, Is.EqualTo(1));
            Assert.That(Followup(work.MovedFacts, budget, preciseMove), Is.EqualTo(400));
            Assert.That(budget.PreviewCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConditionalMoveRequiresRefinementAndPreviewBudget(bool exhaust)
        {
            var input = Input();
            var budget = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            if (exhaust) Assert.That(budget.TryChargePreview(input.ActorId), Is.True);
            using var work = AssessMove(input, budget, allowPrecise: exhaust);
            Assert.That(work.MovedFacts, Is.Null);
            Assert.That(work.Facts.IsPrecise, Is.False);
            Assert.That(work.Facts.BudgetExhausted, Is.EqualTo(exhaust));
            Assert.That(budget.PreviewCount, Is.EqualTo(exhaust ? 1 : 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisarmOrUsedAttackCannotGainFollowup(bool used)
        {
            var input = Input();
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = AssessMove(input, budget);
            var moved = ReplaceActor(work.MovedFacts, actor => used ? actor.With(actionUse: UnitActionUse.Attacked)
                : actor.WithStatusEffects(AppliedStatusRules.Apply(actor.StatusEffects, new ActionRestrictionStatus(StatusEffectKey.Disarm,
                    new EffectOriginKey(EffectOwnerKind.Item, 77, 1, "followup-test"), 2))));
            Assert.That(Followup(moved, budget, false), Is.Zero);
        }

        [Test]
        public void CoarseAttackRetainsDirectAlliedHarmAndKnockbackFollowupIsPrecise()
        {
            var input = Input(false);
            var actual = CombatCommandCalculator.AdvancePrepared(input, Move(input), Rules);
            var moved = AddTrait(CombatCommandComposition.Project(actual), EntityKey.Unit_Demon,
                new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            // 정밀 미리보기가 밀린 메인의 영혼 수집 입력(지속 공격력 = 캐릭터 공격력)을 검사하므로 고정물의 메인 지속 값을 캐릭터와 맞춘다.
            // World()의 메인은 공격력 15인데 지속 원본은 1이다. 이전에는 넉백 공격이 미리보기에 닿지 않아 드러나지 않았다.
            var main = moved.Characters.Single(unit => unit.CharacterKey == EntityKey.Unit_MainCharacter);
            moved = moved.WithWorld(moved.State, moved.Characters,
                new MainCharacterPersistentState(main.Health.CurrentHealth, main.Health.MaxHealth, main.Stats.AttackPower),
                moved.SoulCounts, moved.Souls, moved.EnvironmentBindings);
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            // 넉백 후속 공격은 정밀 미리보기로 실제 결과를 센다(사용자 결정 2026-11-10; 이전 기대값: 아군 전원 상한으로 0 이하, 미리보기 0회).
            Assert.That(Followup(moved, budget, true), Is.Positive);
            Assert.That(budget.PreviewCount, Is.EqualTo(1));
            // 아군 피격을 실제로 허용하는 캐논 정책으로 직접 전달의 피해 집계를 검사한다.
            var splash = RegenerationTurnTests.Regeneration(CombatCommandTestData.Input(category: CategoryKey.Cannon, friendly: true),
                EntityKey.Unit_Akimbo, 10, 100, 100);
            var splashBudget = new EnemyDecisionContext(splash, EnemyDecisionSettings.Default);
            var child = new EnemyDecisionContext(splash, EnemyDecisionSettings.Default);
            foreach (var _ in child.IndexSteps()) { }
            var attack = EnemyDecision.Attack(splash.ActorId, splash.Aim, default);
            using var assessment = CombatActionAssessment.Begin(splash, attack, Rules, splashBudget, allowPrecise: false);
            Drain(assessment);
            Assert.That(assessment.Admitted, Is.True);
            EnemyScoreFacts estimate = null;
            foreach (var _ in EnemyActionCandidates.EstimateAttackSteps(child, Actor(splash), attack,
                assessment.Facts, value => estimate = value)) { }
            Assert.That(estimate.AlliedHpLossPermille, Is.Positive);
            Assert.That(estimate.PossibleAlliedHpLossPermille, Is.GreaterThanOrEqualTo(estimate.AlliedHpLossPermille));
            Assert.That(EnemyUtilityScorer.AttackBenefit(estimate), Is.LessThanOrEqualTo(0));
            Assert.That(estimate.OpponentEliminations, Is.Zero);
            Assert.That(estimate.RestrictionDeltaPermille, Is.Zero);
            Assert.That(estimate.GuaranteedTerminal, Is.False);
        }

        [Test]
        public void MovedWorldOwnsRangeModifiers()
        {
            var source = RegenerationTurnTests.Regeneration(World(targetCell: new CellCoord(5, 2), range: 2), EntityKey.Unit_Demon, 10, 100, 100);
            foreach (bool sandstorm in new[] { false, true })
            {
                var input = sandstorm ? HighGroundMovementTests.TerrainInput(source,
                    new StaticTerrainFact(71, new CellCoord(3, 2), StaticTerrainKind.Sandstorm, true, new[] { Owner.Enemy })) : source;
                var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                using var work = AssessMove(input, budget);
                Assert.That(Followup(work.MovedFacts, budget, false), Is.EqualTo(sandstorm ? 0 : 400));
            }
        }

        [Test]
        public void CollapseRemovesActorFromConditionalFollowup()
        {
            var input = CliffFallTests.WithEnvironment(Input(), new StaticTerrainFact(71, new CellCoord(3, 2),
                StaticTerrainKind.WeakGround, true, new[] { Owner.Enemy }, entryThreshold: 2, entryCount: 1));
            using var work = AssessMove(input, new EnemyDecisionContext(input, EnemyDecisionSettings.Default));
            Assert.That(work.Admitted, Is.True);
            Assert.That(work.MovedFacts, Is.Null);
            var actual = CombatCommandCalculator.AdvancePrepared(input, Move(input), Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(Actor(CombatCommandComposition.Project(actual)).Area, Is.Not.EqualTo(CharacterRuntimeArea.Field));
        }

        [Test]
        public void ConditionalInputDoesNotGainPreciseKillRewardOrSpendAnotherPreview()
        {
            var input = Input(false);
            var budget = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = AssessMove(input, budget);
            var moved = CombatCommandTestData.With(work.MovedFacts, characters: work.MovedFacts.Characters.Select(unit => unit.Owner == Owner.Player
                ? unit.WithHealth(new HealthStateSnapshot(1, 100)) : unit).ToArray());
            Assert.That(Followup(moved, budget, false), Is.EqualTo(10));
            Assert.That(budget.PreviewCount, Is.EqualTo(1));
        }

        [Test]
        public void PartitionDecisionsAndBudgetsAgree()
        {
            EnemyDecision expected = null;
            int? previews = null, followupPreviews = null;
            foreach (int partition in new[] { 1, 2, 31 })
            {
                using var session = new EnemyDecisionSession(Input(), Rules, EnemyDecisionSettings.Default);
                EnemyUtilityDecisionTests.Drain(session, partition);
                expected ??= session.Decision;
                previews ??= session.Diagnostics.PreviewCalls;
                followupPreviews ??= session.Diagnostics.FollowupPreviewCalls;
                Assert.That(session.Decision, Is.EqualTo(expected));
                Assert.That(session.Diagnostics.RefinedMoves, Is.LessThanOrEqualTo(2));
                // 재생 무시(R-1)로 공격 후보도 정밀 평가를 받는다. 분할과 무관하게 같은 수이고 행위자 예산 안이어야 한다.
                Assert.That(session.Diagnostics.PreviewCalls, Is.EqualTo(previews.Value));
                Assert.That(session.Diagnostics.PreviewCalls, Is.LessThanOrEqualTo(EnemyDecisionSettings.Default.ActorPreviewLimit));
                // 정밀 이동의 후속 공격은 정밀 미리보기를 쓴다(R-1 전에는 조건부 투영이라 0이었다).
                Assert.That(session.Diagnostics.FollowupPreviewCalls, Is.EqualTo(followupPreviews.Value));
                TestContext.WriteLine($"partition={partition}; previews={session.Diagnostics.PreviewCalls}; refined={session.Diagnostics.RefinedMoves}; work={session.Diagnostics.WorkUnits}");
            }
        }

        private static CombatActionAssessment.Work AssessMove(CombatCommandInput input, EnemyDecisionContext budget, bool allowPrecise = true)
        {
            string before = Snapshot(input);
            var work = CombatActionAssessment.Begin(input, Move(input), Rules, budget, allowPrecise);
            Drain(work);
            Assert.That(Snapshot(input), Is.EqualTo(before));
            return work;
        }

        private static string Snapshot(CombatCommandInput input)
        {
            var unknown = input.State.Environment.Tiles.OfType<UnknownArrival>().ToArray();
            if (unknown.Length == 0) return EnemyActionAssessmentTests.Encode(input);
            // 공용 직렬화기는 시험 전용 타일을 모르므로 나머지 전체 입력과 해당 타일의 원본 값을 함께 비교한다.
            var known = input.State.With(environment: new MovementEnvironmentFacts(input.State.Environment.Tiles.Where(tile => tile is not UnknownArrival).ToArray()));
            var copy = input.WithWorld(known, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            return EnemyActionAssessmentTests.Encode(copy) + EnemyActionAssessmentTests.Encode(unknown.Select(tile =>
                new object[] { tile.Id, tile.Cell, tile.Active, tile.KnownTo }).ToArray());
        }

        private static void Drain(CombatActionAssessment.Work work)
        {
            for (int i = 0; i < 100000 && !work.IsComplete; i++) work.Step();
            Assert.That(work.IsComplete, Is.True);
        }

        internal static long Followup(CombatCommandInput moved, EnemyDecisionContext budget, bool preciseMove)
        {
            Assert.That(moved, Is.Not.Null);
            string before = EnemyActionAssessmentTests.Encode(moved);
            long benefit = 0;
            using var session = new EnemyDecisionSession(moved, Rules, budget.Settings);
            var steps = (IEnumerable<int>)FollowupMethod.Invoke(session, new object[] { moved, moved.ActorId,
                EnemyActionCandidateTests.MakeForecast(moved), Rules, budget, preciseMove, (Action<long>)(value => benefit = value) });
            int count = 0;
            foreach (var _ in steps) Assert.That(++count, Is.LessThan(100000));
            Assert.That(EnemyActionAssessmentTests.Encode(moved), Is.EqualTo(before));
            return benefit;
        }

        private static EnemyScoreFacts Score(CombatCommandInput input, EnemyScoreFacts facts, long followup, long loss = 0)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            EnemyThreatField threat = null;
            foreach (var _ in context.ThreatSteps(value => threat = value)) { }
            var hazards = EnemyActionCandidateTests.MakeForecast(input);
            using var candidates = new EnemyActionCandidates(context, Actor(input), hazards);
            while (!candidates.IsComplete) candidates.Step();
            return PositionFacts(Actor(input), facts, Move(input).Path, hazards, threat,
                candidates.Progress(Move(input)), followup, loss, CombatAssessmentGap.None, 0);
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) => input.Characters.Single(unit => unit.EntityId == input.ActorId);
        internal static CombatCommandInput ReplaceActor(CombatCommandInput input, Func<CharacterCombatFacts, CharacterCombatFacts> replace) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(unit => unit.EntityId == input.ActorId ? replace(unit) : unit).ToArray());
        internal static CombatCommandInput AddTrait(CombatCommandInput input, EntityKey key, TraitSpec spec, TraitStatusSnapshot status) =>
            CombatCommandTestData.With(input,
                characters: input.Characters.Select(unit => unit.CharacterKey == key
                    ? unit.With(stats: unit.Stats.With(traitStatuses: unit.Stats.TraitStatuses.ToArray().Append(status).ToArray())) : unit).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == key
                    ? new CombatAttackProfile(ImpactBoltTests.CopySpec(p.Definition, traits: p.Definition.Traits.ToArray().Append(spec).ToArray()), p.Method) : p).ToArray());

        // 정밀 평가가 실제 입력 검사를 지나도록 장애물 분류와 내용 프로필을 함께 넣는다(R-1 전에는 이 경로가 막혀 검사에 닿지 않았다).
        private static CombatCommandInput WithObstacle(CombatCommandInput input) =>
            EnemyIgnoredSourceTests.WithBarrels(input, EnemyIgnoredSourceTests.Barrel(90, new CellCoord(8, 4)));

        private sealed class UnknownArrival : MovementTileFact
        {
            internal UnknownArrival(CellCoord cell, Owner knownTo = Owner.Enemy)
                : base(91, cell, "unknown-followup-arrival", 0, true, new[] { knownTo }) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        internal static CombatCommandInput Input(bool regeneration = true, bool remote = false)
        {
            var input = World();
            return regeneration ? RegenerationTurnTests.Regeneration(input,
                remote ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Demon, 10, 100, 100) : input;
        }

        private static EnemyDecision Move(CombatCommandInput input) => EnemyDecision.Move(input.ActorId,
            new MovementPath(new[] { new CellCoord(0, 2), new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2) }), default);

        internal static CombatCommandInput World(int width = 9, int height = 5, CellCoord? actorCell = null,
            CellCoord? targetCell = null, int railX = -1, int range = 1)
        {
            var enemy = Character(11, EntityKey.Unit_Demon, Owner.Enemy, actorCell ?? new CellCoord(0, 2), 40, range);
            var main = Character(1, EntityKey.Unit_MainCharacter, Owner.Player, targetCell ?? new CellCoord(4, 2), 15, 3);
            var units = new[] { main, enemy };
            var profiles = new[] { Profile(enemy.CharacterKey, 40, range), Profile(main.CharacterKey, 15, 3) };
            var definitions = railX < 0 ? Array.Empty<StageGimmickSpec>() : new StageGimmickSpec[] { new RailwayGimmickSpec(2, 3) };
            var rails = railX < 0 ? Array.Empty<CellCoord>() : Enumerable.Range(0, height).Select(y => new CellCoord(railX, y)).ToArray();
            var state = new CombatCommandState(width, height, CombatFlowPhase.Automatic, Array.Empty<StructureCombatFacts>(),
                Array.Empty<ThornFieldCellSnapshot>(), new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2, completedPlayerTurnCount: 1, completedControllerTurnCount: 1));
            state = StageGimmickComposition.Initialize(state, definitions);
            var owned = new[] { new OwnedCharacterRecord(main.OwnedId.Value, main.CharacterKey, main.OwnershipRole.Value) };
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, enemy.EntityId, default, state, units, profiles,
                new MainCharacterPersistentState(100, 100, 1), owned, owned.ToDictionary(o => o.Id, _ => 0), SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(8, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, rails, definitions),
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 2);
        }

        private static CharacterCombatFacts Character(int id, EntityKey key, Owner owner, CellCoord cell, int attack, int range)
        {
            CharacterInstanceId? owned = owner == Owner.Player ? new CharacterInstanceId(id) : null;
            return new CharacterCombatFacts(new EntityId(id), owned, key, owner,
                owned.HasValue ? CharacterOwnershipRole.Main : (CharacterOwnershipRole?)null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(100, 100),
                new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 3, range, attack, 0, 100, Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Main)),
                null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
        }

        private static CombatAttackProfile Profile(EntityKey key, int attack, int range) =>
            new CombatAttackProfile(new UnitSpec(key, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace,
                range, CategoryKey.Main, attack, 0, 100, Array.Empty<TraitSpec>()),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
    }
}
