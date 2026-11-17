using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>
    /// 운영 전투 스테이지에서 적 AI가 실제로 무엇을 하는지 잰다(적 AI 근간 개편 계획 R-0).
    /// 운영 스테이지 정의와 시험 로스터로 전투를 세우고, 플레이어 각본과 로드아웃마다 몇 라운드를 돌려 지표를 남긴다.
    /// 판단은 바꾸지 않는다. 적 결정은 운영 준비 경로와 같은 Utility 세션(<see cref="CombatCommandCalculator.AdvanceUtility(CombatCommandInput, IReadOnlyList{IMovementTileRule})"/>)으로 얻고 운영 게시기로 게시한다.
    /// 결과는 <c>PROJECTL_ENEMY_AI_RAW</c> 폴더의 TSV 셋과 시험 출력 요약이다.
    /// </summary>
    public sealed class EnemyStageBehaviorCorpus
    {
        private static readonly IMovementTileRule[] Rules =
        {
            new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule()
        };
        private static readonly CellCoord[] Directions = { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
        private const int Rounds = 5;
        // 시험 로스터의 메인 캐릭터다(CoreIntegrationTestData 로드아웃).
        private const EntityKey MainKey = EntityKey.Unit_Akimbo;
        // 운영 스테이지는 시험 맵의 노드 2에 이 키로 등록한다. 실제 정의는 ProductionStage가 운영 DB에서 읽는다.
        private const int RegisteredStageKey = 1101;

        private enum Script
        {
            HoldBack,
            Advance,
            Spread
        }

        private enum Loadout
        {
            None,
            Artifact,
            Item
        }

        [Test, Timeout(900000), Explicit("Production-stage enemy behaviour corpus (enemy AI foundation redesign R-0).")]
        public void MeasureProductionStages()
        {
            RequireFullRegression();
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var production = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var stages = CombatStages(production);
            var artifact = RepresentativeArtifact(production);
            var runs = new List<RunRecord>();
            var errors = new List<string>();
            foreach (var stage in stages)
            foreach (Script script in Enum.GetValues(typeof(Script)))
            foreach (Loadout loadout in Enum.GetValues(typeof(Loadout)))
            {
                var run = new CorpusRun(production, stage, script, loadout, artifact);
                try
                {
                    run.Execute();
                }
                catch (Exception exception)
                {
                    run.Record.Error = exception.GetType().Name + ": " + exception.Message.Replace('\t', ' ').Replace('\n', ' ');
                    errors.Add($"{stage.StageKey}/{script}/{loadout}: {run.Record.Error}");
                }
                runs.Add(run.Record);
            }

            string summary = Summarize(runs, artifact);
            TestContext.Out.WriteLine(summary);
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                WriteRuns(Path.Combine(folder, "corpus-runs.tsv"), runs);
                WriteLines(Path.Combine(folder, "corpus-decisions.tsv"), DecisionRow.Header, runs.SelectMany(r => r.Decisions).Select(d => d.ToString()));
                WriteLines(Path.Combine(folder, "corpus-actors.tsv"), ActorRow.Header, runs.SelectMany(r => r.Actors).Select(a => a.ToString()));
                WriteLines(Path.Combine(folder, "corpus-candidates.tsv"), CandidateRow.Header, runs.SelectMany(r => r.Candidates).Select(c => c.ToString()));
                File.WriteAllText(Path.Combine(folder, "corpus-summary.txt"), summary);
            }
            Assert.That(errors, Is.Empty, "Some corpus runs could not be measured:\n" + string.Join("\n", errors));
        }

        /// <summary>
        /// 고정 회귀(설계 2.1 B, 계획 R-2): 1라운드에 앞선 아군이 길목을 막아도 뒤의 언데드 헤비는 접근 진척이 있는 이동이나 공격 후보를 가진다.
        /// 각 헤비가 아직 아무 행동도 하지 않은(이동할 수 있는) 마지막 결정 입력에서 본다. 이미 이동한 헤비는 다시 이동할 수 없어 보지 않는다.
        /// </summary>
        [Test]
        public void Stage1127HeavyBehindAlliesKeepsApproachProgress()
        {
            using var battle = Stage1127(advancePlayers: true);
            battle.EndPlayerTurn();
            battle.AdvanceTo(CombatControllerPhase.Enemy);
            var approach = new Dictionary<EntityId, (bool ok, string detail)>();
            PlayEnemySide(battle, input =>
            {
                var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                foreach (var _ in context.IndexSteps()) { }
                foreach (var actor in context.Actors.Values.Where(a => a.CharacterKey == EntityKey.Unit_UndeadHeavy &&
                    a.ActionUse == UnitActionUse.Available))
                    approach[actor.EntityId] = HasApproach(context, input, actor);
            }, null);
            Assert.That(approach, Is.Not.Empty, "1라운드 결정 입력에 행동 가능한 헤비가 없습니다.");
            foreach (var pair in approach)
                Assert.That(pair.Value.ok, Is.True, $"헤비 {pair.Key.Value}에게 접근 진척이 있는 이동이나 공격 후보가 없습니다: {pair.Value.detail}");
        }

        /// <summary>
        /// 고정 회귀(설계 2.1 B 3라운드, 계획 R-2): 닿을 수 있는 다른 대상이 있으면 데몬은 모든 플레이어 유닛에게서 멀어지는 이동을 하지 않는다. 3라운드를 포함한 1~4라운드의 데몬 이동을 모두 본다.
        /// </summary>
        [Test]
        public void Stage1127DemonDoesNotRetreatWhileTargetReachable()
        {
            using var battle = Stage1127(advancePlayers: true);
            var retreats = new List<string>();
            bool reached = false;
            for (int round = 1; round <= 4 && !battle.IsFinished; round++)
            {
                battle.EndPlayerTurn();
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Enemy);
                int current = round;
                PlayEnemySide(battle, null, (input, transition) =>
                {
                    if (transition.PathMovement == null || transition.ActionUseSettlement == null) return;
                    var actor = input.Characters.Single(c => c.EntityId == transition.ActionUseSettlement.ActorId);
                    if (actor.CharacterKey != EntityKey.Unit_Demon) return;
                    reached = true;
                    var after = battle.Characters().Single(c => c.EntityId == actor.EntityId).Anchor;
                    if (after.HasValue && MovesAwayWhileReachable(input, actor.Anchor.Value, after.Value))
                        retreats.Add($"{actor.Anchor.Value} -> {after.Value}");
                });
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Player);
            }
            if (!reached) Assert.Inconclusive("1~4라운드에 데몬 이동이 없어 확인하지 못했습니다(전투 종료 또는 데몬 사망).");
            Assert.That(retreats, Is.Empty, "데몬 후퇴: " + string.Join(", ", retreats));
        }

        /// <summary>
        /// 고정 회귀(설계 2.1 B 2라운드·6.5 계산 예, 계획 R-3): 데몬은 같은 적 진영 안에서 해머 옆으로 걸어가 공격한다(플레이어가 전진하는 판).
        /// 설계 표의 2라운드 상황((8,6) 대기)은 R-3에서 1라운드에 이미 (8,5)로 가서 생기지 않으므로 1~2라운드 중 한 진영을 본다.
        /// 뒤쪽의 그레이브키퍼·헤비도 교환이 이로우면 교전한다(공격이 하나 이상). 사용자 수치 조정(d527a001d, 2026-11-10) 뒤에는
        /// 전진하는 판이 2라운드에 끝나 뒤쪽 유닛 차례가 오지 않으므로, 이 부분은 플레이어가 제자리인 판의 1~4라운드에서 본다
        /// (넉백 보유 묘지기가 공격하지 못하던 문제의 제작 스테이지 고정이기도 하다; 이전: 전진하는 판의 3~4라운드).
        /// </summary>
        [Test]
        public void Stage1127DemonStepsNextToHammerAndAttacks()
        {
            var advance = PlayStage1127Rounds(advancePlayers: true);
            var hold = PlayStage1127Rounds(advancePlayers: false);
            Assert.That(advance.DemonMoved && advance.DemonAttackedAfterMove, Is.True,
                "1~2라운드 데몬이 걸어가 공격하지 않음: " + advance.Detail);
            Assert.That(hold.BackLineAttacks, Is.Positive, "제자리 판 1~4라운드 그레이브키퍼·헤비 공격 없음: " + hold.Detail);
        }

        // 1127을 최대 네 라운드 돌리며 적 행동을 기록한다. 데몬은 1~2라운드의 이동 후 공격을, 뒤쪽 유닛은 모든 라운드의 공격을 센다.
        private static (bool DemonMoved, bool DemonAttackedAfterMove, int BackLineAttacks, string Detail) PlayStage1127Rounds(bool advancePlayers)
        {
            using var battle = Stage1127(advancePlayers);
            var log = new List<string>();
            bool demonMoved = false, demonAttackedAfterMove = false;
            int backLineAttacks = 0;
            for (int round = 1; round <= 4 && !battle.IsFinished; round++)
            {
                battle.EndPlayerTurn();
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Enemy);
                int current = round;
                var movedThisSide = new HashSet<EntityId>();
                PlayEnemySide(battle, null, (input, transition) =>
                {
                    if (transition.ActionUseSettlement == null) return;
                    var actor = input.Characters.Single(c => c.EntityId == transition.ActionUseSettlement.ActorId);
                    string kind = transition.Attack.HasValue ? "Attack" : transition.PathMovement != null ? "Move" : "Other";
                    log.Add($"r{current} {actor.CharacterKey}#{actor.EntityId.Value}@{actor.Anchor} {kind}");
                    if (current <= 2 && actor.CharacterKey == EntityKey.Unit_Demon)
                    {
                        if (kind == "Move") { demonMoved = true; movedThisSide.Add(actor.EntityId); }
                        else if (kind == "Attack" && movedThisSide.Contains(actor.EntityId)) demonAttackedAfterMove = true;
                    }
                    if (kind == "Attack" &&
                        (actor.CharacterKey == EntityKey.Unit_Gravekeeper || actor.CharacterKey == EntityKey.Unit_UndeadHeavy))
                        backLineAttacks++;
                });
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Player);
            }
            return (demonMoved, demonAttackedAfterMove, backLineAttacks, string.Join(" | ", log));
        }

        /// <summary>
        /// 고정 회귀(계획 R-5, 설계 6.8 동일성 규칙): 운영 스테이지마다 전진 각본·로드아웃 없음으로 다섯 라운드의 적 진영 전체를
        /// 진영 캐시 없이·있게 두 번 돌려, 분할 1/2/31마다 흔적이 같은지 본다. 흔적은 게시 행동(종류·행위자·조준·경로·도발 근원),
        /// 대기·진영 종료, 결과 사건·상태, 진단 기록의 점수 항목과 대기 이유다. 작업·미리보기·캐시 적중·시간은 비교하지 않는다.
        /// </summary>
        [Test, Timeout(900000)]
        public void CacheOnOffIdenticalTracesOnProductionStages()
        {
            RequireFullRegression();
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var production = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var artifact = RepresentativeArtifact(production);
            var hits = default(CacheCounts);
            var reasons = new SortedDictionary<string, int>();
            int decisions = 0;
            foreach (var stage in CombatStages(production))
            foreach (int partition in new[] { 1, 2, 31 })
            {
                var off = new List<string>();
                var on = new List<string>();
                new CorpusRun(production, stage, Script.Advance, Loadout.None, artifact, useSideCache: false, partition: partition, traces: off).Execute();
                var cached = new CorpusRun(production, stage, Script.Advance, Loadout.None, artifact, useSideCache: true, partition: partition, traces: on);
                cached.Execute();
                Assert.That(on.Count, Is.EqualTo(off.Count), $"{stage.StageKey}/{partition}: 결정 수");
                for (int i = 0; i < off.Count; i++)
                    Assert.That(on[i], Is.EqualTo(off[i]), $"{stage.StageKey}/{partition}: 결정 {i + 1}");
                hits += CacheCounts.Of(cached.SideCache);
                foreach (var pair in cached.SideCache.MissReasons) reasons[pair.Key] = reasons.TryGetValue(pair.Key, out int known) ? known + pair.Value : pair.Value;
                decisions += off.Count;
            }
            TestContext.WriteLine($"decisions={decisions} forecastHits/misses threatHits/misses: {hits} misses=[{string.Join(" ", reasons.Select(p => $"{p.Key}={p.Value}"))}]");
            Assert.That(hits.ForecastHits, Is.Positive, "위험 예보를 한 번도 다시 쓰지 않았습니다.");
            Assert.That(hits.ThreatHits, Is.Positive, "위협 필드를 한 번도 다시 쓰지 않았습니다.");
        }

        /// <summary>
        /// 고정 회귀(넉백 보유 적 수정, 2026-11-10): 제작 스테이지에서 진영 시작 때 지금 칠 수 있었던 적 유닛 종류는 적어도 한 번 공격한다.
        /// 원인과 상관없이 한 종류가 공격을 아예 못 하는 상태를 잡는다(예: 묘지기는 자기 넉백이 아군 전원 피해 상한을 만들어 코퍼스에서 공격 0회였다).
        /// </summary>
        [Test, Timeout(900000)]
        public void EveryEnemyKindAttacksWhenItCan()
        {
            RequireFullRegression();
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var production = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var artifact = RepresentativeArtifact(production);
            var able = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var attacks = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var stage in CombatStages(production))
            foreach (var script in new[] { Script.Advance, Script.HoldBack })
            {
                var run = new CorpusRun(production, stage, script, Loadout.None, artifact);
                run.Execute();
                foreach (var actor in run.Record.Actors.Where(a => a.CanAttackNow))
                    able[actor.ActorKey] = (able.TryGetValue(actor.ActorKey, out int count) ? count : 0) + 1;
                foreach (var decision in run.Record.Decisions.Where(d => d.Kind == "Attack"))
                    attacks[decision.ActorKey] = (attacks.TryGetValue(decision.ActorKey, out int count) ? count : 0) + 1;
            }
            TestContext.WriteLine(string.Join("\n", able.Select(pair =>
                $"{pair.Key}: able sides {pair.Value}, attacks {(attacks.TryGetValue(pair.Key, out int count) ? count : 0)}")));
            var silent = able.Keys.Where(key => !attacks.ContainsKey(key)).ToArray();
            Assert.That(able, Is.Not.Empty, "지금 칠 수 있었던 적이 없습니다(코퍼스 확인 필요).");
            Assert.That(silent, Is.Empty, "칠 수 있었는데 한 번도 공격하지 않은 적 유닛 종류: " + string.Join(", ", silent));
        }

        /// <summary>
        /// 진단 덤프(계획 R-5, 옛 Stage1127EnemyHoldDiagnostics를 대신함): 스테이지 1127에서 네 라운드 동안 결정마다 진단 기록
        /// (행위자별 대기 항목, 후보별 점수 항목·치명·정밀 여부, 대기 이유)을 시험 출력과 PROJECTL_ENEMY_AI_RAW 파일로 남긴다. 판정은 바꾸지 않는다.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        [Explicit("Stage 1127 enemy decision record dump")]
        public void DumpStage1127DecisionRecords(bool advancePlayers)
        {
            RequireFullRegression();
            using var battle = Stage1127(advancePlayers);
            var log = new StringBuilder();
            var cache = new EnemyDecisionSideCache();
            for (int round = 1; round <= 4 && !battle.IsFinished; round++)
            {
                battle.EndPlayerTurn();
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Enemy);
                var state = battle.Buffers.CombatStateReadSink;
                for (int decision = 1; decision < 128; decision++)
                {
                    var command = state.Current.CommandState;
                    if (command.Phase == CombatFlowPhase.Finished || command.Turn.ControllerPhase != CombatControllerPhase.Enemy) break;
                    var input = battle.EnemyInput();
                    if (!CombatCommandCalculator.RequiresEnemyDecision(input))
                    {
                        Require(battle.Core.CombatAdvance.AdvanceCombat(), "적 진영 진행");
                        continue;
                    }
                    var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
                    var transition = CombatCommandCalculator.AdvanceUtility(input, Rules, diagnostics, cache);
                    Require(transition.Accepted, "적 결정 거절: " + transition.Reason);
                    log.AppendLine($"== round {round} decision {decision}: units [{string.Join(" ", input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).Select(c => $"{c.CharacterKey}#{c.EntityId.Value}@{Cell(c.Anchor)} hp={c.Health.CurrentHealth}"))}]");
                    log.AppendLine(Trace(transition, diagnostics));
                    Require(battle.Publisher.Publish(transition), "적 결정 게시");
                }
                if (battle.IsFinished) break;
                battle.AdvanceTo(CombatControllerPhase.Player);
            }
            TestContext.Out.WriteLine(log.ToString());
            string folder = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_RAW");
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"stage1127-records-{(advancePlayers ? "advance" : "hold")}.txt"), log.ToString());
            }
        }

        // 사용자 화면과 같은 배치 (2,1)(4,2)(8,2)(10,2). 전진 각본이면 1라운드에 진단 시험과 같은 걸음을 시도한다.
        private static void RequireFullRegression()
        {
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_FULL_REGRESSION") != "1")
                Assert.Ignore("Set PROJECTL_ENEMY_AI_FULL_REGRESSION=1 to run the full production-stage corpus.");
        }

        private static ProductionBattle Stage1127(bool advancePlayers)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var production = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var stage = CombatStages(production).Single(s => s.StageKey == 1127);
            var battle = new ProductionBattle(production, stage);
            try
            {
                Require(battle.TrySubmit(CombatViewCommandData.PlaceMainCharacter(new CellCoord(2, 1), Rotation4.R0), out _), "메인 배치");
                Require(battle.TrySubmit(CombatViewCommandData.PlaceUnit(battle.Unit(EntityKey.Unit_Cowboy).EntityId, new CellCoord(4, 2), Rotation4.R0), out _), "카우보이 배치");
                Require(battle.TrySubmit(CombatViewCommandData.PlaceUnit(battle.Unit(EntityKey.Unit_DynamiteHammer).EntityId, new CellCoord(8, 2), Rotation4.R0), out _), "해머 배치");
                Require(battle.TrySubmit(CombatViewCommandData.PlaceUnit(battle.Unit(EntityKey.Unit_Sniper).EntityId, new CellCoord(10, 2), Rotation4.R0), out _), "저격수 배치");
                battle.EnterCombat();
                if (advancePlayers)
                {
                    battle.TryMove(battle.Unit(EntityKey.Unit_DynamiteHammer).EntityId, new[] { new CellCoord(8, 2), new CellCoord(8, 3), new CellCoord(8, 4) });
                    battle.TryMove(battle.Unit(EntityKey.Unit_Cowboy).EntityId, new[] { new CellCoord(4, 2), new CellCoord(5, 2),
                        new CellCoord(6, 2), new CellCoord(7, 2), new CellCoord(7, 3) });
                    battle.TryMove(battle.Unit(EntityKey.Unit_Sniper).EntityId, new[] { new CellCoord(10, 2), new CellCoord(10, 3) });
                    battle.TryMove(battle.Unit(MainKey).EntityId, new[] { new CellCoord(2, 1), new CellCoord(2, 2) });
                }
                return battle;
            }
            catch
            {
                battle.Dispose();
                throw;
            }
        }

        // 적 진영 하나를 끝까지 게시한다. 결정 입력과 게시한 결정을 호출자에게 보여 준다.
        private static void PlayEnemySide(ProductionBattle battle, Action<CombatCommandInput> beforeDecision,
            Action<CombatCommandInput, CombatCommandTransition> afterPublish)
        {
            var state = battle.Buffers.CombatStateReadSink;
            for (int guard = 0; guard < 128; guard++)
            {
                var command = state.Current.CommandState;
                if (command.Phase == CombatFlowPhase.Finished || command.Turn.ControllerPhase != CombatControllerPhase.Enemy) return;
                var input = battle.EnemyInput();
                if (!CombatCommandCalculator.RequiresEnemyDecision(input))
                {
                    Require(battle.Core.CombatAdvance.AdvanceCombat(), "적 진영 진행");
                    continue;
                }
                beforeDecision?.Invoke(input);
                var transition = CombatCommandCalculator.AdvanceUtility(input, Rules);
                Require(transition.Accepted, "적 결정 거절: " + transition.Reason);
                Require(battle.Publisher.Publish(transition), "적 결정 게시");
                afterPublish?.Invoke(input, transition);
            }
            throw new InvalidOperationException("적 진영이 128번 안에 끝나지 않았습니다.");
        }

        private static (bool ok, string detail) HasApproach(EnemyDecisionContext context, CombatCommandInput input, CharacterCombatFacts actor)
        {
            StageHazardForecast forecast;
            using (var builder = new StageHazardForecast.Builder(input))
            {
                while (!builder.IsComplete) builder.Step();
                forecast = builder.Build();
            }
            using var candidates = new EnemyActionCandidates(context, actor, forecast);
            while (!candidates.IsComplete) candidates.Step();
            bool ok = candidates.Attacks.Count != 0 || candidates.Moves.Any(move => candidates.Progress(move) > 0);
            string detail = $"@{actor.Anchor} attacks={candidates.Attacks.Count} truncated={candidates.EngagementTruncated} " +
                $"nodes={candidates.EngagementExpandedNodes} moves=[{string.Join(" ", candidates.Moves.Select(m => $"{m.Path.Endpoint}:{candidates.Progress(m)}"))}] " +
                $"units=[{string.Join(" ", input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).Select(c => $"{c.CharacterKey}@{c.Anchor}"))}]";
            return (ok, detail);
        }

        // 모든 플레이어 유닛까지의 정적 경로 거리가 늘었고, 출발 칸에서 닿는 플레이어 유닛이 하나라도 있었다.
        // 고정 회귀용: 다른 플레이어 유닛의 칸은 적의 길을 막는다(교전 필드와 같은 규칙, 계획 R-3 기록). 말뭉치 후퇴 지표는 정적 지형 그대로다.
        private static bool MovesAwayWhileReachable(CombatCommandInput input, CellCoord from, CellCoord to)
        {
            var board = Board.Of(input);
            var players = Field(input.Characters, Owner.Player).Where(c => c.Anchor.HasValue).ToArray();
            bool reachable = false, allFarther = true;
            foreach (var target in players)
            {
                var others = players.Where(other => other.EntityId != target.EntityId).SelectMany(other => other.OccupiedCells);
                var distances = RouteDistances(board.Blocking(others), target.Anchor.Value);
                if (!distances.TryGetValue(from, out int before)) continue;
                reachable = true;
                if (!distances.TryGetValue(to, out int after) || after <= before) allFarther = false;
            }
            return reachable && allFarther;
        }

        // 맵에 등록된 운영 전투 스테이지 전체다. 10000 이상은 시험용 스테이지라 뺀다.
        private static IReadOnlyList<StageRootData> CombatStages(IDataBaseQueryContext production)
        {
            var query = production.InGameQuery;
            return query.StageMapTableQuery.GetAllStageMaps().Select(m => m.MapKey).Distinct()
                .SelectMany(mapKey => query.StageTableQuery.GetStages(mapKey))
                .Where(s => s.StageData is CombatStageDetail && s.StageKey < 10000)
                .GroupBy(s => s.StageKey).Select(g => g.First())
                .OrderBy(s => s.StageKey).ToArray();
        }

        // 효과가 있는 운영 Artifact 정의 중 ID가 가장 작은 것이다. 같은 정의를 모든 스테이지에 놓는다.
        private static ArtifactDefinitionData RepresentativeArtifact(IDataBaseQueryContext production)
        {
            var definition = production.InGameQuery.ArtifactDefinitionTableQuery.GetAllDefinitions()
                .Where(d => d.Effects.Count != 0).OrderBy(d => d.ArtifactId).FirstOrDefault();
            if (definition == null) throw new InvalidOperationException("효과가 있는 운영 Artifact 정의가 없습니다.");
            return definition;
        }

        private sealed class CorpusRun
        {
            private readonly IDataBaseQueryContext _production;
            private readonly StageRootData _stage;
            private readonly Script _script;
            private readonly Loadout _loadout;
            private readonly ArtifactDefinitionData _artifact;
            // 운영 준비 경로처럼 적 진영 캐시를 쓴다(계획 R-5). 핸들은 실행 하나에 하나이고, 진영이 바뀌면 핸들이 스스로 비운다.
            private readonly EnemyDecisionSideCache _sideCache;
            // 0이면 운영 계산기(AdvanceUtility)로, 1 이상이면 세션을 그 분할로 돌리고 준비 실행(AdvancePrepared)으로 정산한다.
            private readonly int _partition;
            private readonly List<string> _traces;
            private ProductionBattle _battle;
            private IInGameCoreSystemGroupService _core;
            private IBufferQuery _buffers;

            internal RunRecord Record { get; }

            internal CorpusRun(IDataBaseQueryContext production, StageRootData stage, Script script, Loadout loadout,
                ArtifactDefinitionData artifact, bool useSideCache = true, int partition = 0, List<string> traces = null)
            {
                _production = production;
                _stage = stage;
                _script = script;
                _loadout = loadout;
                _artifact = artifact;
                _sideCache = useSideCache ? new EnemyDecisionSideCache() : null;
                _partition = partition;
                _traces = traces;
                Record = new RunRecord(stage.StageKey, stage.MapKey, stage.MapRole.ToString(), script.ToString(), loadout.ToString());
            }

            internal EnemyDecisionSideCache SideCache => _sideCache;

            internal void Execute()
            {
                using var battle = new ProductionBattle(_production, _stage, _loadout == Loadout.Artifact ? _artifact : null);
                _battle = battle;
                _core = battle.Core;
                _buffers = battle.Buffers;
                if (_loadout == Loadout.Artifact) Record.LoadoutDetail = "artifact " + _artifact.ArtifactId;
                Deploy();
                if (_loadout == Loadout.Item) EquipItem(battle.Definition);
                battle.EnterCombat();
                Record.EnemyKeys = string.Join(",", Field(Characters(), Owner.Enemy).Select(c => c.CharacterKey).Distinct().OrderBy(k => k));
                Play();
            }

            // 아군 배치: 메인 캐릭터부터 손패 유닛을 ID 순서로, 각본이 정한 칸 순서에서 처음 받아 주는 칸에 놓는다.
            private void Deploy()
            {
                var current = _buffers.CombatStateReadSink.Current;
                var zone = new List<CellCoord>();
                for (int y = 0; y < current.BoardHeight; y++)
                for (int x = 0; x < current.BoardWidth; x++)
                    if (current.PlayerZone.Contains(new CellCoord(x, y))) zone.Add(new CellCoord(x, y));
                var enemies = Field(Characters(), Owner.Enemy).Where(c => c.Anchor.HasValue).Select(c => c.Anchor.Value).ToArray();
                var order = _script == Script.Spread ? SpreadOrder(zone, enemies) : FrontOrder(zone, enemies);
                var hand = Characters().Where(c => c.Owner == Owner.Player && c.Area == CharacterRuntimeArea.Hand)
                    .OrderBy(c => c.CharacterKey == MainKey ? 0 : 1).ThenBy(c => c.EntityId.Value).ToArray();
                var used = new HashSet<CellCoord>();
                foreach (var unit in hand)
                {
                    bool placed = false;
                    foreach (var cell in order)
                    {
                        if (used.Contains(cell)) continue;
                        var command = unit.CharacterKey == MainKey
                            ? CombatViewCommandData.PlaceMainCharacter(cell, Rotation4.R0)
                            : CombatViewCommandData.PlaceUnit(unit.EntityId, cell, Rotation4.R0);
                        if (!TrySubmit(command, out _)) continue;
                        used.Add(cell);
                        placed = true;
                        break;
                    }
                    Require(placed, $"{unit.CharacterKey} 배치 칸 없음");
                }
                Record.Deployment = string.Join(" ", used.Select(Cell));
            }

            // 전투 정의의 첫 아이템(키 순서)을 재고에 넣고 배치된 첫 서브 유닛에 기존 장착 명령으로 단다.
            private void EquipItem(BattleInitialization definition)
            {
                var item = definition.ItemDefinitions.OrderBy(d => d.Key).FirstOrDefault();
                Require(item != null, "전투 정의에 아이템이 없습니다.");
                var grant = ItemInventoryCalculator.Grant(_buffers.ItemInventoryReadSink.Current, definition.ItemDefinitions, item.Key, 1);
                Require(grant.Accepted, "아이템 지급: " + grant.Reason);
                _buffers.ItemInventoryWriteSink.PublishGrant(grant.Change);
                var holder = Field(Characters(), Owner.Player).Where(c => c.CharacterKey != MainKey).OrderBy(c => c.EntityId.Value).First();
                Require(TrySubmit(CombatViewCommandData.EquipItem(holder.EntityId, item.Key), out string reason), "아이템 장착: " + reason);
                Record.LoadoutDetail = $"item {item.Key} on {holder.CharacterKey}";
            }

            private void Play()
            {
                Dictionary<EntityId, ActorRow> lastSide = null;
                for (int round = 1; round <= Rounds; round++)
                {
                    Record.Rounds = round;
                    var enemiesAtPlayerStart = LivingIds(Owner.Enemy);
                    if (_script == Script.Advance && round == 1) AdvancePlayers();
                    _battle.EndPlayerTurn();
                    if (IsFinished()) break;
                    AdvanceTo(CombatControllerPhase.Enemy);
                    var enemiesAtSideStart = LivingIds(Owner.Enemy);
                    Record.EnemyDeathsPlayerWindow += enemiesAtPlayerStart.Count(id => !enemiesAtSideStart.Contains(id));
                    ClassifyDeaths(enemiesAtPlayerStart.Where(id => !enemiesAtSideStart.Contains(id)), lastSide, round - 1, "player");
                    var playersAtSideStart = LivingIds(Owner.Player);
                    lastSide = PlaySide(round, _battle.EnemyInput, _battle.Publisher);
                    var enemiesAfterSide = LivingIds(Owner.Enemy);
                    Record.EnemyDeathsEnemyWindow += enemiesAtSideStart.Count(id => !enemiesAfterSide.Contains(id));
                    ClassifyDeaths(enemiesAtSideStart.Where(id => !enemiesAfterSide.Contains(id)), lastSide, round, "enemy");
                    Record.PlayerDeaths += playersAtSideStart.Count(id => !LivingIds(Owner.Player).Contains(id));
                    if (IsFinished()) break;
                    AdvanceTo(CombatControllerPhase.Player);
                    var enemiesAfterBoundary = LivingIds(Owner.Enemy);
                    ClassifyDeaths(enemiesAfterSide.Where(id => !enemiesAfterBoundary.Contains(id)), lastSide, round, "boundary");
                }
                Record.Finished = IsFinished();
            }

            // 적 사망을 그 유닛의 마지막 결정 기준으로 나눈다(계획 R-3 말뭉치 보강). 말뭉치 아군은 공격하지 않는다.
            // 피할 수 있음: 마지막 자리가 확실히 치명인데 제자리도 치명이 아니었다(같은 판정, 안전한 선택지가 있었다).
            // 피할 수 없음: 모든 선택지가 치명(대기 이유 LethalOnly, 또는 제자리도 치명인 행동). 그 밖: 결정 때 확실한 치명이 아니었다.
            private void ClassifyDeaths(IEnumerable<EntityId> died, Dictionary<EntityId, ActorRow> side, int round, string window)
            {
                foreach (var id in died)
                {
                    ActorRow row = null;
                    side?.TryGetValue(id, out row);
                    string kind = row == null || !row.FinalLethal ? "other" : row.FinalUnavoidable ? "unavoidable" : "avoidable";
                    if (kind == "avoidable") Record.DeathsAvoidable++;
                    else if (kind == "unavoidable") Record.DeathsUnavoidable++;
                    else Record.DeathsOther++;
                    Record.DeathNotes.Add($"r{round}/{window}/{row?.ActorKey ?? "?"}#{id.Value}/{kind}/{row?.HoldReason ?? "-"}");
                }
            }

            // 대기 이유와 확실한 치명 판정을 결정 세션의 진단 기록(설계 6.9)에서 읽는다(계획 R-5: 말뭉치가 판정을 다시 계산하지 않는다).
            // 대기한 행위자: 제자리 치명 = 기록의 대기 치명. 행동한 행위자: 게시한 행동과 같은 후보의 치명, 피할 수 없음 = 제자리도 치명.
            // 마지막 기록이 그 유닛의 마지막 자리다.
            private static void Observe(CombatCommandTransition transition, EnemyDecisionDiagnostics diagnostics,
                Dictionary<EntityId, ActorRow> actors)
            {
                for (int i = 0; i < diagnostics.ActorRecordCount; i++)
                {
                    var record = diagnostics.GetActorRecord(i);
                    if (record.HoldReason is not EnemyHoldReason reason || !actors.TryGetValue(record.ActorId, out var row)) continue;
                    row.HoldReason = reason.ToString();
                    row.FinalLethal = record.HoldLethal;
                    row.FinalUnavoidable = reason == EnemyHoldReason.LethalOnly;
                }
                if (!(transition.ActionUseSettlement?.ActorId is EntityId id) || !actors.TryGetValue(id, out var acted) ||
                    !transition.Attack.HasValue && transition.PathMovement == null) return;
                var actedRecord = LastRecord(diagnostics, id);
                var candidate = actedRecord == null ? (EnemyDecisionDiagnostics.CandidateRecord?)null : PublishedCandidate(actedRecord, transition, diagnostics);
                acted.HoldReason = "-";
                acted.FinalLethal = candidate?.Lethal ?? actedRecord?.HoldLethal ?? false;
                acted.FinalUnavoidable = actedRecord?.HoldLethal ?? false;
            }

            // 적 진영 하나: 첫 결정 입력에서 행위자마다 이번 진영에 공격 위치에 닿을 수 있는지 재고, 결정마다 행동을 기록한다.
            private Dictionary<EntityId, ActorRow> PlaySide(int round, Func<CombatCommandInput> enemyInput, CombatTransitionPublisher publisher)
            {
                Record.EnemySides++;
                var state = _buffers.CombatStateReadSink;
                Dictionary<EntityId, ActorRow> actors = null;
                var acted = new HashSet<EntityId>();
                for (int guard = 0; guard < 128; guard++)
                {
                    var command = state.Current.CommandState;
                    if (command.Phase == CombatFlowPhase.Finished || command.Turn.ControllerPhase != CombatControllerPhase.Enemy) break;
                    var input = enemyInput();
                    if (!CombatCommandCalculator.RequiresEnemyDecision(input))
                    {
                        Require(_core.CombatAdvance.AdvanceCombat(), "적 진영 진행");
                        continue;
                    }
                    actors ??= _traces == null ? MeasureReach(round, input) : new Dictionary<EntityId, ActorRow>();
                    var diagnostics = new EnemyDecisionDiagnostics(captureAlternatives: true);
                    var before = CacheCounts.Of(_sideCache);
                    long start = Stopwatch.GetTimestamp();
                    var transition = Decide(input, diagnostics);
                    double latency = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                    Require(transition.Accepted, "적 결정 거절: " + transition.Reason);
                    var row = DescribeDecision(round, input, transition, diagnostics, latency);
                    row.Cache = CacheCounts.Of(_sideCache) - before;
                    Observe(transition, diagnostics, actors);
                    Record.Candidates.AddRange(CandidateRows(round, Record.Decisions.Count, transition, diagnostics));
                    _traces?.Add(Trace(transition, diagnostics));
                    if (row.Kind != "End" && transition.ActionUseSettlement != null) acted.Add(transition.ActionUseSettlement.ActorId);
                    Require(publisher.Publish(transition), "적 결정 게시");
                    if (row.Kind == "Move" && transition.ActionUseSettlement != null)
                    {
                        var after = Characters().SingleOrDefault(c => c.EntityId == transition.ActionUseSettlement.ActorId);
                        row.To = after?.Anchor;
                        MarkRetreat(row, input);
                        var record = LastRecord(diagnostics, transition.ActionUseSettlement.ActorId);
                        var published = record == null ? (EnemyDecisionDiagnostics.CandidateRecord?)null : PublishedCandidate(record, transition, diagnostics);
                        row.FieldRetreat = published?.Terms.Progress < 0;
                    }
                    Record.Add(row);
                }
                if (actors == null) return null;
                foreach (var actor in actors.Values)
                {
                    actor.Acted = acted.Contains(actor.ActorId);
                    Record.Actors.Add(actor);
                }
                return actors;
            }

            // 운영 계산기(분할 0) 또는 운영 준비 경로와 같은 세션·준비 실행(분할 1 이상)으로 적 결정 하나를 정산한다.
            private CombatCommandTransition Decide(CombatCommandInput input, EnemyDecisionDiagnostics diagnostics)
            {
                if (_partition <= 0) return CombatCommandCalculator.AdvanceUtility(input, Rules, diagnostics, _sideCache);
                using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default, diagnostics, _sideCache);
                while (true)
                {
                    for (int guard = 0; !session.IsReady; guard++)
                    {
                        Require(guard < 4_000_000, "적 결정이 끝나지 않았습니다.");
                        for (int i = 0; i < _partition && !session.IsReady; i++) session.Step();
                    }
                    var decision = session.Decision;
                    var transition = CombatCommandCalculator.AdvancePrepared(input, decision, Rules);
                    if (transition.Accepted || decision.Kind == EnemyDecisionKind.EndEnemyPhase)
                    {
                        if (transition.Accepted && decision.Kind == EnemyDecisionKind.Attack) diagnostics.SelectedAim = decision.Aim;
                        return transition;
                    }
                    session.RejectSelected();
                }
            }

            private DecisionRow DescribeDecision(int round, CombatCommandInput input, CombatCommandTransition transition,
                EnemyDecisionDiagnostics diagnostics, double latency)
            {
                string kind = transition.Attack.HasValue ? "Attack" : transition.PathMovement != null ? "Move" : "End";
                var actorId = transition.ActionUseSettlement?.ActorId;
                var actor = actorId.HasValue ? input.Characters.SingleOrDefault(c => c.EntityId == actorId.Value) : null;
                return new DecisionRow(Record, round, kind, actor?.EntityId.Value ?? 0, actor?.CharacterKey.ToString() ?? "-",
                    actor?.Anchor, diagnostics.WorkUnits, diagnostics.ExpandedNodes, diagnostics.PreviewCalls,
                    diagnostics.GeneratedAttacks, diagnostics.GeneratedMoves, diagnostics.CapHits, latency,
                    Field(input.Characters, Owner.Enemy).Count(c => !input.EnvironmentBindings.ObstacleEntityIds.Contains(c.EntityId)));
            }

            // 진단 기록의 후보 한 줄씩이다(계획 R-5, 1127 후보 점수 덤프를 대신함). 게시한 행동과 같은 후보에 표시한다.
            private IEnumerable<CandidateRow> CandidateRows(int round, int decision, CombatCommandTransition transition,
                EnemyDecisionDiagnostics diagnostics)
            {
                for (int i = 0; i < diagnostics.ActorRecordCount; i++)
                {
                    var record = diagnostics.GetActorRecord(i);
                    var published = PublishedCandidate(record, transition, diagnostics);
                    yield return new CandidateRow(Record, round, decision, record, null, false);
                    foreach (var candidate in record.Candidates)
                        yield return new CandidateRow(Record, round, decision, record, candidate,
                            published.HasValue && SameAction(published.Value.Action, candidate.Action));
                }
            }

            // 정적 지형 기준 후퇴(참고용): 살아 있는 모든 플레이어 유닛까지의 정적 지형 경로 거리가 늘어난 이동이다. 출발 칸이 위험 예보 칸이면
            // 위험 회피로 따로 표시한다(두 후퇴 지표가 같이 쓴다). 문턱 3은 AI 경로 비용 기준(DecisionRow.FieldRetreat)으로 본다.
            private static void MarkRetreat(DecisionRow row, CombatCommandInput input)
            {
                if (!row.From.HasValue || !row.To.HasValue) return;
                var targets = Field(input.Characters, Owner.Player).Where(c => c.Anchor.HasValue).ToArray();
                if (targets.Length == 0) return;
                var board = Board.Of(input);
                bool any = false, allFarther = true;
                foreach (var target in targets)
                {
                    var distances = RouteDistances(board, target.Anchor.Value);
                    if (!distances.TryGetValue(row.From.Value, out int before)) continue;
                    any = true;
                    if (!distances.TryGetValue(row.To.Value, out int after) || after <= before) allFarther = false;
                }
                row.Retreat = any && allFarther;
                row.HazardOrigin = HazardCells(input).Contains(row.From.Value);
            }

            // 진영 시작에서 각 적 행위자가 자리 또는 이동 가능한 칸에서 살아 있는 플레이어 유닛 하나라도 칠 수 있는지 잰다.
            private Dictionary<EntityId, ActorRow> MeasureReach(int round, CombatCommandInput input)
            {
                var environment = input.State.Environment.VisibleTo(Owner.Enemy);
                var obstacles = new HashSet<EntityId>(input.EnvironmentBindings.ObstacleEntityIds);
                var targets = Field(input.Characters, Owner.Player).ToArray();
                var result = new Dictionary<EntityId, ActorRow>();
                var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                foreach (var _ in context.IndexSteps()) { }
                foreach (var actor in Field(input.Characters, Owner.Enemy).Where(c => c.Anchor.HasValue && !obstacles.Contains(c.EntityId))
                    .OrderBy(c => c.EntityId.Value))
                {
                    var row = new ActorRow(Record, round, actor.EntityId, actor.CharacterKey.ToString(), actor.Anchor.Value);
                    try
                    {
                        var cells = new List<CellCoord> { actor.Anchor.Value };
                        cells.AddRange(CombatMovementCalculator.UnitMoveCells(input, actor, environment));
                        row.MoveCells = cells.Count - 1;
                        // 공격자 허용은 입력의 행위자로 판정하므로 행위자를 이 유닛으로 묶은 입력을 넘긴다(추적 탐색과 같은 조건).
                        var actorInput = input.WithActor(actor.EntityId);
                        bool reachable = false;
                        foreach (var target in targets)
                        {
                            var reach = new EnemyAttackReach(actorInput, actor, target.EntityId, environment);
                            foreach (var _ in reach.TargetSteps()) { }
                            if (reach.From(actor.Anchor.Value)) row.CanAttackNow = true;
                            if (!reachable && cells.Any(reach.From)) reachable = true;
                        }
                        row.CanReachAttack = reachable;
                        // 교전 필드가 칸 상한에 걸렸는지 본다(계획 R-3 말뭉치 보강, 예상 0).
                        EnemyEngagementField field = null;
                        foreach (var _ in EnemyEngagementField.BuildSteps(context, actorInput, actor, null, value => field = value)) { }
                        row.Truncated = field?.IsTruncated == true;
                    }
                    catch (Exception exception)
                    {
                        row.Note = exception.GetType().Name;
                    }
                    result.Add(actor.EntityId, row);
                }
                return result;
            }

            // 1라운드 전진 각본: 유닛마다 가장 가까운 적까지의 정적 경로 거리를 가장 많이 줄이는 칸으로 걷는다. 거절되면 다음 칸을 쓴다.
            private void AdvancePlayers()
            {
                foreach (var unit in Field(Characters(), Owner.Player).Where(c => c.Anchor.HasValue).OrderBy(c => c.EntityId.Value).ToArray())
                {
                    var characters = Characters();
                    var current = characters.Single(c => c.EntityId == unit.EntityId);
                    var board = Board.Of(_buffers.CombatStateReadSink.Current, characters);
                    var enemyDistance = new Dictionary<CellCoord, int>();
                    foreach (var enemy in Field(characters, Owner.Enemy).Where(c => c.Anchor.HasValue))
                        foreach (var pair in RouteDistances(board, enemy.Anchor.Value))
                            if (!enemyDistance.TryGetValue(pair.Key, out int known) || pair.Value < known) enemyDistance[pair.Key] = pair.Value;
                    var occupied = new HashSet<CellCoord>(characters.Where(c => c.EntityId != current.EntityId &&
                        c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).SelectMany(c => c.OccupiedCells));
                    var parents = WalkableParents(board, current.Anchor.Value, current.Stats.MoveRange, occupied);
                    var candidates = parents.Keys.Where(c => c != current.Anchor.Value && enemyDistance.ContainsKey(c))
                        .OrderBy(c => enemyDistance[c]).ThenByDescending(c => PathOf(parents, c).Count).ThenBy(c => c.Y).ThenBy(c => c.X)
                        .Take(6).ToArray();
                    foreach (var endpoint in candidates)
                    {
                        if (enemyDistance.TryGetValue(current.Anchor.Value, out int now) && enemyDistance[endpoint] >= now) break;
                        if (TryMove(current.EntityId, PathOf(parents, endpoint)))
                        {
                            Record.PlayerMoves++;
                            break;
                        }
                    }
                }
            }

            private bool TryMove(EntityId actor, IReadOnlyList<CellCoord> cells) => _battle.TryMove(actor, cells);
            private bool TrySubmit(CombatViewCommandData command, out string reason) => _battle.TrySubmit(command, out reason);
            private void AdvanceTo(CombatControllerPhase target) => _battle.AdvanceTo(target);
            private bool IsFinished() => _battle.IsFinished;
            private IReadOnlyList<CharacterCombatFacts> Characters() => _battle.Characters();
            private HashSet<EntityId> LivingIds(Owner owner) => new(Field(Characters(), owner).Select(c => c.EntityId));
        }

        /// <summary>
        /// 운영 스테이지 정의와 시험 로스터로 세운 전투 하나다. 배치 명령·국면 진행·적 결정 입력 조립·운영 게시기를 제공한다.
        /// Artifact를 주면 전투 진입 전에 그 정의 하나를 보드에 배치한다.
        /// </summary>
        private sealed class ProductionBattle : IDisposable
        {
            private readonly FrameworkRuntime _runtime;
            private readonly ProductionStage _source;
            private readonly CoreIntegrationTestData _database;
            private CombatInputAssembler _assembler;

            internal IInGameCoreSystemGroupService Core { get; }
            internal IBufferQuery Buffers { get; }
            internal BattleInitialization Definition => _source.Definition;
            internal CombatTransitionPublisher Publisher { get; private set; }
            internal bool IsFinished => Buffers.CombatStateReadSink.Current.CommandState.Phase == CombatFlowPhase.Finished;

            internal ProductionBattle(IDataBaseQueryContext production, StageRootData stage, ArtifactDefinitionData artifact = null)
            {
                _source = new ProductionStage(production, stage);
                _database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
                if (artifact != null) _database.ArtifactDefinitions = new[] { artifact };
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create(_source).ExportServices(builder);
                _runtime = new FrameworkRuntime(builder.BuildSystemReader());
                try
                {
                    _runtime.InitData(_database);
                    _runtime.PostBindService();
                    Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                    Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                    _source.Buffers = Buffers;
                    Require(Core.Characters.InitializeRoster(1).Succeeded, "로스터 초기화");
                    CoreIntegrationTestData.InstallDeck(Buffers);
                    if (artifact != null)
                        Require(Buffers.ArtifactInitializationWriteSink.TryInitialize(PlacedArtifact(artifact)), "Artifact 보드 초기화");
                    Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                    {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, RegisteredStageKey, 1, 0, Array.Empty<int>())
                    });
                    Require(Core.ChapterProgression.EnterChapter(1).Accepted, "챕터 진입");
                    Require(Core.BattleEntry.EnterBattle(1, new StageNodeId(2)).Accepted, "전투 진입");
                }
                catch
                {
                    _runtime.Shutdown();
                    throw;
                }
            }

            public void Dispose() => _runtime.Shutdown();

            // 배치를 끝내고 전투를 시작한 뒤 첫 플레이어 턴까지 진행한다. 적 결정 입력 조립기와 게시기도 여기서 만든다.
            internal void EnterCombat()
            {
                Require(TrySubmit(CombatViewCommandData.EnterCombat(), out string enter), "전투 시작: " + enter);
                AdvanceTo(CombatControllerPhase.Player);
                var services = new SystemBuilderHelper();
                services.AddService<IBufferQuery>(Buffers);
                var reader = services.BuildSystemReader().ServiceStoreReader;
                var artifacts = new ArtifactLoadoutReader();
                artifacts.InitData(_database);
                artifacts.PostBindService(reader);
                _assembler = new CombatInputAssembler(new DefinitionRead(Definition), artifacts);
                _assembler.PostBindService(reader);
                var planner = new BattleProgressionPlanner(Core.ChapterProgression);
                planner.PostBindService(reader);
                Publisher = new CombatTransitionPublisher(planner);
                Publisher.PostBindService(reader);
            }

            internal CombatCommandInput EnemyInput()
            {
                var state = Buffers.CombatStateReadSink;
                return _assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.CompleteEnemyTurn,
                    default, default, processingKind: CombatProcessingKind.Advance, expectedBattleInstanceId: state.BattleInstanceId,
                    expectedControllerTurnSequence: state.Current.CommandState.Turn.ControllerTurnSequence));
            }

            internal void EndPlayerTurn()
            {
                var state = Buffers.CombatStateReadSink;
                Require(TrySubmit(CombatViewCommandData.EndPlayerTurn(state.BattleInstanceId,
                    state.Current.CommandState.Turn.ControllerTurnSequence), out string end), "플레이어 턴 종료: " + end);
            }

            internal bool TryMove(EntityId actor, IReadOnlyList<CellCoord> cells)
            {
                var path = new MovementPath(cells);
                Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor, path));
                Require(Core.CombatPreview.ProcessLatestPreview(), "이동 미리보기 처리");
                if (!Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview) || preview.MovementContext == null) return false;
                return TrySubmit(CombatViewCommandData.Move(actor, path, preview.MovementContext), out _);
            }

            internal bool TrySubmit(CombatViewCommandData command, out string reason)
            {
                int number = Buffers.CombatViewWriteSink.Submit(command);
                Require(Core.CombatCommands.ProcessNextCommand(), "명령 처리");
                Require(Buffers.CombatResultReadSink.TryGetLatest(out var result) && result.CommandNumber == number, "명령 결과");
                reason = result.Reason;
                return result.Accepted;
            }

            internal void AdvanceTo(CombatControllerPhase target)
            {
                for (int i = 0; i < 32; i++)
                {
                    var turn = Buffers.CombatStateReadSink.Current.CommandState.Turn;
                    if (turn.ControllerPhase == target || IsFinished) return;
                    Require(Core.CombatAdvance.AdvanceCombat(), $"진행 {turn.StagePhase}/{turn.ControllerPhase}");
                }
                throw new InvalidOperationException("국면에 닿지 않았습니다: " + target);
            }

            internal IReadOnlyList<CharacterCombatFacts> Characters() =>
                Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();

            internal CharacterCombatFacts Unit(EntityKey key) => Characters().Single(c => c.CharacterKey == key);
        }

        // 같은 결정에서 그 행위자의 마지막 평가 기록이다(거절 뒤 다시 평가하면 뒤의 것).
        private static EnemyDecisionDiagnostics.ActorRecord LastRecord(EnemyDecisionDiagnostics diagnostics, EntityId actor)
        {
            EnemyDecisionDiagnostics.ActorRecord last = null;
            for (int i = 0; i < diagnostics.ActorRecordCount; i++)
                if (diagnostics.GetActorRecord(i).ActorId == actor) last = diagnostics.GetActorRecord(i);
            return last;
        }

        // 게시한 행동과 같은 후보다: 같은 행위자의 공격이면 고른 조준, 이동이면 같은 요청 경로다.
        private static EnemyDecisionDiagnostics.CandidateRecord? PublishedCandidate(EnemyDecisionDiagnostics.ActorRecord record,
            CombatCommandTransition transition, EnemyDecisionDiagnostics diagnostics)
        {
            if (!(transition.ActionUseSettlement?.ActorId is EntityId id) || id != record.ActorId) return null;
            foreach (var candidate in record.Candidates)
            {
                var action = candidate.Action;
                if (transition.Attack.HasValue && action.Kind == EnemyDecisionKind.Attack && diagnostics.SelectedAim is AttackAim aim &&
                    action.Aim.Equals(aim)) return candidate;
                if (transition.PathMovement != null && action.Kind == EnemyDecisionKind.Move &&
                    action.Path.Cells.SequenceEqual(transition.PathMovement.RequestedPath.Cells)) return candidate;
            }
            return null;
        }

        private static bool SameAction(EnemyDecision left, EnemyDecision right) => left.ActorId == right.ActorId && DescribeAction(left) == DescribeAction(right);

        private static string DescribeAction(EnemyDecision action) => action.Kind switch
        {
            EnemyDecisionKind.Attack => $"attack {action.Aim.Kind}:{Cell(action.Aim.Cell)}:{action.Aim.Direction} taunt={action.TauntSourceId.Value}",
            EnemyDecisionKind.Move => $"move {string.Join(";", action.Path.Cells.Select(Cell))} taunt={action.TauntSourceId.Value}",
            _ => action.Kind.ToString()
        };

        private static string Terms(EnemyScoreTerms terms) => string.Join("\t", terms.Attack, terms.Hazard, terms.PlayerRisk, terms.Progress,
            terms.Followup, terms.NextTurn, terms.Bind, terms.Total);

        /// <summary>
        /// 결정 하나의 흔적이다(계획 R-5 비교 범위): 게시한 행동(종류·행위자·조준·경로·도발 근원), 결과 사건·상태,
        /// 진단 기록의 점수 항목과 대기 이유. 작업·미리보기·캐시 적중·시간은 넣지 않는다.
        /// </summary>
        private static string Trace(CombatCommandTransition transition, EnemyDecisionDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            var taunt = transition.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .FirstOrDefault(t => t.TraitKey == TraitKey.Taunt && t.TraitActivationKind == TraitActivationKind.TauntSelected);
            text.Append(string.Join("|", transition.Accepted, transition.ActionUseSettlement?.ActorId.Value ?? 0,
                transition.Attack.HasValue ? "Attack" : transition.PathMovement != null ? "Move" : "Boundary",
                diagnostics.SelectedAim?.Kind.ToString() ?? "", Cell(diagnostics.SelectedAim?.Cell), diagnostics.SelectedAim?.Direction.ToString() ?? "",
                transition.PathMovement == null ? "" : string.Join(";", transition.PathMovement.RequestedPath.Cells.Select(Cell)),
                taunt?.SourceUnitId.Value ?? 0, transition.Outcome.Outcome, transition.FinalState.Turn.ControllerPhase,
                transition.FinalState.Turn.ControllerTurnSequence));
            text.Append('\n').Append(string.Join(",", transition.Events.Select(e => e.Kind)));
            foreach (var final in transition.Characters.Characters.Select(change => change.Final).Concat(transition.Characters.Created))
                text.Append('\n').Append($"char {final.EntityId.Value} {final.Area} {Cell(final.Anchor)} hp={final.Health.CurrentHealth} " +
                    $"use={final.ActionUse} statuses={final.StatusEffects.Entries.Count}");
            text.Append('\n').Append($"env={transition.FinalState.Environment.Tiles.Count(t => t.Active)} structures=" +
                string.Join(",", transition.FinalState.Structures.Select(s => $"{s.EntityId.Value}:{s.Health.CurrentHealth}")));
            for (int i = 0; i < diagnostics.ActorRecordCount; i++)
            {
                var record = diagnostics.GetActorRecord(i);
                text.Append('\n').Append($"actor={record.ActorId.Value} hold=[{record.HoldTerms}] tier={record.HoldTier} lethal={record.HoldLethal} " +
                    $"reason={record.HoldReason?.ToString() ?? "-"}");
                foreach (var candidate in record.Candidates)
                    text.Append('\n').Append($"  {DescribeAction(candidate.Action)} [{candidate.Terms}] tier={candidate.Tier} " +
                        $"lethal={candidate.Lethal} precise={candidate.Precise}");
            }
            return text.ToString();
        }

        /// <summary>진영 캐시의 누계 적중·실패다(측정용). 결정 하나의 증가분을 결정 행에 남긴다.</summary>
        private readonly struct CacheCounts
        {
            internal int ForecastHits { get; }
            internal int ForecastMisses { get; }
            internal int ThreatHits { get; }
            internal int ThreatMisses { get; }

            private CacheCounts(int forecastHits, int forecastMisses, int threatHits, int threatMisses)
            {
                ForecastHits = forecastHits;
                ForecastMisses = forecastMisses;
                ThreatHits = threatHits;
                ThreatMisses = threatMisses;
            }

            internal static CacheCounts Of(EnemyDecisionSideCache cache) => cache == null ? default : new CacheCounts(cache.ForecastHits,
                cache.ForecastMisses, cache.ThreatHits, cache.ThreatMisses);

            public static CacheCounts operator -(CacheCounts a, CacheCounts b) => new CacheCounts(a.ForecastHits - b.ForecastHits,
                a.ForecastMisses - b.ForecastMisses, a.ThreatHits - b.ThreatHits, a.ThreatMisses - b.ThreatMisses);

            public static CacheCounts operator +(CacheCounts a, CacheCounts b) => new CacheCounts(a.ForecastHits + b.ForecastHits,
                a.ForecastMisses + b.ForecastMisses, a.ThreatHits + b.ThreatHits, a.ThreatMisses + b.ThreatMisses);

            public override string ToString() => string.Join("\t", ForecastHits, ForecastMisses, ThreatHits, ThreatMisses);
        }

        // 위험 예보가 가리키는 칸 전체다(붕괴·선로·낙석 경고·하붑 구름 등). 후퇴의 위험 이유 판정에만 쓴다.
        private static HashSet<CellCoord> HazardCells(CombatCommandInput input)
        {
            var cells = new HashSet<CellCoord>();
            using var builder = new StageHazardForecast.Builder(input);
            while (!builder.IsComplete) builder.Step();
            foreach (var fact in builder.Build().Facts)
            {
                if (fact.Cell.HasValue) cells.Add(fact.Cell.Value);
                if (fact.HaboobRegion != null) cells.UnionWith(fact.HaboobRegion);
            }
            return cells;
        }

        /// <summary>
        /// 시험 쪽 정적 지형 근사다: 공개된 활성 벽·낭떠러지, 살아 있는 장애물·구조물만 막는다. 유닛은 지나간다.
        /// 고지대 한 방향 규칙은 보지 않는다. Domain 규칙은 이 어셈블리에 보이지 않아 공개 사실로만 계산한다.
        /// </summary>
        private sealed class Board
        {
            private readonly int _width;
            private readonly int _height;
            private readonly HashSet<CellCoord> _blocked = new();

            private Board(int width, int height, MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
                IEnumerable<StructureCombatFacts> structures, IEnumerable<CharacterCombatFacts> characters)
            {
                _width = width;
                _height = height;
                var terrainIds = new HashSet<long>(bindings.TerrainSourceIds);
                foreach (var tile in environment.VisibleTo(Owner.Enemy).Tiles)
                    if (tile is StaticTerrainFact terrain && terrain.Active && terrainIds.Contains(terrain.Id) &&
                        (terrain.Kind == StaticTerrainKind.Wall || terrain.Kind == StaticTerrainKind.Cliff))
                        _blocked.Add(terrain.Cell);
                var obstacles = new HashSet<EntityId>(bindings.ObstacleEntityIds);
                foreach (var character in characters)
                    if (obstacles.Contains(character.EntityId) && character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead)
                        _blocked.UnionWith(character.OccupiedCells);
                foreach (var structure in structures)
                    if (!structure.Health.IsDead) _blocked.UnionWith(structure.OccupiedCells);
            }

            internal static Board Of(CombatCommandInput input) => new(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.Structures, input.Characters);

            internal static Board Of(CombatStateData state, IEnumerable<CharacterCombatFacts> characters) => new(state.BoardWidth,
                state.BoardHeight, state.Environment, state.EnvironmentBindings, state.Structures, characters);

            internal bool IsOpen(CellCoord cell) =>
                cell.X >= 0 && cell.Y >= 0 && cell.X < _width && cell.Y < _height && !_blocked.Contains(cell);

            private Board(Board source, IEnumerable<CellCoord> blocked)
            {
                _width = source._width;
                _height = source._height;
                _blocked.UnionWith(source._blocked);
                _blocked.UnionWith(blocked);
            }

            internal Board Blocking(IEnumerable<CellCoord> cells) => new(this, cells);
        }

        private static Dictionary<CellCoord, int> RouteDistances(Board board, CellCoord origin)
        {
            var distances = new Dictionary<CellCoord, int> { [origin] = 0 };
            var queue = new Queue<CellCoord>();
            queue.Enqueue(origin);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!board.IsOpen(next) || distances.ContainsKey(next)) continue;
                    distances.Add(next, distances[current] + 1);
                    queue.Enqueue(next);
                }
            }
            return distances;
        }

        // 걸음 수 안에서 유닛·막힌 칸을 지나지 않고 닿는 칸과 그 직전 칸이다(전진 각본 전용, 실제 수락은 Core가 정한다).
        private static Dictionary<CellCoord, CellCoord?> WalkableParents(Board board, CellCoord origin, int steps,
            HashSet<CellCoord> occupied)
        {
            var parents = new Dictionary<CellCoord, CellCoord?> { [origin] = null };
            var depth = new Dictionary<CellCoord, int> { [origin] = 0 };
            var queue = new Queue<CellCoord>();
            queue.Enqueue(origin);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (depth[current] == steps) continue;
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!board.IsOpen(next) || occupied.Contains(next) || parents.ContainsKey(next)) continue;
                    parents.Add(next, current);
                    depth.Add(next, depth[current] + 1);
                    queue.Enqueue(next);
                }
            }
            return parents;
        }

        private static IReadOnlyList<CellCoord> PathOf(Dictionary<CellCoord, CellCoord?> parents, CellCoord endpoint)
        {
            var cells = new List<CellCoord>();
            for (CellCoord? cell = endpoint; cell.HasValue; cell = parents[cell.Value]) cells.Add(cell.Value);
            cells.Reverse();
            return cells;
        }

        // 앞쪽 배치: 처음 적 위치에 가까운 칸부터(맨해튼), 같으면 위쪽·왼쪽.
        private static IReadOnlyList<CellCoord> FrontOrder(IReadOnlyList<CellCoord> zone, IReadOnlyList<CellCoord> enemies) =>
            zone.OrderBy(c => NearestManhattan(c, enemies)).ThenByDescending(c => c.Y).ThenBy(c => c.X).ToArray();

        // 흩어진 배치: 앞쪽 첫 칸에서 시작해 이미 고른 칸과의 최소 거리가 가장 먼 칸을 차례로 고른다.
        private static IReadOnlyList<CellCoord> SpreadOrder(IReadOnlyList<CellCoord> zone, IReadOnlyList<CellCoord> enemies)
        {
            var front = FrontOrder(zone, enemies).ToList();
            var order = new List<CellCoord>();
            while (front.Count != 0)
            {
                var next = order.Count == 0 ? front[0]
                    : front.OrderByDescending(c => order.Min(o => Manhattan(c, o))).ThenBy(c => front.IndexOf(c)).First();
                order.Add(next);
                front.Remove(next);
            }
            return order;
        }

        private static int NearestManhattan(CellCoord cell, IReadOnlyList<CellCoord> others) =>
            others.Count == 0 ? 0 : others.Min(o => Manhattan(cell, o));

        private static int Manhattan(CellCoord a, CellCoord b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        private static IEnumerable<CharacterCombatFacts> Field(IEnumerable<CharacterCombatFacts> characters, Owner owner) =>
            characters.Where(c => c.Owner == owner && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead);

        // 정의의 칸을 음수 없이 옮겨 한 개를 배치한 보드다. 보드 크기는 그 칸을 겨우 담는 크기다.
        private static ArtifactState PlacedArtifact(ArtifactDefinitionData definition)
        {
            int minX = definition.Cells.Min(c => c.X), minY = definition.Cells.Min(c => c.Y);
            var anchor = new CellCoord(-minX, -minY);
            var cells = definition.Cells.Select(c => c + anchor).ToArray();
            int width = cells.Max(c => c.X) + 1, height = cells.Max(c => c.Y) + 1;
            return new ArtifactState(width, height, Array.Empty<ArtifactItemState>(),
                new[] { new ArtifactItemState(definition.ArtifactId, 1, true, anchor, Rotation4.R0, cells) });
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Cell(CellCoord cell) => $"{cell.X},{cell.Y}";

        private static string Cell(CellCoord? cell) => cell.HasValue ? Cell(cell.Value) : "-";

        private static string Summarize(IReadOnlyList<RunRecord> runs, ArtifactDefinitionData artifact)
        {
            var text = new StringBuilder();
            text.AppendLine($"Enemy stage corpus: {runs.Select(r => r.StageKey).Distinct().Count()} stages, {runs.Count} runs, {Rounds} rounds, artifact {artifact.ArtifactId}.");
            // 문턱 3은 AI 경로 비용 기준 후퇴(fieldRetreatsNoHazard)로 본다. 정적 지형 기준(retreatsNoHazard)은 참고로 남긴다(계획 R-6 뒤 사용자 결정).
            text.AppendLine("loadout\treachActorSides\tidle\tidleRatio\tmoves\tretreats\tretreatsNoHazard\tfieldRetreats\tfieldRetreatsNoHazard\tattacks\tsidesWithAttack\tenemyDeathsEnemyWindow\tplayerDeaths\tmeanFirstWork\tmeanLatencyMs\tmaxLatencyMs\terrors");
            foreach (var group in runs.GroupBy(r => r.Loadout))
            {
                var ok = group.Where(r => r.Error == null).ToArray();
                int reach = ok.Sum(r => r.ReachActorSides), idle = ok.Sum(r => r.IdleActorSides);
                var decisions = ok.SelectMany(r => r.Decisions).ToArray();
                text.AppendLine(string.Join("\t", group.Key, reach, idle, Ratio(idle, reach), ok.Sum(r => r.Moves), ok.Sum(r => r.Retreats),
                    ok.Sum(r => r.RetreatsWithoutHazard), ok.Sum(r => r.FieldRetreats), ok.Sum(r => r.FieldRetreatsWithoutHazard),
                    ok.Sum(r => r.Attacks), ok.Sum(r => r.SidesWithAttack),
                    ok.Sum(r => r.EnemyDeathsEnemyWindow), ok.Sum(r => r.PlayerDeaths),
                    ok.Length == 0 ? "-" : ok.Average(r => r.FirstDecisionWork).ToString("0", CultureInfo.InvariantCulture),
                    decisions.Length == 0 ? "-" : decisions.Average(d => d.LatencyMs).ToString("0.00", CultureInfo.InvariantCulture),
                    decisions.Length == 0 ? "-" : decisions.Max(d => d.LatencyMs).ToString("0.00", CultureInfo.InvariantCulture),
                    group.Count(r => r.Error != null)));
            }
            text.AppendLine();
            // 계획 R-3 말뭉치 보강: 대기 이유별 대기(행동하지 않은 행위자-진영), 그중 닿을 수 있었던 것, 환경 사망 분류, 교전 필드 칸 상한.
            text.AppendLine("loadout\tholdReason\theldActorSides\tidleReachable");
            foreach (var group in runs.GroupBy(r => r.Loadout))
            foreach (var reason in group.SelectMany(r => r.Actors).Where(a => !a.Acted).GroupBy(a => a.HoldReason).OrderBy(g => g.Key))
                text.AppendLine(string.Join("\t", group.Key, reason.Key, reason.Count(), reason.Count(a => a.CanReachAttack == true)));
            text.AppendLine();
            text.AppendLine("loadout\tdeathsAvoidable\tdeathsUnavoidable\tdeathsOther\ttruncatedActorSides");
            foreach (var group in runs.GroupBy(r => r.Loadout))
                text.AppendLine(string.Join("\t", group.Key, group.Sum(r => r.DeathsAvoidable), group.Sum(r => r.DeathsUnavoidable),
                    group.Sum(r => r.DeathsOther), group.Sum(r => r.TruncatedActorSides)));
            foreach (var run in runs.Where(r => r.DeathNotes.Count != 0 || r.TruncatedActorSides != 0))
                text.AppendLine($"  {run.StageKey}/{run.Script}/{run.Loadout}: deaths [{string.Join(" ", run.DeathNotes)}] truncated={run.TruncatedActorSides}");
            text.AppendLine();
            // 계획 R-5: 진영 캐시 적중(결정마다 하나인 예보·위협 필드).
            text.AppendLine("loadout\tforecastHits\tforecastMisses\tthreatHits\tthreatMisses");
            foreach (var group in runs.GroupBy(r => r.Loadout))
                text.AppendLine(group.Key + "\t" + group.SelectMany(r => r.Decisions).Aggregate(default(CacheCounts), (sum, d) => sum + d.Cache));
            text.AppendLine();
            // 계획 R-5: 결정 지연을 필드 적 수 구간으로 묶는다(R-0 기준과 같은 방식).
            text.AppendLine("loadout\tfieldEnemies\tdecisions\tmeanLatencyMs\tp95LatencyMs\tmaxLatencyMs\tmeanWork");
            foreach (var group in runs.GroupBy(r => r.Loadout))
            foreach (var band in group.SelectMany(r => r.Decisions).GroupBy(d => EnemyBand(d.FieldEnemies)).OrderBy(b => b.Key))
            {
                var latency = band.Select(d => d.LatencyMs).OrderBy(v => v).ToArray();
                text.AppendLine(string.Join("\t", group.Key, band.Key, latency.Length, latency.Average().ToString("0.00", CultureInfo.InvariantCulture),
                    latency[Math.Min(latency.Length - 1, (int)Math.Ceiling(latency.Length * 0.95) - 1)].ToString("0.00", CultureInfo.InvariantCulture),
                    latency[latency.Length - 1].ToString("0.00", CultureInfo.InvariantCulture),
                    band.Average(d => d.Work).ToString("0", CultureInfo.InvariantCulture)));
            }
            text.AppendLine();
            text.AppendLine("stage\trole\tscript\tloadout\tidle/reach\tretreats/moves\tfieldRetreats/moves\tattacks\tfirstContact\tfinished\terror");
            foreach (var run in runs)
                text.AppendLine(string.Join("\t", run.StageKey, run.Role, run.Script, run.Loadout, $"{run.IdleActorSides}/{run.ReachActorSides}",
                    $"{run.RetreatsWithoutHazard}/{run.Moves}", $"{run.FieldRetreatsWithoutHazard}/{run.Moves}", run.Attacks, run.FirstContactRound?.ToString() ?? "-", run.Finished, run.Error ?? ""));
            return text.ToString();
        }

        // 필드 적 수 구간(계획 R-5·R-6 문턱 6): 5 이하, 6–8, 9–10, 10 초과.
        private static string EnemyBand(int enemies) => enemies <= 5 ? "a:<=5" : enemies <= 8 ? "b:6-8" : enemies <= 10 ? "c:9-10" : "d:>10";

        private static string Ratio(int part, int whole) =>
            whole == 0 ? "-" : (part / (double)whole).ToString("0.000", CultureInfo.InvariantCulture);

        private static void WriteRuns(string path, IReadOnlyList<RunRecord> runs) =>
            WriteLines(path, RunRecord.Header, runs.Select(r => r.ToString()));

        private static void WriteLines(string path, string header, IEnumerable<string> rows)
        {
            using var writer = File.CreateText(path);
            writer.WriteLine(header);
            foreach (string row in rows) writer.WriteLine(row);
        }

        private sealed class RunRecord
        {
            internal const string Header = "stage\tmap\trole\tscript\tloadout\tloadoutDetail\tenemyKeys\tdeployment\trounds\tenemySides\tdecisions\tattacks\tmoves\tends\t" +
                "sidesWithAttack\tfirstContactRound\treachActorSides\tidleActorSides\tidleRatio\tretreats\tretreatsNoHazard\tfieldRetreats\tfieldRetreatsNoHazard\tplayerMoves\t" +
                "enemyDeathsEnemyWindow\tenemyDeathsPlayerWindow\tplayerDeaths\tfirstDecisionWork\tworkTotal\tpreviewTotal\tmeanLatencyMs\tmaxLatencyMs\tfinished\terror\t" +
                "deathsAvoidable\tdeathsUnavoidable\tdeathsOther\ttruncatedActorSides\tdeathNotes";

            internal int StageKey { get; }
            internal int MapKey { get; }
            internal string Role { get; }
            internal string Script { get; }
            internal string Loadout { get; }
            internal string LoadoutDetail { get; set; } = "-";
            internal string EnemyKeys { get; set; } = "-";
            internal string Deployment { get; set; } = "-";
            internal int Rounds { get; set; }
            internal int EnemySides { get; set; }
            internal int PlayerMoves { get; set; }
            internal int EnemyDeathsEnemyWindow { get; set; }
            internal int EnemyDeathsPlayerWindow { get; set; }
            internal int PlayerDeaths { get; set; }
            internal int DeathsAvoidable { get; set; }
            internal int DeathsUnavoidable { get; set; }
            internal int DeathsOther { get; set; }
            internal List<string> DeathNotes { get; } = new();
            internal int TruncatedActorSides => Actors.Count(a => a.Truncated);
            internal bool Finished { get; set; }
            internal string Error { get; set; }
            internal List<DecisionRow> Decisions { get; } = new();
            internal List<ActorRow> Actors { get; } = new();
            internal List<CandidateRow> Candidates { get; } = new();

            internal int Attacks => Decisions.Count(d => d.Kind == "Attack");
            internal int Moves => Decisions.Count(d => d.Kind == "Move");
            internal int Ends => Decisions.Count(d => d.Kind == "End");
            internal int Retreats => Decisions.Count(d => d.Retreat);
            internal int RetreatsWithoutHazard => Decisions.Count(d => d.Retreat && !d.HazardOrigin);
            internal int FieldRetreats => Decisions.Count(d => d.FieldRetreat);
            internal int FieldRetreatsWithoutHazard => Decisions.Count(d => d.FieldRetreat && !d.HazardOrigin);
            internal int SidesWithAttack => Decisions.Where(d => d.Kind == "Attack").Select(d => d.Round).Distinct().Count();
            internal int? FirstContactRound => Decisions.Where(d => d.Kind == "Attack").Select(d => (int?)d.Round).Min();
            internal int ReachActorSides => Actors.Count(a => a.CanReachAttack == true);
            internal int IdleActorSides => Actors.Count(a => a.CanReachAttack == true && !a.Acted);
            internal int FirstDecisionWork => Decisions.Count == 0 ? 0 : Decisions[0].Work;

            internal RunRecord(int stageKey, int mapKey, string role, string script, string loadout)
            {
                StageKey = stageKey;
                MapKey = mapKey;
                Role = role;
                Script = script;
                Loadout = loadout;
            }

            internal void Add(DecisionRow row) => Decisions.Add(row);

            public override string ToString() => string.Join("\t", StageKey, MapKey, Role, Script, Loadout, LoadoutDetail, EnemyKeys, Deployment,
                Rounds, EnemySides, Decisions.Count, Attacks, Moves, Ends, SidesWithAttack, FirstContactRound?.ToString() ?? "-",
                ReachActorSides, IdleActorSides, Ratio(IdleActorSides, ReachActorSides), Retreats, RetreatsWithoutHazard, FieldRetreats, FieldRetreatsWithoutHazard, PlayerMoves,
                EnemyDeathsEnemyWindow, EnemyDeathsPlayerWindow, PlayerDeaths, FirstDecisionWork, Decisions.Sum(d => d.Work),
                Decisions.Sum(d => d.Previews),
                Decisions.Count == 0 ? "-" : Decisions.Average(d => d.LatencyMs).ToString("0.000", CultureInfo.InvariantCulture),
                Decisions.Count == 0 ? "-" : Decisions.Max(d => d.LatencyMs).ToString("0.000", CultureInfo.InvariantCulture),
                Finished, Error ?? "", DeathsAvoidable, DeathsUnavoidable, DeathsOther, TruncatedActorSides, string.Join(" ", DeathNotes));
        }

        private sealed class DecisionRow
        {
            internal const string Header = "stage\tscript\tloadout\tround\tkind\tactor\tactorKey\tfrom\tto\tretreat\tfieldRetreat\thazardOrigin\t" +
                "work\texpanded\tpreviews\tgeneratedAttacks\tgeneratedMoves\tcapHits\tlatencyMs\tfieldEnemies\t" +
                "forecastHits\tforecastMisses\tthreatHits\tthreatMisses";

            private readonly RunRecord _run;
            internal int Round { get; }
            internal string Kind { get; }
            internal long Actor { get; }
            internal string ActorKey { get; }
            internal CellCoord? From { get; }
            internal CellCoord? To { get; set; }
            internal bool Retreat { get; set; }
            // AI 경로 비용 기준 후퇴: 게시한 이동 후보의 교전 진행 항목이 음수(자기 교전 필드 비용이 늘어남, 계획 R-6 뒤 사용자 결정).
            internal bool FieldRetreat { get; set; }
            internal bool HazardOrigin { get; set; }
            internal int Work { get; }
            internal int Expanded { get; }
            internal int Previews { get; }
            internal int GeneratedAttacks { get; }
            internal int GeneratedMoves { get; }
            internal int CapHits { get; }
            internal double LatencyMs { get; }
            // 결정 입력의 필드 적 유닛 수다(지연을 적 수 구간으로 묶는다, 계획 R-5).
            internal int FieldEnemies { get; }
            internal CacheCounts Cache { get; set; }

            internal DecisionRow(RunRecord run, int round, string kind, long actor, string actorKey, CellCoord? from,
                int work, int expanded, int previews, int generatedAttacks, int generatedMoves, int capHits, double latencyMs, int fieldEnemies)
            {
                FieldEnemies = fieldEnemies;
                _run = run;
                Round = round;
                Kind = kind;
                Actor = actor;
                ActorKey = actorKey;
                From = from;
                Work = work;
                Expanded = expanded;
                Previews = previews;
                GeneratedAttacks = generatedAttacks;
                GeneratedMoves = generatedMoves;
                CapHits = capHits;
                LatencyMs = latencyMs;
            }

            public override string ToString() => string.Join("\t", _run.StageKey, _run.Script, _run.Loadout, Round, Kind, Actor, ActorKey,
                Cell(From), Cell(To), Retreat, FieldRetreat, HazardOrigin, Work, Expanded, Previews, GeneratedAttacks, GeneratedMoves, CapHits,
                LatencyMs.ToString("0.000", CultureInfo.InvariantCulture), FieldEnemies, Cache);
        }

        /// <summary>진단 기록 한 줄이다: 행위자의 대기 항목(후보 칸이 비어 있음) 또는 후보 하나의 항목(계획 R-5).</summary>
        private sealed class CandidateRow
        {
            internal const string Header = "stage\tscript\tloadout\tround\tdecision\tactor\trow\tholdReason\tholdTier\tholdLethal\t" +
                "action\ttier\tlethal\tprecise\tpublished\tattack\thazard\tplayerRisk\tprogress\tfollowup\tnextTurn\tbind\ttotal";

            private readonly RunRecord _run;
            private readonly int _round;
            private readonly int _decision;
            private readonly EnemyDecisionDiagnostics.ActorRecord _record;
            private readonly EnemyDecisionDiagnostics.CandidateRecord? _candidate;
            private readonly bool _published;

            internal CandidateRow(RunRecord run, int round, int decision, EnemyDecisionDiagnostics.ActorRecord record,
                EnemyDecisionDiagnostics.CandidateRecord? candidate, bool published)
            {
                _run = run;
                _round = round;
                _decision = decision;
                _record = record;
                _candidate = candidate;
                _published = published;
            }

            public override string ToString() => string.Join("\t", _run.StageKey, _run.Script, _run.Loadout, _round, _decision,
                _record.ActorId.Value, _candidate.HasValue ? "candidate" : "hold", _record.HoldReason?.ToString() ?? "-", _record.HoldTier,
                _record.HoldLethal, _candidate.HasValue ? DescribeAction(_candidate.Value.Action) : "Hold",
                _candidate?.Tier.ToString() ?? "", _candidate?.Lethal.ToString() ?? "", _candidate?.Precise.ToString() ?? "", _published,
                Terms(_candidate?.Terms ?? _record.HoldTerms));
        }

        private sealed class ActorRow
        {
            internal const string Header = "stage\tscript\tloadout\tround\tactor\tactorKey\tanchor\tmoveCells\tcanAttackNow\tcanReachAttack\tacted\tidle\tnote\tholdReason\tfinalLethal\tfinalUnavoidable\ttruncated";

            private readonly RunRecord _run;
            internal int Round { get; }
            internal EntityId ActorId { get; }
            internal string ActorKey { get; }
            internal CellCoord Anchor { get; }
            internal int MoveCells { get; set; }
            internal bool CanAttackNow { get; set; }
            internal bool? CanReachAttack { get; set; }
            internal bool Acted { get; set; }
            internal string Note { get; set; } = "";
            // 진영 안에서 결정 세션이 한 번도 보지 않았다(전투가 그 진영 도중 끝남 등). 행동했으면 "-"다.
            internal string HoldReason { get; set; } = "NotEvaluated";
            internal bool FinalLethal { get; set; }
            internal bool FinalUnavoidable { get; set; }
            internal bool Truncated { get; set; }

            internal ActorRow(RunRecord run, int round, EntityId actorId, string actorKey, CellCoord anchor)
            {
                _run = run;
                Round = round;
                ActorId = actorId;
                ActorKey = actorKey;
                Anchor = anchor;
            }

            public override string ToString() => string.Join("\t", _run.StageKey, _run.Script, _run.Loadout, Round, ActorId.Value, ActorKey,
                Cell(Anchor), MoveCells, CanAttackNow, CanReachAttack?.ToString() ?? "?", Acted, CanReachAttack == true && !Acted, Note,
                HoldReason, FinalLethal, FinalUnavoidable, Truncated);
        }

        // 요청한 스테이지 키와 상관없이 운영 DB의 지정 스테이지 정의를 돌려준다(아군 로스터·덱은 시험 DB 그대로).
        private sealed class ProductionStage : IBattleInitializationSource
        {
            private readonly IDataBaseQueryContext _production;
            private readonly StageRootData _stage;
            internal IBufferQuery Buffers { get; set; }
            internal BattleInitialization Definition { get; private set; }

            internal ProductionStage(IDataBaseQueryContext production, StageRootData stage)
            {
                _production = production;
                _stage = stage;
            }

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = CoreIntegrationTestData.DatabaseSource(Buffers).Read(_production, _stage.MapKey, new StageKey(_stage.StageKey));
                // 코퍼스는 기존 시험 로스터의 서브 저격수를 유지한다. 운영 Main 전환은 되돌리지 않고 역할만 시험 정의에 고정한다.
                var units = source.Units.Select(unit => unit.Key != EntityKey.Unit_Sniper ? unit : new UnitSpec(
                    unit.Key, UnitRole.Regular, unit.FootprintWidth, unit.FootprintHeight, unit.FootprintLocalCells.ToArray(),
                    unit.MaxHealth, unit.MoveMethod, unit.AttackRange, unit.CategoryKey, unit.AttackPower, unit.PhysicalDefense,
                    unit.Priority, unit.Traits.ToArray(), unit.OwnerEligibility, unit.Occlusion, unit.Behavior, unit.KillReward)).ToArray();
                var byKey = units.ToDictionary(unit => unit.Key);
                var profiles = source.AttackProfiles.Select(profile => new CombatAttackProfile(byKey[profile.Definition.Key],
                    profile.Method, profile.OwnedId)).ToArray();
                return Definition = new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, units, source.Structures,
                    source.Categories, profiles, source.Environment, source.EnvironmentBindings, source.ObstaclePlacements, source.WallBlocking,
                    source.ObjectiveKind, source.ItemDefinitions, source.Progression, source.InitialRandomTornadoPlacements, source.ClearRewards,
                    source.TraitGrowth, source.NexusReinforcements, source.PlayerSkill);
            }
        }

        private sealed class DefinitionRead : IBattleDefinitionRead
        {
            private readonly BattleInitialization _definition;
            internal DefinitionRead(BattleInitialization definition) => _definition = definition;
            public BattleInitialization Require(long battleInstanceId) => _definition;
        }
    }
}
