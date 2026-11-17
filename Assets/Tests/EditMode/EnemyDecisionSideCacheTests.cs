using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 진영 캐시(적 AI 근간 개편 설계 6.8)와 진단 기록(6.9) 시험이다(계획 R-5).
    /// 캐시는 결정을 바꾸지 않아야 하고, 읽기 집합의 사실이 하나라도 바뀌면 다시 계산해야 하며, 다른 진영·전투에는 쓰이지 않아야 한다.
    /// </summary>
    public sealed class EnemyDecisionSideCacheTests
    {
        private static readonly int[] Partitions = { 1, 2, 31 };

        /// <summary>
        /// 대표 fixture와 시드의 적 진영을 캐시 없이·있게 돌려 분할 1/2/31마다 같은 흔적인지 본다.
        /// 전체 회귀에서는 기준 fixture와 시드의 모든 조합을 검사한다.
        /// 흔적: 게시한 행동(종류·행위자·조준·경로·도발 근원), 대기·진영 종료, 결과 사건·상태, 진단 기록의 점수 항목과 대기 이유.
        /// 작업·확장·미리보기 횟수·캐시 적중·시간은 비교하지 않는다(계획 R-5 비교 범위).
        /// </summary>
        [Test]
        public void CacheOnOffIdenticalTraces()
        {
            int forecastHits = 0, threatHits = 0, decisions = 0;
            var reasons = new SortedDictionary<string, int>();
            bool fullRegression = EnemyAiBenchmarkFixtures.FullRegressionEnabled;
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                if (!fullRegression && !EnemyAiBenchmarkFixtures.IsRepresentative(fixture, seed)) continue;
                var input = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                foreach (int partition in Partitions)
                {
                    var off = PlaySide(input, partition, null);
                    var cache = new EnemyDecisionSideCache();
                    var on = PlaySide(input, partition, cache);
                    Assert.That(on.Count, Is.EqualTo(off.Count), $"{fixture}/{seed}/{partition}: 결정 수");
                    for (int i = 0; i < off.Count; i++)
                        Assert.That(on[i], Is.EqualTo(off[i]), $"{fixture}/{seed}/{partition}: 결정 {i + 1}");
                    forecastHits += cache.ForecastHits;
                    threatHits += cache.ThreatHits;
                    foreach (var pair in cache.MissReasons) reasons[pair.Key] = reasons.TryGetValue(pair.Key, out int known) ? known + pair.Value : pair.Value;
                    decisions += off.Count;
                }
            }
            TestContext.WriteLine($"decisions={decisions} forecastHits={forecastHits} threatHits={threatHits} misses=[{string.Join(" ", reasons.Select(p => $"{p.Key}={p.Value}"))}]");
            // 캐시가 실제로 쓰였는지 본다(쓰이지 않으면 위 비교는 의미가 없다).
            Assert.That(forecastHits, Is.Positive, "위험 예보를 한 번도 다시 쓰지 않았습니다.");
            Assert.That(threatHits, Is.Positive, "위협 필드를 한 번도 다시 쓰지 않았습니다.");
        }

        /// <summary>
        /// 각 캐시의 읽기 집합에서 사실 하나를 바꾸면 다시 계산해야 한다(일치하지 않음). 바꾸지 않은 복사본은 일치한다.
        /// 읽지 않는 사실은 바꿔도 일치한다(예: 스플래시·관통 플레이어가 없으면 위협 필드는 적 위치를 읽지 않는다, 예보는 메인·목표 대상만 읽는다).
        /// </summary>
        [Test]
        public void DigestInvalidation()
        {
            var input = EnemyExchangeModelTests.Arena(rockfall: new[] { new CellCoord(6, 0) }, allies: new[] { (12, new CellCoord(0, 4)) });
            var main = new EntityId(1);
            var actor = new EntityId(11);
            var ally = new EntityId(12);
            var copy = CombatCommandTestData.With(input, characters: input.Characters.ToArray(), profiles: input.Profiles.ToArray());
            var mainHurt = Change(input, main, c => c.WithHealth(new HealthStateSnapshot(60, 100)));
            var mainDead = Change(input, main, c => CombatCommandCalculator.Hurt(c, 100));
            var mainPushed = Change(input, main, c => CombatCommandTestData.With(c, position: c.Anchor.Value + CellCoord.Up));
            var mainDisarmed = Change(input, main, Disarm);
            var mainRange = Change(input, main, c => CombatCommandTestData.With(c, appointRange: c.Stats.AppointRange + 1));
            var allyMoved = Change(input, ally, c => CombatCommandTestData.With(c, position: c.Anchor.Value + CellCoord.Right));
            var allyDead = Change(input, ally, c => CombatCommandCalculator.Hurt(c, 100));
            var gimmick = CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 2, new[] { new CellCoord(6, 1) }))));
            var environment = WithTiles(input, WallStateLifecycleTests.Wall(71, new CellCoord(2, 1)));

            // 위협 필드: 플레이어 사실·생존, 전투 상태(기믹·환경). 스플래시·관통 플레이어가 없으면 적 위치는 읽지 않는다.
            var threat = EnemyThreatField.ReadSet.Capture(input);
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(copy)), Is.True, "threat: copy");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(mainHurt)), Is.False, "threat: player HP");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(mainDead)), Is.False, "threat: player death");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(mainPushed)), Is.False, "threat: player forced move");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(mainDisarmed)), Is.False, "threat: player disarm");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(mainRange)), Is.False, "threat: player range");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(allyDead)), Is.False, "threat: enemy death");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(gimmick)), Is.False, "threat: gimmick");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(environment)), Is.False, "threat: visible environment");
            Assert.That(threat.Matches(EnemyThreatField.ReadSet.Capture(allyMoved)), Is.True, "threat: enemy move without splash/pierce");
            // 스플래시 플레이어가 있으면 적 위치(스플래시 중심 자격)를 읽는다.
            var splash = WithSplashPlayer(input);
            var splashThreat = EnemyThreatField.ReadSet.Capture(splash);
            Assert.That(splashThreat.Matches(EnemyThreatField.ReadSet.Capture(Change(splash, ally,
                c => CombatCommandTestData.With(c, position: c.Anchor.Value + CellCoord.Right)))), Is.False, "threat: enemy move with splash");

            // 위험 예보: 메인·목표 대상 사실, 기믹. 다른 유닛 위치와 환경은 읽지 않는다.
            var forecast = StageHazardForecast.ReadSet.Capture(input);
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(copy)), Is.True, "forecast: copy");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(mainHurt)), Is.False, "forecast: main HP");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(mainDead)), Is.False, "forecast: main death");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(mainPushed)), Is.False, "forecast: main forced move");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(gimmick)), Is.False, "forecast: gimmick");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(allyMoved)), Is.True, "forecast: enemy move");
            Assert.That(forecast.Matches(StageHazardForecast.ReadSet.Capture(environment)), Is.True, "forecast: environment");
        }

        /// <summary>새 진영·새 전투에서는 이전 항목을 쓰지 않는다. 같은 진영의 같은 입력은 다시 쓴다.</summary>
        [Test]
        public void CacheScopedToSide()
        {
            var input = EnemyExchangeModelTests.Arena(rails: new[] { new CellCoord(4, 0), new CellCoord(4, 1) });
            var cache = new EnemyDecisionSideCache();
            Decide(input, cache);
            Assert.That(cache.ForecastMisses, Is.EqualTo(1));
            Assert.That(cache.ThreatMisses, Is.EqualTo(1));
            Decide(input, cache);
            Assert.That(cache.ForecastHits, Is.EqualTo(1), "같은 진영의 같은 입력");
            Assert.That(cache.ThreatHits, Is.EqualTo(1));

            var turn = input.State.Turn;
            var nextSide = CombatCommandTestData.With(input, state: input.State.With(turn: new CombatTurnFacts(StagePhase.Combat,
                controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: turn.ControllerTurnSequence + 2,
                completedPlayerTurnCount: turn.CompletedPlayerTurnCount, completedControllerTurnCount: turn.CompletedControllerTurnCount)));
            Decide(nextSide, cache);
            Assert.That(cache.ScopeResets, Is.EqualTo(1), "새 진영");
            Assert.That((cache.ForecastHits, cache.ThreatHits), Is.EqualTo((1, 1)), "새 진영은 다시 쓰지 않는다");

            var otherBattle = WithBattle(nextSide, 7);
            Decide(otherBattle, cache);
            Assert.That(cache.ScopeResets, Is.EqualTo(2), "새 전투");
            Assert.That((cache.ForecastHits, cache.ThreatHits), Is.EqualTo((1, 1)), "새 전투는 다시 쓰지 않는다");
        }

        /// <summary>진단 기록은 평가한 행위자마다 모든 후보의 점수 항목과 대기 항목을 남기고, 대기한 행위자에게는 이유를 남긴다.</summary>
        [Test]
        public void DiagnosticsRecordTerms()
        {
            // 행위자 11은 이동하면 칠 수 있고, 12는 레일 너머 막혀 대기한다.
            var input = EnemyExchangeModelTests.Arena(allies: new[] { (12, new CellCoord(0, 0)) });
            var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
            using var session = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default, diagnostics);
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(diagnostics.ActorRecordCount, Is.Positive, "평가 기록이 없습니다.");
            for (int i = 0; i < diagnostics.ActorRecordCount; i++)
            {
                var record = diagnostics.GetActorRecord(i);
                Assert.That(record.HoldTerms.Total, Is.EqualTo(record.HoldTerms.Attack + record.HoldTerms.Hazard + record.HoldTerms.PlayerRisk +
                    record.HoldTerms.Progress + record.HoldTerms.Followup + record.HoldTerms.NextTurn + record.HoldTerms.Bind));
                foreach (var candidate in record.Candidates)
                {
                    Assert.That(candidate.Action.ActorId, Is.EqualTo(record.ActorId));
                    Assert.That(candidate.Terms.PlayerRisk, Is.LessThanOrEqualTo(0));
                    Assert.That(candidate.Terms.Hazard, Is.LessThanOrEqualTo(0));
                    Assert.That(candidate.Terms.Bind, Is.LessThanOrEqualTo(0));
                }
            }
            // 행동을 고른 행위자의 기록에는 그 행동이 후보로 있고 대기 이유가 없다.
            var selected = session.Decision;
            Assert.That(selected.Kind, Is.Not.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            var acted = Enumerable.Range(0, diagnostics.ActorRecordCount).Select(diagnostics.GetActorRecord).Last();
            Assert.That(acted.ActorId, Is.EqualTo(selected.ActorId));
            Assert.That(acted.HoldReason, Is.Null);
            Assert.That(acted.Candidates.Any(c => SameAction(c.Action, selected)), Is.True, "고른 행동이 후보 기록에 없습니다.");
            Assert.That(acted.Candidates.Select(c => c.Terms.Total).Max(), Is.GreaterThan(acted.HoldTerms.Total));

            // 벽에 갇혀 대기한 행위자에게는 이유가 있다(대기 기록과 같은 값).
            var blocked = CliffFallTests.WithEnvironment(EnemyExchangeModelTests.Arena(), new[] { new CellCoord(0, 1), new CellCoord(1, 1),
                new CellCoord(1, 2), new CellCoord(1, 3), new CellCoord(0, 3) }.Select((cell, i) => (MovementTileFact)WallStateLifecycleTests.Wall(300 + i, cell)).ToArray());
            var held = new EnemyDecisionDiagnostics(captureAlternatives: true);
            using var holdSession = new EnemyDecisionSession(blocked, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default, held);
            EnemyUtilityDecisionTests.Drain(holdSession);
            Assert.That(holdSession.Decision.Kind, Is.EqualTo(EnemyDecisionKind.EndEnemyPhase));
            Assert.That(held.ActorRecordCount, Is.EqualTo(held.HoldCount));
            for (int i = 0; i < held.ActorRecordCount; i++)
            {
                var record = held.GetActorRecord(i);
                Assert.That(record.HoldReason, Is.EqualTo(held.GetHold(i).Reason));
                Assert.That(record.ActorId, Is.EqualTo(held.GetHold(i).ActorId));
            }
        }

        // 진영 하나를 끝까지 결정·정산한다. 운영 준비 경로처럼 세션이 고르고 AdvancePrepared가 정산하며, 거절되면 같은 세션의 대안으로 넘어간다.
        private static List<string> PlaySide(CombatCommandInput input, int partition, EnemyDecisionSideCache cache)
        {
            var lines = new List<string>();
            var current = input;
            for (int count = 0; current.State.Turn.ControllerPhase == CombatControllerPhase.Enemy; count++)
            {
                Assert.That(count, Is.LessThan(64), "적 진영이 끝나지 않았습니다.");
                var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
                var (result, decision) = Decide(current, partition, diagnostics, cache);
                Assert.That(result.Accepted, Is.True, result.Reason);
                lines.Add(Line(result, decision, diagnostics, fullState: partition == 1));
                if (result.Outcome.IsStageEnded) break;
                current = CombatCommandComposition.Project(result);
            }
            return lines;
        }

        private static (CombatCommandTransition, EnemyDecision) Decide(CombatCommandInput input, int partition, EnemyDecisionDiagnostics diagnostics,
            EnemyDecisionSideCache cache)
        {
            if (!CombatCommandCalculator.RequiresEnemyDecision(input))
                return (CombatCommandCalculator.AdvancePrepared(input, null, EnemyAiBenchmarkFixtures.Rules), null);
            using var session = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default, diagnostics, cache);
            while (true)
            {
                EnemyDecisionSessionTests.Drain(session, partition);
                var decision = session.Decision;
                var result = CombatCommandCalculator.AdvancePrepared(input, decision, EnemyAiBenchmarkFixtures.Rules);
                if (result.Accepted || decision.Kind == EnemyDecisionKind.EndEnemyPhase) return (result, decision);
                session.RejectSelected();
            }
        }

        private static void Decide(CombatCommandInput input, EnemyDecisionSideCache cache)
        {
            using var session = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default, cache);
            EnemyUtilityDecisionTests.Drain(session);
        }

        // 결정 하나의 흔적이다. 결과 상태는 분할 1에서 전체를 직렬화하고, 다른 분할은 유닛·구조물·환경 요약으로 비교한다(시험 시간).
        private static string Line(CombatCommandTransition result, EnemyDecision decision, EnemyDecisionDiagnostics diagnostics, bool fullState)
        {
            var text = new StringBuilder();
            text.Append(EnemyAiBaselineTests.Trace(result, decision?.Kind == EnemyDecisionKind.Attack ? decision.Aim : (AttackAim?)null));
            text.Append("\n").Append(Record(diagnostics));
            if (fullState)
                text.Append("\n").Append(AttackPreviewParityTests.Serialize(new object[] { result.Characters, result.FinalState, result.Events,
                    result.Outcome }, new HashSet<string>()));
            else
            {
                foreach (var final in result.Characters.Characters.Select(change => change.Final).Concat(result.Characters.Created))
                    text.Append($"\nchar {final.EntityId.Value} {final.Area} {final.Anchor} hp={final.Health.CurrentHealth} use={final.ActionUse} " +
                        $"statuses={final.StatusEffects.Entries.Count}");
                text.Append($"\nenv={result.FinalState.Environment.Tiles.Count(t => t.Active)} structures=" +
                    string.Join(",", result.FinalState.Structures.Select(s => $"{s.EntityId.Value}:{s.Health.CurrentHealth}")) +
                    $" outcome={result.Outcome.Outcome}");
            }
            return text.ToString();
        }

        /// <summary>진단 기록의 점수 항목과 대기 이유를 글자로 적는다(흔적 비교용).</summary>
        internal static string Record(EnemyDecisionDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            for (int i = 0; i < diagnostics.ActorRecordCount; i++)
            {
                var record = diagnostics.GetActorRecord(i);
                text.Append($"actor={record.ActorId.Value} hold=[{record.HoldTerms}] tier={record.HoldTier} lethal={record.HoldLethal} reason={record.HoldReason?.ToString() ?? "-"}\n");
                foreach (var candidate in record.Candidates)
                    text.Append($"  {Describe(candidate.Action)} [{candidate.Terms}] tier={candidate.Tier} lethal={candidate.Lethal} precise={candidate.Precise}\n");
            }
            for (int i = 0; i < diagnostics.HoldCount; i++)
                text.Append($"hold={diagnostics.GetHold(i).ActorId.Value}:{diagnostics.GetHold(i).Reason}\n");
            return text.ToString();
        }

        internal static string Describe(EnemyDecision action) => action.Kind switch
        {
            EnemyDecisionKind.Attack => $"attack {action.Aim.Kind}:{OptionalCell(action.Aim.Cell)}:{action.Aim.Direction} taunt={action.TauntSourceId.Value}",
            EnemyDecisionKind.Move => $"move {EnemyAiBenchmarkFixtures.Cells(action.Path.Cells)} taunt={action.TauntSourceId.Value}",
            _ => action.Kind.ToString()
        };

        private static string OptionalCell(CellCoord? cell) => cell.HasValue ? EnemyAiBenchmarkFixtures.Cell(cell.Value) : "-";

        private static bool SameAction(EnemyDecision left, EnemyDecision right) => Describe(left) == Describe(right) && left.ActorId == right.ActorId;

        private static CombatCommandInput Change(CombatCommandInput input, EntityId id, Func<CharacterCombatFacts, CharacterCombatFacts> change) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == id ? change(c) : c).ToArray());

        // 기믹 상태를 지키고 환경 원본만 바꾼다(다음 원본 번호는 새 원본보다 크게).
        private static CombatCommandInput WithTiles(CombatCommandInput input, params MovementTileFact[] tiles)
        {
            var progress = input.State.EnvironmentProgress;
            return CombatCommandTestData.With(input, state: input.State.With(environment: new MovementEnvironmentFacts(tiles),
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, 1000, progress.RandomState)));
        }

        private static CharacterCombatFacts Disarm(CharacterCombatFacts unit) => unit.With(debuffs: new CharacterDebuffState(
            disarm: new DisarmState(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Disarm, "test"), 2)));

        // 스플래시 지정 방식을 쓰는 플레이어 유닛을 하나 더 놓는다.
        private static CombatCommandInput WithSplashPlayer(CombatCommandInput input)
        {
            var unit = CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, new CellCoord(8, 4), Owner.Player);
            var added = EnemyEngagementFieldTests.WithUnit(input, unit);
            return CombatCommandTestData.With(added, profiles: added.Profiles.Select(p => p.Definition.Key != EntityKey.Unit_Cowboy ? p
                : new CombatAttackProfile(p.Definition, new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                    TargetingSplash.Cross, 50))).ToArray());
        }

        private static CombatCommandInput WithBattle(CombatCommandInput i, long battleInstanceId) => new CombatCommandInput(i.Number, i.Kind,
            i.ActorId, i.Aim, i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: i.Battle,
            processingKind: i.ProcessingKind, battleInstanceId: battleInstanceId, environmentBindings: i.EnvironmentBindings,
            expectedBattleInstanceId: battleInstanceId, expectedControllerTurnSequence: i.State.Turn.ControllerTurnSequence);
    }
}
