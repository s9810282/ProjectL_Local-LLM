using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 자동 국면에서 진영 시작 또는 적 진행 요청을 만들어 계산하고 게시한다. View 명령을 읽거나 소비하지 않는다.
    /// </summary>
    internal sealed class CombatAdvanceService : ICombatAdvanceService, ISystemInitData<IDataBaseQueryContext>,
        ISystemPostBindService, IExportSystemServices, ISystemTick, ISystemShutdown
    {
        private EnemyDecisionSettings _settings = EnemyDecisionSettings.Default;
        private readonly CombatInputAssembler _assembler;
        private readonly CombatTransitionPublisher _publisher;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private ICombatStateBufferReadSink _combat;
        private IRunProgressBufferReadSink _run;

        private readonly Func<int> _readFrame;
        private readonly Func<long> _readTimestamp;
        private readonly long _timestampFrequency;
        private readonly bool _usePreparation;
        private EnemyDecisionSourceStamp _source;
        private EnemyDecisionSession _session;
        // 적 진영 하나 동안 결정끼리 공유 구조를 다시 쓰는 캐시다(적 AI 근간 개편 설계 6.8). 진영이 끝나거나 전투가 바뀌면 버린다.
        private EnemyDecisionSideCache _sideCache;
        private long _sideCacheBattle;
        private long _sideCacheSequence;
        private RunId _sourceRun;
        private bool _immediateRequired;
        private bool _hasSpentFrame;
        private int _lastSpentFrame;
        // 연구 저장소 전용(LLMTest/docs/2026-11-18-planner-background-parallel-design.md BP7): 턴 계획 AI 계획 시간 기록.
        private long _sessionStartTimestamp;
        private bool _planReported;
        // 연구 저장소 전용 진단(설계 §13): 계획 AI를 기다리는 동안의 프레임 시간(ms)과 그 프레임에 GC 수집이 있었는지.
        private readonly List<(float milliseconds, bool collected)> _waitFrames = new List<(float, bool)>();
        private int _lastCollectionCount = -1;
        // 연구 저장소 전용 실험(병렬 설계 §28): 정한 행동의 정산을 메인 스레드 밖에서 미리 해 두고, 실행 때는 게시만 한다.
        // 실행 인자 -researchPresettle 이 있을 때만 켠다(에디터·기본 실행은 지금 동작 그대로).
        private static readonly bool PresettleEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-researchPresettle") >= 0;
        private CombatCommandInput _sessionInput;
        private EnemyDecision _presettleDecision;
        private Task<CombatCommandTransition> _presettle;
        private int _presettleUsed, _presettleInline;

        internal long PreparationWorkVersion => _session?.WorkVersion ?? 0;
        internal EnemyDecisionDiagnostics PreparationDiagnostics => _session?.Diagnostics;
        internal bool HasPreparation => _source != null;
        internal long AtomicOverrunCount { get; private set; }
        internal long LargestPreparationAtomTicks { get; private set; }
        internal long LastPreparationTicks { get; private set; }
        internal long LastPreparationLargestAtomTicks { get; private set; }
        internal long PreparationSliceCount { get; private set; }
        internal long InputCaptureCount { get; private set; }

        internal CombatAdvanceService(CombatInputAssembler assembler, CombatTransitionPublisher publisher,
            IReadOnlyList<IMovementTileRule> movementRules, Func<int> readFrame, Func<long> readTimestamp,
            long timestampFrequency, bool usePreparation = false)
        {
            _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
            _readFrame = readFrame ?? throw new ArgumentNullException(nameof(readFrame));
            _readTimestamp = readTimestamp ?? throw new ArgumentNullException(nameof(readTimestamp));
            if (timestampFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(timestampFrequency));
            _timestampFrequency = timestampFrequency;
            _usePreparation = usePreparation;
        }

        internal CombatAdvanceService(CombatInputAssembler assembler, CombatTransitionPublisher publisher,
            IReadOnlyList<IMovementTileRule> movementRules)
            : this(assembler, publisher, movementRules, usePreparation: false) { }

        internal CombatAdvanceService(CombatInputAssembler assembler, CombatTransitionPublisher publisher,
            IReadOnlyList<IMovementTileRule> movementRules, bool usePreparation)
            : this(assembler, publisher, movementRules, () => UnityEngine.Time.frameCount,
                Stopwatch.GetTimestamp, Stopwatch.Frequency, usePreparation) { }

        public AdvancePreparationState ReadPreparationState()
        {
            RequireBound();
            if (!_usePreparation || !CanAdvance) return AdvancePreparationState.NotRequired;
            bool current = SourceIsCurrent;
            if (SpentThisFrame) return AdvancePreparationState.FrameDeferred;
            if (!IsEnemyController) return AdvancePreparationState.NotRequired;
            if (!current) return AdvancePreparationState.Required;
            if (_immediateRequired) return AdvancePreparationState.NotRequired;
            return DecisionReady ? AdvancePreparationState.Ready : AdvancePreparationState.Preparing;
        }

        public AdvancePreparationState PrepareAdvanceSlice()
        {
            long start = _readTimestamp();
            RequireBound();
            if (!_usePreparation) return AdvancePreparationState.NotRequired;
            if (SpentThisFrame) return AdvancePreparationState.FrameDeferred;
            if (!CanAdvance || !IsEnemyController)
            {
                CancelPreparation();
                DropSideCache();
                return AdvancePreparationState.NotRequired;
            }
            SpendFrame();
            PreparationSliceCount++;
            LastPreparationLargestAtomTicks = 0;
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyPreparation);
            try
            {
                // 검증·해제 비용도 첫 진행 단위에 포함한다. 첫 단위 뒤에는 기한을 넘겨 다음 단위를 시작하지 않는다.
                if (!SourceIsCurrent) CancelPreparation();
                if (_source == null)
                {
                    _sourceRun = _run.RunId;
                    InputCaptureCount++;
                    var input = _assembler.AssembleDecision(CreateRequest(), out _source);
                    if (input == null || !CanAdvance || _sourceRun != _run.RunId)
                    {
                        CancelPreparation();
                        RecordAtom(start, _readTimestamp());
                        return AdvancePreparationState.Preparing;
                    }
                    _immediateRequired = !CombatCommandCalculator.RequiresEnemyDecision(input);
                    if (!_immediateRequired)
                    {
                        _session = new EnemyDecisionSession(input, _movementRules, _settings, SideCacheFor(input));
                        _sessionInput = input;
                        _sessionStartTimestamp = start;
                        _planReported = false;
                    }
                }
                else if (!_immediateRequired && !_session.IsReady) _session.Step();

                long end = _readTimestamp();
                RecordAtom(start, end);
                // 연구 저장소 전용 연결(설계 BP3): 턴 계획 AI가 일 스레드 계산을 기다리는 동안은 이 프레임을 멈춘다(헛돌지 않게).
                while (!_immediateRequired && !_session.IsReady && !_session.IsWaiting && !BudgetSpent(start, end))
                {
                    long atomStart = end;
                    _session.Step();
                    end = _readTimestamp();
                    RecordAtom(atomStart, end);
                }
                if (!_immediateRequired) ReportTurnPlan();
                if (!_immediateRequired) BeginPresettle();
                RecordWaitFrame(!_immediateRequired && _session != null && _session.IsWaiting);
                return _immediateRequired ? AdvancePreparationState.NotRequired :
                    DecisionReady ? AdvancePreparationState.Ready : AdvancePreparationState.Preparing;
            }
            catch
            {
                CancelPreparation();
                throw;
            }
            finally { LastPreparationTicks = _readTimestamp() - start; }
        }

        public PreparedAdvanceResult AdvancePreparedCombat()
        {
            RequireBound();
            if (SpentThisFrame)
                throw new InvalidOperationException("이번 프레임의 전투 준비·실행 예산을 이미 사용했습니다.");
            if (!CanAdvance)
            {
                CancelPreparation();
                return PreparedAdvanceResult.Cancelled;
            }
            SpendFrame();
            if (!_usePreparation || !SourceIsCurrent || _session == null || !_session.IsReady)
            {
                CancelPreparation();
                return PreparedAdvanceResult.Stale;
            }
            try
            {
                // 미리 정산해 둔 결과가 있으면(§28) 다시 조립·정산하지 않는다. 그 결과는 준비 때 고정한 입력으로 낸 것이고,
                // 그 입력의 원본이 지금도 그대로인지(SourceIsCurrent)는 위와 아래에서 확인한다.
                var fresh = _source;
                var transition = TakePresettled();
                if (transition == null)
                {
                    InputCaptureCount++;
                    var input = _assembler.AssembleDecision(CreateRequest(), out fresh);
                    if (!CanAdvance)
                    {
                        CancelPreparation();
                        return PreparedAdvanceResult.Cancelled;
                    }
                    if (input == null || !SourceIsCurrent || !_assembler.IsCurrent(fresh))
                    {
                        CancelPreparation();
                        return PreparedAdvanceResult.Stale;
                    }
                    using (CoreProfiling.Sample(CoreProfileSection.EnemyActionExecution))
                        transition = CombatCommandCalculator.AdvancePrepared(input, _session.Decision, _movementRules);
                }
                if (!CanAdvance)
                {
                    CancelPreparation();
                    return PreparedAdvanceResult.Cancelled;
                }
                if (!SourceIsCurrent || !_assembler.IsCurrent(fresh))
                {
                    CancelPreparation();
                    return PreparedAdvanceResult.Stale;
                }
                if (!transition.Accepted)
                {
                    DropPresettle();
                    _session.RejectSelected();
                    return PreparedAdvanceResult.RetryPreparation;
                }
                bool published;
                using (CoreProfiling.Sample(CoreProfileSection.EnemyActionExecution))
                    published = _publisher.Publish(transition);
                CancelPreparation();
                return published ? PreparedAdvanceResult.Published : PreparedAdvanceResult.Cancelled;
            }
            catch
            {
                CancelPreparation();
                throw;
            }
        }

        public void CancelPreparation()
        {
            var session = _session;
            _session = null;
            _source = null;
            _sourceRun = default;
            _immediateRequired = false;
            _sessionInput = null;
            DropPresettle();
            session?.Dispose();
            _waitFrames.Clear();
            _lastCollectionCount = -1;
        }

        /// <summary>결정이 나왔고, 미리 정산을 켰으면 그 정산도 끝났다. 정산이 끝날 때까지는 "준비 중"으로 답해 메인 스레드가 기다리지 않게 한다.</summary>
        private bool DecisionReady => _session != null && _session.IsReady &&
            (!PresettleEnabled || _presettle != null && ReferenceEquals(_presettleDecision, _session.Decision) && _presettle.IsCompleted);

        /// <summary>
        /// 연구 저장소 전용 실험(§28): 결정이 나오면 그 행동의 정산을 일 스레드에 맡긴다. 입력은 준비 때 고정한 불변 입력이고
        /// 정산은 그 입력과 결정만 읽으므로(계획 AI가 스레드에서 도는 정산과 같은 계산기) 메인 스레드와 함께 돌아도 된다.
        /// </summary>
        private void BeginPresettle()
        {
            if (!PresettleEnabled || _session == null || !_session.IsReady) return;
            var decision = _session.Decision;
            if (_presettle != null && ReferenceEquals(_presettleDecision, decision)) return;
            DropPresettle();
            var input = _sessionInput;
            var rules = _movementRules;
            _presettleDecision = decision;
            _presettle = Task.Run(() => CombatCommandCalculator.AdvancePrepared(input, decision, rules));
        }

        /// <summary>지금 결정으로 미리 정산해 둔 결과다. 없으면 null이다. 아직 덜 끝났으면 끝날 때까지 기다리고, 정산이 던진 예외는 여기서 다시 던진다.</summary>
        private CombatCommandTransition TakePresettled()
        {
            if (_presettle == null || !ReferenceEquals(_presettleDecision, _session.Decision))
            {
                if (PresettleEnabled) _presettleInline++;
                return null;
            }
            var task = _presettle;
            _presettle = null;
            _presettleDecision = null;
            _presettleUsed++;
            return task.GetAwaiter().GetResult();
        }

        /// <summary>쓰지 않게 된 미리 정산을 버린다. 도는 중이면 끝까지 돌게 두되(읽기만 한다), 그 예외는 받아 버린다.</summary>
        private void DropPresettle()
        {
            var task = _presettle;
            _presettle = null;
            _presettleDecision = null;
            task?.ContinueWith(static finished => { _ = finished.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Tick(float deltatime)
        {
            if (_source != null && (!CanAdvance || !IsEnemyController || !SourceIsCurrent)) CancelPreparation();
            if (_sideCache != null && !SideCacheIsCurrent) DropSideCache();
        }

        public void Shutdown()
        {
            CancelPreparation();
            DropSideCache();
        }

        /// <summary>
        /// 이 입력의 적 진영 캐시다. 진영 시작(처음 준비)에 만들고, 같은 전투·같은 진영 턴 순번이면 같은 캐시를 넘긴다.
        /// 준비를 취소해도(게시 뒤 등) 진영이 이어지면 캐시는 남는다.
        /// </summary>
        private EnemyDecisionSideCache SideCacheFor(CombatCommandInput input)
        {
            long sequence = input.State.Turn.ControllerTurnSequence;
            if (_sideCache == null || _sideCacheBattle != input.BattleInstanceId || _sideCacheSequence != sequence)
            {
                _sideCache = new EnemyDecisionSideCache();
                _sideCacheBattle = input.BattleInstanceId;
                _sideCacheSequence = sequence;
            }
            return _sideCache;
        }

        // 적 진영이 이어지는 동안만 참이다: 자동 진행 중, 적 제어자, 같은 전투, 같은 진영 턴 순번.
        private bool SideCacheIsCurrent => CanAdvance && IsEnemyController && _combat.BattleInstanceId == _sideCacheBattle &&
            _combat.Current.CommandState.Turn.ControllerTurnSequence == _sideCacheSequence;

        private void DropSideCache() => _sideCache = null;

        internal EnemyDecisionSideCache SideCache => _sideCache;

        private bool SourceIsCurrent => _source != null && _sourceRun == _run.RunId && _assembler.IsCurrent(_source);
        private bool SpentThisFrame => _hasSpentFrame && _lastSpentFrame == _readFrame();
        private bool CanAdvance => !RunEnded && _combat.HasBattle && _combat.Completion == null &&
            _combat.Phase == CombatFlowPhase.Automatic;
        private bool IsEnemyController => _combat.Current.CommandState.Turn.ControllerPhase == CombatControllerPhase.Enemy;

        private void SpendFrame()
        {
            _lastSpentFrame = _readFrame();
            _hasSpentFrame = true;
        }

        private bool BudgetSpent(long start, long end) => (double)(end - start) * 1000 >= _timestampFrequency;

        /// <summary>
        /// 연구 저장소 전용(설계 BP7): 턴 계획 AI가 새로 계획해 결정이 나오면, 준비를 시작한 뒤 걸린 시간과 계산량을 콘솔에 한 줄 남긴다.
        /// 보관한 계획에서 꺼낸 결정은 남기지 않는다. 쓴 성향 값(조정 에셋 프리셋, Core 이식 설계 §11)도 같은 줄에 적는다.
        /// </summary>
        private static string StyleText(EnemyTurnPlannerStyle style) =>
            $"성향 겁 {style.Fear:0.##} 기회 {style.Opportunity:0.##} 메인 배수 {style.MainWeight:0.##} 메인 거리 {style.GoalStepPercent:0.##}% " +
            $"걸음 {style.StepPenalty:0.##} 증원 {style.ReinforcementDiscount:0.##} 한 턴 더 {(style.Lookahead ? "켬" : "끔")} " +
            $"빔 {style.Beam} 후보 {style.PerUnit}/{style.MoveOnly} 다시 매김 {style.Rerank} " +
            $"행동 규칙 {style.MustAct:0.##} 걸음 대상 고정 {(style.StepAnchor ? "켬" : "끔")} 기회 모으기 {(style.PooledChance ? "켬" : "끔")}";

        private void ReportTurnPlan()
        {
            if (_settings.DecisionModel != EnemyDecisionModel.TurnPlanner || _planReported || !_session.IsReady) return;
            _planReported = true;
            var diagnostics = _session.Diagnostics;
            if (diagnostics.CacheHits > 0 && diagnostics.CapHits == 0) return;
            double seconds = (double)(_readTimestamp() - _sessionStartTimestamp) / _timestampFrequency;
            global::UnityEngine.Debug.Log($"[턴 계획 AI] 준비 시작부터 결정까지 {seconds:F1}초, 정산 {diagnostics.PreviewCalls}번, 판 매김 {diagnostics.ExpandedNodes}번" +
                (diagnostics.CapHits > 0 ? $", 지금 AI로 대신함 {diagnostics.CapHits}번" : "") +
                (PresettleEnabled ? $", 미리 정산 씀 {_presettleUsed}번·못 씀 {_presettleInline}번" : "") + " | " + StyleText(_settings.PlannerStyle));
            ReportWaitFrames();
        }

        /// <summary>
        /// 연구 저장소 전용 진단(설계 §13): 기다리는 프레임마다 앞 프레임의 길이(<c>Time.unscaledDeltaTime</c>)와,
        /// 그 사이 GC 수집 횟수(<c>GC.CollectionCount(0)</c>)가 늘었는지를 모은다. 앞 프레임 길이를 다음 프레임에 읽으므로
        /// 수집 표시는 "이번 길이를 잰 구간 안에 수집이 끝났나"다.
        /// </summary>
        private void RecordWaitFrame(bool waiting)
        {
            if (!waiting) return;
            int collections = GC.CollectionCount(0);
            bool collected = _lastCollectionCount >= 0 && collections != _lastCollectionCount;
            if (_lastCollectionCount >= 0)
                _waitFrames.Add((global::UnityEngine.Time.unscaledDeltaTime * 1000f, collected));
            _lastCollectionCount = collections;
        }

        /// <summary>모은 프레임을 한 줄로: 수, 중간값·90%·가장 긴 값, 8.3ms를 넘은 수, GC 수집이 있던 프레임 수와 그중 8.3ms를 넘은 수.</summary>
        private void ReportWaitFrames()
        {
            if (_waitFrames.Count == 0) return;
            var lengths = new List<float>(_waitFrames.Count);
            int over = 0, collectedFrames = 0, collectedOver = 0;
            foreach (var (milliseconds, collected) in _waitFrames)
            {
                lengths.Add(milliseconds);
                bool isOver = milliseconds > 8.33f;
                if (isOver) over++;
                if (collected) collectedFrames++;
                if (collected && isOver) collectedOver++;
            }
            lengths.Sort();
            float At(double fraction) => lengths[Math.Min(lengths.Count - 1, (int)(fraction * lengths.Count))];
            global::UnityEngine.Debug.Log($"[턴 계획 AI] 기다린 프레임 {lengths.Count}개: 중간 {At(0.5):F1}ms, 90% {At(0.9):F1}ms, 가장 김 {lengths[lengths.Count - 1]:F1}ms, " +
                $"8.3ms 넘음 {over}개 | GC 수집 있던 프레임 {collectedFrames}개(그중 8.3ms 넘음 {collectedOver}개)");
            _waitFrames.Clear();
            _lastCollectionCount = -1;
        }

        private void RecordAtom(long start, long end)
        {
            long elapsed = end - start;
            if (elapsed < 0) throw new InvalidOperationException("준비 측정 시계는 단조 증가해야 합니다.");
            LargestPreparationAtomTicks = Math.Max(LargestPreparationAtomTicks, elapsed);
            LastPreparationLargestAtomTicks = Math.Max(LastPreparationLargestAtomTicks, elapsed);
            if ((double)elapsed * 1000 > _timestampFrequency) AtomicOverrunCount++;
        }

        private CombatActionRequest CreateRequest()
        {
            var turn = _combat.Current.CommandState.Turn;
            return new CombatActionRequest(0, turn.ControllerPhase == CombatControllerPhase.Enemy
                    ? CombatCommandKind.CompleteEnemyTurn : CombatCommandKind.StartControllerTurn,
                default, default, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: _combat.BattleInstanceId,
                expectedControllerTurnSequence: turn.ControllerTurnSequence);
        }

        private void RequireBound()
        {
            if (_combat == null || _run == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
        }

        /// <summary>적 AI 기획 조정 테이블을 읽어 결정 설정을 만든다. 테이블이 없으면 계약 기본값이다(설계 6.5.1).</summary>
        public void InitData(IDataBaseQueryContext data)
        {
            var query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
            _settings = (query as IHasEnemyAiTuningTable)?.EnemyAiTuning?.Tuning.ToSettings() ?? EnemyDecisionSettings.Default;
        }

        /// <summary>준비 경로가 결정 세션에 넘기는 설정이다.</summary>
        internal EnemyDecisionSettings DecisionSettings => _settings;

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _run = buffers.RunProgressReadSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatAdvanceService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool AdvanceCombat()
        {
            RequireBound();
            if (RunEnded) return false;
            if (!_combat.HasBattle || _combat.Phase != CombatFlowPhase.Automatic)
                return false;

            if (_usePreparation)
            {
                if (SpentThisFrame) throw new InvalidOperationException("이번 프레임의 전투 예산을 이미 사용했습니다.");
                SpendFrame();
            }
            CancelPreparation();

            using var sample = CoreProfilerMarkers.Advance.Auto();
            var input = _assembler.Assemble(CreateRequest());
            if (RunEnded) return false;
            if (_usePreparation && CombatCommandCalculator.RequiresEnemyDecision(input))
                throw new InvalidOperationException("준비 경로의 적 행동은 준비된 행동 실행으로만 처리해야 합니다.");
            CombatCommandTransition transition;
            using (CoreProfilerMarkers.Calculate.Auto())
                transition = CombatCommandCalculator.Advance(input, _movementRules);
            return _publisher.Publish(transition);
        }

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;
    }
}
