using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyUtilityAcceptanceMeasurements
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;

        /// <summary>
        /// 일정 동일성(분할 1/2/31의 진영 흔적, 공개 사실이 같고 난수 커서만 다른 입력의 첫 의도)을 확인하고,
        /// A8·X8·H8의 첫 결정 진단 기록(설계 6.9)을 남긴다. 옛 판(CaptureHoldScoresAndScheduleParity)의 대기 단언은
        /// 근간 개편 전의 전원 대기 상태를 굳힌 것이라 걷었다(적 AI 근간 개편 계획 R-6).
        /// </summary>
        [Test, Explicit("Requires an isolated Phase 7 diagnostic capture.")]
        public void CaptureScheduleParityAndDecisionRecords()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") != "phase-7-behavior")
                Assert.Ignore("Enable the Phase 7 diagnostic explicitly.");
            string directory = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            Assert.That(directory, Is.Not.Null.And.Not.Empty);
            using var records = File.CreateText(Path.Combine(directory, "first-decision-records.tsv"));
            using var parity = File.CreateText(Path.Combine(directory, "schedule-parity.tsv"));
            records.WriteLine("fixture\tseed\tactor\trow\tholdReason\taction\ttier\tlethal\tprecise\tattack\thazard\tplayerRisk\tprogress\tfollowup\tnextTurn\tbind\ttotal");
            parity.WriteLine("fixture\tseed\tsidePublications\ttraceHash\tfirstKind\tpreviews\twork\tgeneratedAttacks\tgeneratedMoves\tcapHits");
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                var input = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                string before = EnemyAiBenchmarkFixtures.Describe(input, true);
                var (baseline, first, diagnostics, publications) = Side(input, 1);
                Assert.That(Side(input, 2).trace, Is.EqualTo(baseline), $"{fixture}/{seed}, slice 2");
                Assert.That(Side(input, 31).trace, Is.EqualTo(baseline), $"{fixture}/{seed}, slice 31");
                var progress = input.State.EnvironmentProgress;
                var changed = CombatCommandTestData.With(input, state: input.State.With(itemRandomState: 9751,
                    environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 9187)));
                Assert.That(EnemyAiBenchmarkFixtures.Describe(changed, false), Is.EqualTo(EnemyAiBenchmarkFixtures.Describe(input, false)));
                Assert.That(First(changed, 31).decision, Is.EqualTo(first), $"{fixture}/{seed}, RNG cursor");
                parity.WriteLine(string.Join("\t", fixture, seed, publications,
                    EnemyAiBenchmarkFixtures.Hash(string.Join("\n", baseline)), first.Kind,
                    diagnostics.PreviewCalls, diagnostics.WorkUnits, diagnostics.GeneratedAttacks,
                    diagnostics.GeneratedMoves, diagnostics.CapHits));
                if (fixture is "A8" or "X8" or "H8")
                    CaptureRecords(input, fixture, seed, records);
                Assert.That(EnemyAiBenchmarkFixtures.Describe(input, true), Is.EqualTo(before));
            }
        }

        private static (EnemyDecision decision, EnemyDecisionDiagnostics diagnostics) First(CombatCommandInput input, int slice)
        {
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session, slice);
            return (session.Decision, session.Diagnostics);
        }

        private static (string[] trace, EnemyDecision first, EnemyDecisionDiagnostics diagnostics, int publications) Side(CombatCommandInput input, int slice)
        {
            var trace = new List<string>();
            EnemyDecision first = null;
            EnemyDecisionDiagnostics firstDiagnostics = null;
            var current = input;
            for (int step = 0; step < 128; step++)
            {
                using var session = new EnemyDecisionSession(current, Rules, EnemyDecisionSettings.Default);
                while (true)
                {
                    EnemyUtilityDecisionTests.Drain(session, slice);
                    var decision = session.Decision;
                    first ??= decision;
                    firstDiagnostics ??= session.Diagnostics;
                    var result = CombatCommandCalculator.AdvancePrepared(current, decision, Rules);
                    trace.Add($"{Intent(decision)}|{result.Reason}|{EnemyAiBaselineTests.Trace(result, decision.Kind == EnemyDecisionKind.Attack ? decision.Aim : null)}");
                    if (!result.Accepted)
                    {
                        Assert.That(decision.Kind, Is.Not.EqualTo(EnemyDecisionKind.EndEnemyPhase));
                        Assert.That(session.Diagnostics.RejectedAttempts, Is.LessThan(current.Characters.Count * 2));
                        session.RejectSelected();
                        continue;
                    }
                    current = CombatCommandComposition.Project(result);
                    if (result.Outcome.IsStageEnded || current.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                    {
                        var state = current.State;
                        string final = AttackPreviewParityTests.Serialize(new object[] { state.Turn, state.Structures,
                            state.Gimmicks, state.EnvironmentProgress, state.ItemRandomState, current.Characters, current.Main,
                            current.SoulCounts, current.Souls, current.ItemInventory, current.GoldBalance, current.FameBalance },
                            new HashSet<string>());
                        string tiles = string.Join("\n", state.Environment.Tiles.Select(tile =>
                            $"{tile.Id}|{tile.Cell}|{tile.RuleKey}|{tile.Active}|{tile.IsMine}|{string.Join(",", tile.KnownTo)}"));
                        trace.Add(EnemyAiBenchmarkFixtures.Hash(final + tiles));
                        return (trace.ToArray(), first, firstDiagnostics, step + 1);
                    }
                    break;
                }
            }
            throw new InvalidOperationException("The side exceeded the finite publication bound.");
        }

        private static string Intent(EnemyDecision decision) => decision.Kind switch
        {
            EnemyDecisionKind.Move => $"{decision.ActorId.Value}:{EnemyAiBenchmarkFixtures.Cells(decision.Path.Cells)}",
            EnemyDecisionKind.Attack => $"{decision.ActorId.Value}:{decision.Aim.Kind}:{decision.Aim.Cell}:{decision.Aim.Direction}",
            _ => "End"
        };

        // 첫 결정의 진단 기록(행위자별 대기 항목·대기 이유, 후보별 점수 항목)을 한 줄씩 쓴다. 판단은 바꾸지 않는다.
        private static void CaptureRecords(CombatCommandInput input, string fixture, int seed, StreamWriter rows)
        {
            var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default, diagnostics);
            EnemyUtilityDecisionTests.Drain(session);
            for (int i = 0; i < diagnostics.ActorRecordCount; i++)
            {
                var record = diagnostics.GetActorRecord(i);
                string reason = record.HoldReason?.ToString() ?? "-";
                rows.WriteLine(string.Join("\t", fixture, seed, record.ActorId.Value, "hold", reason, "Hold", record.HoldTier,
                    record.HoldLethal, "", Terms(record.HoldTerms)));
                foreach (var candidate in record.Candidates)
                    rows.WriteLine(string.Join("\t", fixture, seed, record.ActorId.Value, "candidate", reason, Intent(candidate.Action),
                        candidate.Tier, candidate.Lethal, candidate.Precise, Terms(candidate.Terms)));
            }
        }

        private static string Terms(EnemyScoreTerms terms) => string.Join("\t", terms.Attack, terms.Hazard, terms.PlayerRisk,
            terms.Progress, terms.Followup, terms.NextTurn, terms.Bind, terms.Total);
    }
}
