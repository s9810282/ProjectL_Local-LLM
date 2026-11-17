using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 AI 근간 개편 R-3: 기하 후속 공격(설계 6.4), 교환 모형(6.5), 확실한 치명 위험 거르기(계획 R-3 치명 위험 규칙), 대기 이유, 조정값.
    /// 기본 판은 9×5, 적(공격 40·사거리 1·이동 3)이 (0,2), 메인(체력 100·공격 15·사거리 3)이 (4,2)다.
    /// 메인에 닿는 공격 위치 중 이동 3으로 갈 수 있는 칸은 (3,2) 하나다.
    /// </summary>
    public sealed class EnemyExchangeModelTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;
        private static readonly CellCoord Origin = new CellCoord(0, 2);
        private static readonly CellCoord Target = new CellCoord(4, 2);
        private static readonly CellCoord AttackCell = new CellCoord(3, 2);
        private static readonly MovementPath Straight = new MovementPath(new[] { Origin, new CellCoord(1, 2), new CellCoord(2, 2), AttackCell });

        [Test]
        public void ExchangeWorkedExamples()
        {
            // 설계 6.5의 1127 B 2라운드 데몬: 이동 = 후속 300 + 다음 기회 300/2 + 진척 1칸 20 − 위협 400/2.
            var move = new EnemyScoreFacts(CombatAssessmentGap.None, engagementProgress: 1, subsequentAttackBenefit: 300,
                nextTurnStrike: 300, playerThreat: 400);
            var hold = new EnemyScoreFacts(CombatAssessmentGap.None, nextTurnStrike: 300);
            Assert.That((Score(move), Score(hold)), Is.EqualTo((270L, 150L)));

            // 처치당할 수 있는 칸의 목숨값은 500(죽음 교환 기준 50%)이다.
            Assert.That(Score(Lethal(opponentLoss: 100)), Is.EqualTo(-380L), "찌르기는 대기");
            Assert.That(Score(Lethal(opponentLoss: 600)), Is.EqualTo(120L), "큰 피해는 목숨과 바꾼다");
            Assert.That(Score(Lethal(opponentLoss: 1000, eliminations: 1)), Is.EqualTo(2520L), "처치는 받아들인다");

            // 목숨값은 남은 체력과 상관없다(다친 유닛이 더 겁먹는 역전이 없다).
            Assert.That(Score(Lethal(opponentLoss: 600, threat: 1000)), Is.EqualTo(Score(Lethal(opponentLoss: 600, threat: 300))));
            Assert.That(EnemyUtilityScorer.Score(move), Is.EqualTo(Score(move)), "기본 가중치 판");
        }

        [Test]
        public void AdjacentAttackPositionCreditedWithoutProjection()
        {
            // 선언된 공백(사냥꾼 = 무작위 결과)이 이동 투영을 막는다. 끝 칸이 공격 위치이면 기하 추정으로 후속 공격을 센다.
            var input = EnemyFollowupEstimateTests.AddTrait(EnemyFollowupEstimateTests.Input(regeneration: false),
                EntityKey.Unit_MainCharacter, new HunterTraitSpec(2, 3), new HunterTraitStatusSnapshot());
            var actor = Actor(input);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var move = Assess(input, EnemyDecision.Move(actor.EntityId, Straight, default), context);
            Assert.That(move.Admitted, Is.True);
            Assert.That(move.MovedFacts, Is.Null, "투영이 막혀야 한다");
            Assert.That(move.RouteGaps, Is.EqualTo(CombatAssessmentGap.None));

            var field = Field(input);
            Assert.That(field.IsAttackPosition(AttackCell), Is.True);
            Assert.That(EnemyFollowupGeometry.IsEligible(field, Straight, move.RouteGaps, false), Is.True);
            var moved = EnemyFollowupGeometry.Relocate(input, actor, Straight, input.State.Environment);
            Assert.That(moved, Is.Not.Null);
            Assert.That(Actor(moved).Anchor, Is.EqualTo(AttackCell));
            Assert.That(EnemyFollowupEstimateTests.Followup(moved, context, false), Is.EqualTo(400));

            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Endpoint, Is.EqualTo(AttackCell));
        }

        [Test]
        public void FollowupRespectsRules()
        {
            var input = EnemyFollowupEstimateTests.Input(regeneration: false);
            var actor = Actor(input);
            var field = Field(input);

            // 무장 해제: 끝 칸에서 공격할 수 없다.
            var disarmed = EnemyFollowupEstimateTests.ReplaceActor(input, unit => unit.With(debuffs: new CharacterDebuffState(
                disarm: new DisarmState(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Disarm, "main"), 2))));
            Assert.That(EnemyFollowupGeometry.Relocate(disarmed, Actor(disarmed), Straight, disarmed.State.Environment), Is.Null);

            // 경로 위 도착 원본(지뢰)이 남긴 공백이 있으면 추정하지 않는다.
            var mined = CliffFallTests.WithEnvironment(input, new MineSourceFact(71, new CellCoord(1, 2), 1, 10, true, new[] { Owner.Enemy }));
            using (var route = Assess(mined, EnemyDecision.Move(actor.EntityId, Straight, default),
                new EnemyDecisionContext(mined, EnemyDecisionSettings.Default)))
            {
                Assert.That(route.Admitted, Is.True);
                Assert.That(route.RouteGaps, Is.Not.EqualTo(CombatAssessmentGap.None));
                Assert.That(EnemyFollowupGeometry.IsEligible(Field(mined), Straight, route.RouteGaps, false), Is.False);
            }

            // 붕괴 착지와 공격 위치가 아닌 끝 칸에서는 추정하지 않는다.
            Assert.That(EnemyFollowupGeometry.IsEligible(field, Straight, CombatAssessmentGap.None, true), Is.False);
            var short_ = new MovementPath(new[] { Origin, new CellCoord(1, 2) });
            Assert.That(EnemyFollowupGeometry.IsEligible(field, short_, CombatAssessmentGap.None, false), Is.False);

            // 대상 값: 처치할 수 있는 대상(카우보이 체력 30)을 먼저 노린다.
            var cowboyCell = new CellCoord(3, 1);
            var two = EnemyEngagementFieldTests.WithUnit(input,
                CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, cowboyCell, Owner.Player).WithHealth(new HealthStateSnapshot(30, 100)));
            Assert.That(FollowupAim(two), Is.EqualTo(cowboyCell));

            // 끝 칸에서의 도발 의무: 도발하는 메인만 노릴 수 있다.
            var taunted = TraitCombatFixtures.Equip(two, EntityKey.Unit_MainCharacter, new TauntTraitSpec(8));
            Assert.That(FollowupAim(taunted), Is.EqualTo(Target));
        }

        [Test]
        public void MovedActorFactsComeFromMovementOwner()
        {
            var input = EnemyFollowupEstimateTests.AddTrait(EnemyFollowupEstimateTests.Input(regeneration: false),
                EntityKey.Unit_Demon, new StationaryFireTraitSpec(30), new StationaryFireTraitStatusSnapshot(false));
            var actor = Actor(input);
            var moved = CombatMovementCalculator.MovedActor(input, actor, Straight, input.State.Environment);
            Assert.That(moved.Anchor, Is.EqualTo(AttackCell));
            Assert.That(moved.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(MovedFlag(moved), Is.True);

            // 실제 이동 정산과 같은 사실이다.
            var actual = CombatCommandCalculator.AdvancePrepared(input, EnemyDecision.Move(actor.EntityId, Straight, default), Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            var settled = Actor(CombatCommandComposition.Project(actual).WithActor(actor.EntityId));
            Assert.That((settled.Anchor, settled.ActionUse, MovedFlag(settled)), Is.EqualTo((moved.Anchor, moved.ActionUse, MovedFlag(moved))));

            // 정지 사격 보너스는 이동한 행위자의 후속 공격에 붙지 않는다.
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            var relocated = EnemyFollowupGeometry.Relocate(input, actor, Straight, input.State.Environment);
            var teleported = EnemyFollowupEstimateTests.ReplaceActor(input, unit => unit.WithPlacement(unit.Area, unit.ReserveSlotIndex,
                AttackCell, unit.Rotation, new[] { AttackCell }));
            long afterMove = EnemyFollowupEstimateTests.Followup(relocated, context, true);
            long standing = EnemyFollowupEstimateTests.Followup(teleported, new EnemyDecisionContext(teleported, EnemyDecisionSettings.Default), true);
            Assert.That(afterMove, Is.EqualTo(400));
            Assert.That(standing, Is.GreaterThan(afterMove));
        }

        [TestCase("Railway")]
        [TestCase("Rockfall")]
        [TestCase("Collapse")]
        public void LethalArrivalNeverChosen(string source)
        {
            CombatCommandInput input;
            CellCoord lethal;
            if (source == "Collapse")
            {
                lethal = new CellCoord(3, 1);
                input = EnemyActionCandidateTests.RiderOnWeakGround(lethal, remaining: 1);
            }
            else
            {
                lethal = AttackCell;
                input = source == "Railway" ? Arena(rails: Column(3)) : Arena(rockfall: new[] { AttackCell });
                var forecast = EnemyActionCandidateTests.MakeForecast(input);
                Assert.That(forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), AttackCell).KnownHpLoss, Is.GreaterThanOrEqualTo(100));
                Assert.That(forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), Origin).KnownHpLoss, Is.Zero);
            }
            var rules = source == "Collapse" ? Array.Empty<IMovementTileRule>() : Rules;
            using var session = new EnemyDecisionSession(input, rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            if (session.Decision.Kind == EnemyDecisionKind.Move)
                Assert.That(session.Decision.Path.Endpoint, Is.Not.EqualTo(lethal));
            var result = CombatCommandCalculator.AdvancePrepared(input, session.Decision, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
        }

        [TestCase("Railway")]
        [TestCase("Rockfall")]
        public void SafeCrossingAllowed(string source)
        {
            // 지나가기만 하는 칸은 도착이 아니다. 끝 칸이 안전하면 그 경로는 후보이고 고를 수 있다.
            var input = source == "Railway" ? Arena(rails: Column(2)) : Arena(rockfall: new[] { new CellCoord(1, 2), new CellCoord(2, 2) });
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            Assert.That(forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), new CellCoord(2, 2)).KnownHpLoss, Is.GreaterThanOrEqualTo(100));
            Assert.That(forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), AttackCell).KnownHpLoss, Is.Zero);
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Endpoint, Is.EqualTo(AttackCell));
            Assert.That(session.Decision.Path.Cells, Does.Contain(new CellCoord(2, 2)));
        }

        [Test]
        public void LethalMineTransit_UsesSafeRoute()
        {
            // 곧은 길의 지뢰 두 칸(60 + 60)은 체력 100 이상이다. 같은 끝 칸은 돌아가는 길로 남는다.
            var mineCells = new[] { new CellCoord(1, 2), new CellCoord(2, 2) };
            var input = TraitTrailTestData.Environment(Arena(moveRange: 5), mineCells.Select((cell, i) =>
                new MineSourceFact(71 + i, cell, 1 + i, 60, true, new[] { Owner.Enemy })).ToArray());
            var actor = Actor(input);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, actor, EnemyActionCandidateTests.MakeForecast(input));
            while (!candidates.IsComplete) candidates.Step();
            var forcedRoute = CombatCommandCalculator.AdvancePrepared(input,
                EnemyDecision.Move(actor.EntityId, Straight, default), Rules);
            Assert.That(forcedRoute.Accepted, Is.True, forcedRoute.Reason);
            Assert.That(Actor(CombatCommandComposition.Project(forcedRoute).WithActor(actor.EntityId)).Health.IsDead, Is.True);
            // 탐색이 착지로 남긴 공격 위치 중에는 안전한 경로가 있다. 같은 끝 칸이면 곧은 길이 아니다.
            var attackRoutes = candidates.Moves.Where(move => candidates.Field.IsAttackPosition(move.Path.Endpoint)).ToArray();
            Assert.That(attackRoutes, Is.Not.Empty);
            Assert.That(attackRoutes.SelectMany(move => move.Path.Cells).Intersect(mineCells), Is.Empty);

            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            if (session.Decision.Kind == EnemyDecisionKind.Move)
                Assert.That(session.Decision.Path.Cells.Intersect(mineCells), Is.Empty);
            var result = CombatCommandCalculator.AdvancePrepared(input, session.Decision, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Actor(CombatCommandComposition.Project(result).WithActor(actor.EntityId)).Health.IsDead, Is.False);
        }

        [Test]
        public void UncertainHazardScoredNotFiltered()
        {
            // 가능 손실만 체력 이상이면 거르지 않고, 위험 점수로만 뺀다.
            var uncertain = new HazardExposure(10, 100, 0, 0, CombatAssessmentGap.UnknownPosition, Array.Empty<HazardCellFact>());
            Assert.That(EnemyActionCandidates.IsCertainLethal(100, uncertain, false), Is.False);
            Assert.That(EnemyActionCandidates.LethalRank(100, uncertain, false).terminal, Is.False);
            var certain = new HazardExposure(100, 100, 0, 0, CombatAssessmentGap.None, Array.Empty<HazardCellFact>());
            Assert.That(EnemyActionCandidates.IsCertainLethal(100, certain, false), Is.True);
            Assert.That(EnemyActionCandidates.IsCertainLethal(100, uncertain, true), Is.True, "붕괴 착지");

            var penalized = new EnemyScoreFacts(CombatAssessmentGap.None).WithPosition(CombatAssessmentGap.None,
                EnemyUtilityScorer.LossPermille(uncertain.PossibleHpLoss, 100, 100), 0, 0, 0);
            Assert.That(Score(penalized), Is.EqualTo(-4000L));
        }

        [Test]
        public void AllOptionsLethalKeepsLeastLethal()
        {
            // 제자리와 유일한 이동이 모두 열차 칸이다. 덜 치명적인 후보는 남아 대기와 점수로 겨룬다(후속 공격이 있어 이동한다).
            var doomed = Arena(width: 3, height: 1, actor: new CellCoord(0, 0), target: new CellCoord(2, 0),
                rails: new[] { new CellCoord(0, 0), new CellCoord(1, 0) });
            var forecast = EnemyActionCandidateTests.MakeForecast(doomed);
            Assert.That(forecast.Evaluate(Actor(doomed), Array.Empty<CellCoord>(), new CellCoord(0, 0)).KnownHpLoss, Is.GreaterThanOrEqualTo(100));
            using (var session = Session(doomed))
            {
                Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
                Assert.That(session.Decision.Path.Endpoint, Is.EqualTo(new CellCoord(1, 0)));
            }

            // 제자리가 안전하고 유일한 후보만 치명이면 대기하고 이유는 LethalOnly다.
            var safeHold = Arena(width: 3, height: 1, actor: new CellCoord(0, 0), target: new CellCoord(2, 0),
                rails: new[] { new CellCoord(1, 0) });
            using (var session = Session(safeHold))
            {
                Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
                Assert.That(session.Diagnostics.HoldCount, Is.EqualTo(1));
                Assert.That(session.Diagnostics.GetHold(0).Reason, Is.EqualTo(EnemyHoldReason.LethalOnly));
            }

            var collapse = EnemyActionCandidates.LethalRank(100, new HazardExposure(0, 0, 0, 0, CombatAssessmentGap.None,
                Array.Empty<HazardCellFact>()), true);
            var railway = EnemyActionCandidates.LethalRank(100, new HazardExposure(100, 100, 0, 0, CombatAssessmentGap.None,
                Array.Empty<HazardCellFact>()), false);
            // 경로 통과 피해(옛 가시 장판)는 없어졌다. 더 큰 확실한 손실로 "덜 치명적인 쪽이 앞" 순위를 확인한다(가시 폭탄 Phase 4 D7).
            var heavier = EnemyActionCandidates.LethalRank(100, new HazardExposure(110, 110, 0, 0, CombatAssessmentGap.None,
                Array.Empty<HazardCellFact>()), false);
            Assert.That(heavier.terminal && railway.terminal && collapse.terminal, Is.True);
            Assert.That(railway.CompareTo(heavier), Is.LessThan(0));
            Assert.That(railway.CompareTo(collapse), Is.EqualTo(0));
        }

        [Test]
        public void LethalFilterPriority()
        {
            // 도발 후보도 치명이면 뺀다: 도발하는 카우보이에게 가는 길의 끝 칸이 모두 열차 칸(x = 3)이다.
            var input = EnemyEngagementFieldTests.WithUnit(Arena(rails: Column(3)),
                CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, new CellCoord(5, 2), Owner.Player));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Cowboy, new TauntTraitSpec(8));
            using (var session = Session(input))
            {
                if (session.Decision.Kind == EnemyDecisionKind.Move)
                    Assert.That(session.Decision.Path.Endpoint.X, Is.Not.EqualTo(3));
            }

            // 이번 행동으로 적 승리가 확정되는 후보는 치명 칸에 서 있어도 거르지 않는다.
            var rail = Arena(rails: Column(3));
            var actor = Actor(rail);
            var hazards = EnemyActionCandidateTests.MakeForecast(rail);
            var win = new EnemyScoreFacts(CombatAssessmentGap.None, precise: true, projectedOutcome: StageOutcome.Defeat,
                postActionActor: CombatMovementCalculator.MoveTo(actor, AttackCell));
            Assert.That(win.GuaranteedTerminal, Is.True);
            Assert.That(EnemyDecisionSession.CandidateLethality(win, actor, AttackCell, hazards, false).lethal, Is.False);
            var notWin = new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment, precise: true, projectedOutcome: StageOutcome.Defeat,
                postActionActor: CombatMovementCalculator.MoveTo(actor, AttackCell));
            Assert.That(EnemyDecisionSession.CandidateLethality(notWin, actor, AttackCell, hazards, false).lethal, Is.True);
        }

        [Test]
        public void HoldReasonCodes()
        {
            // 움직일 수 없고 닿는 대상도 없다.
            using (var session = Session(Arena(moveRange: 0)))
            {
                Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
                Assert.That(session.Diagnostics.HoldCount, Is.EqualTo(1));
                Assert.That(session.Diagnostics.GetHold(0).Reason, Is.EqualTo(EnemyHoldReason.NoReachableTarget));
            }

            // 칸 상한에 걸린 필드는 "닿는 대상 없음"이 아니다(18 × 17 = 306칸 > 289).
            var large = Arena(width: 18, height: 17, actor: new CellCoord(0, 0), target: new CellCoord(17, 16));
            var truncated = EnemyEngagementFieldTests.Build(large);
            Assert.That(truncated.IsTruncated, Is.True);
            var empty = new List<EnemyDecisionSession.Option>();
            Assert.That(EnemyDecisionSession.HoldReason(false, false, false, 0, truncated, new CellCoord(0, 0), empty),
                Is.EqualTo(EnemyHoldReason.FieldTruncated));
            Assert.That(EnemyDecisionSession.HoldReason(false, false, false, 0, null, new CellCoord(0, 0), empty),
                Is.EqualTo(EnemyHoldReason.NoReachableTarget));

            var field = Field(EnemyFollowupEstimateTests.Input(regeneration: false));
            var move = EnemyDecision.Move(new EntityId(11), Straight, default);
            var plain = new EnemyDecisionSession.Option(move, (0, -1), new EnemyScoreFacts(CombatAssessmentGap.None), false, default);
            var capped = new EnemyDecisionSession.Option(move, (0, -1), new EnemyScoreFacts(CombatAssessmentGap.UnsupportedEffect,
                alliedHpLoss: 0, possibleAlliedHpLoss: 500), false, default);
            Assert.That(EnemyDecisionSession.HoldReason(false, false, false, 1, field, Origin, new List<EnemyDecisionSession.Option> { plain }),
                Is.EqualTo(EnemyHoldReason.ExchangeLoss));
            Assert.That(EnemyDecisionSession.HoldReason(false, false, false, 1, field, Origin, new List<EnemyDecisionSession.Option> { capped }),
                Is.EqualTo(EnemyHoldReason.WorldUncertainty));
            Assert.That(EnemyDecisionSession.HoldReason(true, false, false, 1, field, Origin, new List<EnemyDecisionSession.Option> { plain }),
                Is.EqualTo(EnemyHoldReason.WorldUncertainty), "실제 정산의 거절");
            Assert.That(EnemyDecisionSession.HoldReason(false, true, false, 1, field, Origin, new List<EnemyDecisionSession.Option> { plain }),
                Is.EqualTo(EnemyHoldReason.LethalOnly));

            // 후보 표본을 보관하지 않으면 대기 이유도 보관하지 않는다.
            using var silent = new EnemyDecisionSession(Arena(moveRange: 0), Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(silent);
            Assert.That(silent.Diagnostics.HoldCount, Is.Zero);
        }

        [Test]
        public void TuningValuesChangeDecisions()
        {
            // 체력 10인 적: (3,2)는 메인이 다음 턴에 처치할 수 있는 칸이다. 이동 = 후속 400 + 다음 기회 200 + 진척 60 − 목숨값.
            var fragile = Arena(actorHealth: 10);
            Assert.That(Decide(fragile, new EnemyDecisionSettings(deathTradeThresholdPercent: 50)).Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            var brave = Decide(fragile, new EnemyDecisionSettings(deathTradeThresholdPercent: 30));
            Assert.That(brave.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(brave.Path.Endpoint, Is.EqualTo(AttackCell));

            // 체력 100인 적: 위협 150은 처치가 아니다. 위협 회피를 4배로 올리면 다가가지 않는다.
            var sturdy = Arena();
            Assert.That(Decide(sturdy, EnemyDecisionSettings.Default).Path?.Endpoint, Is.EqualTo(AttackCell));
            Assert.That(Decide(sturdy, new EnemyDecisionSettings(threatAversionPercent: 400)).Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));

            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(deathTradeThresholdPercent: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(threatAversionPercent: EnemyDecisionSettings.MaxTuningPercent + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDecisionSettings(nextTurnApproachPercent: -5));
        }

        [Test]
        public void NextTurnStrikeFollowsHaboobDesignation()
        {
            // 다음 기회 경계에서 지정할 수 있을 때만 다음 기회 값을 준다. 구름이 제자리(0,2)를 덮으면 구름 밖 메인을 지정할 수 없다.
            var clear = Arena();
            Assert.That(NextTurn(clear, Origin), Is.EqualTo(400), "닿는 거리(3 ≤ 이동 3)");

            var input = Arena(haboob: new HaboobGimmickSpec(footprintOffsets: new[] { CellCoord.Zero }, initialAnchor: Origin));
            var hazards = EnemyActionCandidateTests.MakeForecast(input);
            var field = Field(input);
            foreach (var endpoint in new[] { Origin, AttackCell })
            {
                Assert.That(field.TryGetBestTarget(endpoint, out var best), Is.True, $"{endpoint}");
                bool? designatable = hazards.CanDesignateAt(endpoint, Target, hazards.LastBoundaryCount);
                Assert.That(NextTurn(input, endpoint), Is.EqualTo(designatable == true ? field.TargetValue(best) : 0), $"{endpoint}");
            }
            Assert.That(hazards.CanDesignateAt(Origin, Target, hazards.LastBoundaryCount), Is.Not.True);
            Assert.That(NextTurn(input, Origin), Is.Zero, "필드가 닿는다고 해도 다음 경계에 지정할 수 없으면 값이 없다");
        }

        private static long Score(EnemyScoreFacts facts) => EnemyUtilityScorer.Score(facts, EnemyScoreWeights.Default);

        private static long NextTurn(CombatCommandInput input, CellCoord endpoint)
        {
            IReadOnlyDictionary<(int kind, int id), EnemyDecisionContext.HostileTarget> targets = null;
            foreach (var _ in new EnemyDecisionContext(input, EnemyDecisionSettings.Default)
                .TargetSteps(value => targets = value)) { }
            return EnemyActionCandidates.NextTurnStrike(Field(input), Actor(input), endpoint, targets,
                EnemyActionCandidateTests.MakeForecast(input));
        }

        private static EnemyScoreFacts Lethal(long opponentLoss, int eliminations = 0, long threat = 1000) =>
            new EnemyScoreFacts(CombatAssessmentGap.None, opponentHpLoss: opponentLoss, opponentEliminations: eliminations,
                engagementProgress: 1, playerThreat: threat, playerThreatLethal: true);

        private static CharacterCombatFacts Actor(CombatCommandInput input) => input.Characters.Single(unit => unit.EntityId == input.ActorId);

        private static bool MovedFlag(CharacterCombatFacts unit) =>
            unit.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().Any(status => status.MovedThisOwnTurn);

        private static CombatActionAssessment.Work Assess(CombatCommandInput input, EnemyDecision decision, EnemyDecisionContext context)
        {
            var work = CombatActionAssessment.Begin(input, decision, Rules, context);
            for (int i = 0; i < 1000000 && !work.IsComplete; i++) work.Step();
            Assert.That(work.IsComplete, Is.True);
            return work;
        }

        private static EnemyEngagementField Field(CombatCommandInput input)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, Actor(input), EnemyActionCandidateTests.MakeForecast(input));
            while (!candidates.IsComplete) candidates.Step();
            Assert.That(candidates.Field, Is.Not.Null);
            return candidates.Field;
        }

        private static CellCoord? FollowupAim(CombatCommandInput input)
        {
            var actor = Actor(input);
            var moved = EnemyFollowupGeometry.Relocate(input, actor, Straight, input.State.Environment);
            Assert.That(moved, Is.Not.Null);
            var context = new EnemyDecisionContext(moved, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, Actor(moved), EnemyActionCandidateTests.MakeForecast(moved), followupOnly: true);
            while (!candidates.IsComplete) candidates.Step();
            Assert.That(candidates.Attacks.Count, Is.EqualTo(1));
            return candidates.Attacks[0].Aim.Cell;
        }

        private static EnemyDecisionSession Session(CombatCommandInput input)
        {
            var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default,
                new EnemyDecisionDiagnostics(captureAlternatives: true));
            EnemyUtilityDecisionTests.Drain(session);
            return session;
        }

        private static EnemyDecision Decide(CombatCommandInput input, EnemyDecisionSettings settings)
        {
            using var session = new EnemyDecisionSession(input, Rules, settings);
            EnemyUtilityDecisionTests.Drain(session);
            return session.Decision;
        }

        private static CellCoord[] Column(int x, int height = 5) => Enumerable.Range(0, height).Select(y => new CellCoord(x, y)).ToArray();

        /// <summary>
        /// 기본 판(<see cref="EnemyFollowupEstimateTests.World"/>와 같은 유닛·턴)에 열차 칸·낙석 경고 칸(다음 경계에 최대 체력 100%)·
        /// 하붑을 마음대로 놓는다. 열차는 다음 경계(2)에 모든 열차 칸을 지난다.
        /// </summary>
        internal static CombatCommandInput Arena(int width = 9, int height = 5, CellCoord? actor = null, CellCoord? target = null,
            CellCoord[] rails = null, CellCoord[] rockfall = null, HaboobGimmickSpec haboob = null,
            int moveRange = 3, int actorHealth = 100, (int id, CellCoord cell)[] allies = null)
        {
            var enemy = Unit(11, EntityKey.Unit_Demon, Owner.Enemy, actor ?? Origin, 40, 1, moveRange)
                .WithHealth(new HealthStateSnapshot(actorHealth, 100));
            var main = Unit(1, EntityKey.Unit_MainCharacter, Owner.Player, target ?? Target, 15, 3, 3);
            var units = new List<CharacterCombatFacts> { main, enemy };
            // 같은 프로필의 적 유닛을 더 놓는다(행위자 순서 시험). 행위자는 늘 11번이다.
            foreach (var ally in allies ?? Array.Empty<(int id, CellCoord cell)>())
                units.Add(Unit(ally.id, EntityKey.Unit_Demon, Owner.Enemy, ally.cell, 40, 1, moveRange));
            var profiles = new[] { Profile(enemy.CharacterKey, 40, 1), Profile(main.CharacterKey, 15, 3) };
            var definitions = new List<StageGimmickSpec>();
            if (rails != null) definitions.Add(new RailwayGimmickSpec(2, 3));
            int rockfallIndex = definitions.Count;
            if (rockfall != null) definitions.Add(new RockfallGimmickSpec(maximumHealthLossPercent: 100, bindAffectedSideTurns: 1));
            if (haboob != null) definitions.Add(haboob);
            var state = new CombatCommandState(width, height, CombatFlowPhase.Automatic, Array.Empty<StructureCombatFacts>(),
                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2, completedPlayerTurnCount: 1, completedControllerTurnCount: 1));
            state = StageGimmickComposition.Initialize(state, definitions.ToArray());
            if (rockfall != null)
                state = state.With(gimmicks: state.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(rockfallIndex + 1),
                    RockfallGimmickPhase.Warning, 2, rockfall)));
            var owned = new[] { new OwnedCharacterRecord(main.OwnedId.Value, main.CharacterKey, main.OwnershipRole.Value) };
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, enemy.EntityId, default, state, units.ToArray(), profiles,
                new MainCharacterPersistentState(100, 100, 1), owned, owned.ToDictionary(o => o.Id, _ => 0), SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(8, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, rails ?? Array.Empty<CellCoord>(),
                    definitions.ToArray()),
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 2);
        }

        private static CharacterCombatFacts Unit(int id, EntityKey key, Owner owner, CellCoord cell, int attack, int range, int moveRange)
        {
            CharacterInstanceId? owned = owner == Owner.Player ? new CharacterInstanceId(id) : null;
            return new CharacterCombatFacts(new EntityId(id), owned, key, owner,
                owned.HasValue ? CharacterOwnershipRole.Main : (CharacterOwnershipRole?)null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(100, 100),
                new UnitStatsSnapshot(MoveMethod.LiftAndPlace, moveRange, range, attack, 0, 100, Array.Empty<TraitStatusSnapshot>(),
                    CategorySnapshot.CreateStateless(CategoryKey.Main)),
                null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
        }

        private static CombatAttackProfile Profile(EntityKey key, int attack, int range) =>
            new CombatAttackProfile(new UnitSpec(key, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace,
                range, CategoryKey.Main, attack, 0, 100, Array.Empty<TraitSpec>()),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
    }
}
