using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    #region 서비스

    /// <summary>
    /// 튜토리얼에 값 명령을 제출합니다(튜토리얼 설계 §4). true는 요청 Buffer에 보관됐다는 뜻이며 수락이나 완료가 아닙니다.
    /// 제출 뒤 <c>Register(JobRootType.Tutorial, JobType.Next)</c>로 처리를 앞당길 수 있지만, 빠뜨려도 실행 소유자가 등록합니다.
    /// 입력 처리(콜백) 안에서는 제출만 하고 Job 등록은 하지 않습니다.
    /// </summary>
    public interface ITutorialRequestBufferWriteSink : IServiceModule
    {
        /// <summary>
        /// 명령을 보관하고 번호를 발급합니다. 초기화 전이거나 런타임이 멈췄으면 false와 번호 0을 돌려주고 보관하지 않습니다.
        /// 형식이 잘못된 명령은 <see cref="ArgumentException"/>, 번호 소진은 <see cref="InvalidOperationException"/>이며
        /// 어느 쪽도 번호를 쓰지 않습니다. 의미상 거절(정의 없음, 이미 실행 중 등)은 결과로 게시합니다.
        /// </summary>
        bool TrySubmit(TutorialCommandData command, out int requestNumber, out TutorialSubmitFailure submitFailure);
    }

    /// <summary>
    /// 요청 번호에 대응하는 처리 결과를 비파괴로 읽습니다(튜토리얼 설계 §7·§11). 결과는 런타임 동안 보존하며 지우는 API는 없습니다.
    /// 발급받은 번호 N의 결과가 없고 N이 <see cref="HandledRequestNumber"/>보다 크면 아직 미처리입니다.
    /// </summary>
    public interface ITutorialResultBufferReadSink : IServiceModule
    {
        /// <summary>결과가 게시된 연속된 마지막 요청 번호입니다. 0은 아직 없음입니다. 늘었다고 기능이 성공한 것은 아닙니다.</summary>
        int HandledRequestNumber { get; }

        /// <summary>요청 번호의 결과를 읽습니다. 미처리이거나 발급하지 않은 번호면 false입니다.</summary>
        bool TryGetResult(int requestNumber, out TutorialRequestResult result);
    }

    /// <summary>실행 환경, 공개 정의, 세션 상태, 전투 입력 제한을 비파괴로 읽습니다(튜토리얼 설계 §4·§5·§8).</summary>
    public interface ITutorialStateBufferReadSink : IServiceModule
    {
        /// <summary>현재 화면 연결과 사용 가능 여부, 활성 세션입니다. 바뀔 때만 새 객체입니다.</summary>
        TutorialContextSnapshot Context { get; }

        /// <summary>지금 요청할 수 있도록 등록된 정의입니다. 목록에 있다고 실행 시점의 수락이 예약되지는 않습니다.</summary>
        IReadOnlyList<TutorialDefinitionInfo> AvailableDefinitions { get; }

        /// <summary>세션을 읽습니다. 끝난 세션도 런타임 동안 보존하므로 다음 Start 뒤에도 읽을 수 있습니다.</summary>
        bool TryGetSession(int sessionId, out TutorialSessionSnapshot session);

        /// <summary>
        /// 활성 세션이 전투 입력에 거는 제한입니다(설계 §5). 정의의 단계 순서와 현재 단계·대기 이유로 매번 계산합니다.
        /// 제한 실습 단계이거나, 그 실습을 통과한 뒤 바로 다음 설명이 시작 조건을 기다리는 동안 이어집니다.
        /// </summary>
        TutorialCombatInputRestriction CombatInputRestriction { get; }
    }

    /// <summary>
    /// 튜토리얼 실행 소유자와 TutorialPlan·Job만 쓰는 내부 저장 계약입니다(튜토리얼 설계 §9.2·§10).
    /// 결과·처리 번호·세션 변경은 <see cref="Publish"/> 한 번에 검증한 뒤 함께 설치합니다.
    /// </summary>
    internal interface ITutorialBufferStore : IServiceModule
    {
        /// <summary>
        /// 공개 정의와 단계 규칙을 한 번 설치하고 제출을 받기 시작합니다. 두 목록의 정의 키는 같아야 합니다.
        /// 이후 연결이 없으면 NoContext입니다.
        /// </summary>
        void Initialize(IReadOnlyList<TutorialDefinitionInfo> definitions, IReadOnlyList<TutorialDefinitionRules> rules);

        /// <summary>
        /// 화면 연결을 바꿉니다. 사용 가능이면 번호가 있어야 하고, 연결이 없으면 번호 0과 NoContext입니다.
        /// 새 번호는 이전에 쓴 번호보다 커야 합니다. 런타임이 멈춘 뒤에는 무시합니다(정지 뒤의 화면 해제).
        /// </summary>
        void SetContext(int contextId, TutorialUnavailableReason unavailableReason);

        /// <summary>
        /// 초기화 때 받은 정의의 단계 규칙입니다. TutorialPlan이 단계 순서와 실습 통과 조건을 여기서 읽습니다(구현 계획 §12.3).
        /// 정의가 없으면 상태와 정의가 어긋난 것이므로 <see cref="InvalidOperationException"/>입니다.
        /// </summary>
        TutorialDefinitionRules GetRules(int tutorialKey);

        /// <summary>다음 미처리 요청입니다. 번호는 항상 처리 번호 + 1입니다.</summary>
        bool TryGetNextRequest(out TutorialRequest request);

        /// <summary>
        /// 다음 미처리 요청의 결과를 게시하고, 있으면 그 요청이 바꾼 세션을 함께 설치합니다.
        /// 받아들인 Start만 새 세션을 만들고, 거절·실패 결과는 세션을 바꾸지 않습니다.
        /// 검증에 실패하면 아무것도 바꾸지 않고 <see cref="InvalidOperationException"/>을 던집니다.
        /// </summary>
        void Publish(TutorialRequestResult result, TutorialSessionSnapshot session = null);

        /// <summary>요청 없이 바뀐 기존 세션(내부 진행)을 설치합니다. 새 세션은 만들 수 없고 전이 규칙은 <see cref="Publish"/>와 같습니다.</summary>
        void InstallSession(TutorialSessionSnapshot session);

        /// <summary>런타임 정지를 기록합니다. 이후 제출은 RuntimeStopped로 실패하고, 남은 요청 마감은 소유자가 게시합니다.</summary>
        void StopRuntime();
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// 튜토리얼 요청 큐·결과·세션 상태의 유일한 원본입니다(튜토리얼 설계 §10).
    /// 번호는 런타임 동안 단조 증가하며 재사용하지 않고, 결과와 종료 세션은 런타임 동안 보존합니다(설계 §11).
    /// 세션 상태는 설치 전에 단계 규칙·전이 규칙을 모두 검사하므로 실패한 설치는 아무것도 바꾸지 않습니다.
    /// </summary>
    internal sealed class TutorialBuffer :
        ITutorialRequestBufferWriteSink,
        ITutorialResultBufferReadSink,
        ITutorialStateBufferReadSink,
        ITutorialBufferStore,
        IExportSystemServices
    {
        // 아직 결과가 없는 요청입니다. 번호 오름차순이며 맨 앞이 처리 번호 + 1입니다.
        private readonly List<TutorialRequest> _pending = new();
        private readonly Dictionary<int, TutorialRequestResult> _results = new();
        private readonly Dictionary<int, TutorialSessionSnapshot> _sessions = new();
        // 정의 키 → 단계 키 → 정의에서 미리 계산한 단계 규칙. 초기화 때 한 번 만든다.
        private readonly Dictionary<int, Dictionary<int, StepEntry>> _steps = new();
        // 정의 키 → 받은 단계 규칙 그대로. 불변이라 사본을 만들지 않는다.
        private readonly Dictionary<int, TutorialDefinitionRules> _rules = new();
        private IReadOnlyList<TutorialDefinitionInfo> _definitions = Array.Empty<TutorialDefinitionInfo>();
        private int _lastIssuedNumber;
        private int _handledRequestNumber;
        private bool _isInitialized;
        private bool _isRuntimeStopped;
        // 화면 연결 사실만 둔다. 초기화 전·정지는 위 두 값에서 공개 사유를 계산한다.
        private int _contextId;
        private TutorialUnavailableReason _connectionReason = TutorialUnavailableReason.NoContext;
        private int _lastContextId;
        private int _activeSessionId;
        private TutorialContextSnapshot _context;

        public int HandledRequestNumber => _handledRequestNumber;
        public IReadOnlyList<TutorialDefinitionInfo> AvailableDefinitions => _definitions;

        // 바뀔 때만 새로 만든다. 매 Tick 읽어도 새 객체를 만들지 않는다.
        public TutorialContextSnapshot Context => _context ??= CreateContext();

        public TutorialCombatInputRestriction CombatInputRestriction
        {
            get
            {
                if (_activeSessionId == 0)
                    return TutorialCombatInputRestriction.None;

                TutorialSessionSnapshot session = _sessions[_activeSessionId];
                if (session.Status != TutorialSessionStatus.Running)
                    return TutorialCombatInputRestriction.None;

                StepEntry step = _steps[session.TutorialKey][session.CurrentStepKey];
                if (session.StepKind == TutorialStepKind.Practice)
                    return step.Restriction;
                if (session.StepKind == TutorialStepKind.Explanation
                    && session.WaitReason == TutorialWaitReason.StartCondition)
                    return step.BridgeRestriction;
                return TutorialCombatInputRestriction.None;
            }
        }

        public bool TrySubmit(TutorialCommandData command, out int requestNumber, out TutorialSubmitFailure submitFailure)
        {
            if (command.Kind == TutorialCommandKind.None || !System.Enum.IsDefined(typeof(TutorialCommandKind), command.Kind))
                throw new ArgumentException("명령 종류가 없습니다. TutorialCommandData의 생성 함수로 만드세요.", nameof(command));

            requestNumber = 0;
            if (_isRuntimeStopped)
            {
                submitFailure = TutorialSubmitFailure.RuntimeStopped;
                return false;
            }

            if (!_isInitialized)
            {
                submitFailure = TutorialSubmitFailure.NotInitialized;
                return false;
            }

            if (_lastIssuedNumber == int.MaxValue)
                throw new InvalidOperationException("튜토리얼 요청 번호를 더 발급할 수 없습니다.");

            int number = _lastIssuedNumber + 1;
            _pending.Add(new TutorialRequest(number, command));
            _lastIssuedNumber = number;
            requestNumber = number;
            submitFailure = TutorialSubmitFailure.None;
            return true;
        }

        public bool TryGetResult(int requestNumber, out TutorialRequestResult result)
        {
            return _results.TryGetValue(requestNumber, out result);
        }

        public bool TryGetSession(int sessionId, out TutorialSessionSnapshot session)
        {
            return _sessions.TryGetValue(sessionId, out session);
        }

        public void Initialize(IReadOnlyList<TutorialDefinitionInfo> definitions,
            IReadOnlyList<TutorialDefinitionRules> rules)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));
            if (_isInitialized)
                throw new InvalidOperationException("튜토리얼 Buffer는 한 번만 초기화합니다.");

            var copied = new List<TutorialDefinitionInfo>(definitions.Count);
            var keys = new HashSet<int>();
            for (int i = 0; i < definitions.Count; i++)
            {
                TutorialDefinitionInfo definition = definitions[i]
                    ?? throw new ArgumentException($"정의 목록 {i}번이 비어 있습니다.", nameof(definitions));
                if (!keys.Add(definition.TutorialKey))
                    throw new ArgumentException($"정의 키 {definition.TutorialKey}가 중복됩니다.", nameof(definitions));
                copied.Add(definition);
            }

            var steps = BuildSteps(rules);
            if (steps.Count != keys.Count || !keys.SetEquals(steps.Keys))
                throw new ArgumentException("공개 정의와 단계 규칙의 정의 키가 같아야 합니다.", nameof(rules));

            foreach (KeyValuePair<int, Dictionary<int, StepEntry>> pair in steps)
                _steps.Add(pair.Key, pair.Value);
            foreach (TutorialDefinitionRules definition in rules)
                _rules.Add(definition.TutorialKey, definition);
            _definitions = new ReadOnlyCollection<TutorialDefinitionInfo>(copied);
            _isInitialized = true;
            _context = null;
        }

        public void SetContext(int contextId, TutorialUnavailableReason unavailableReason)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("초기화 전에는 화면 연결을 바꿀 수 없습니다.");
            if (_isRuntimeStopped)
                return;
            if (contextId < 0)
                throw new ArgumentOutOfRangeException(nameof(contextId), contextId, "0 이상이어야 합니다.");
            if (unavailableReason != TutorialUnavailableReason.None
                && unavailableReason != TutorialUnavailableReason.NoContext
                && unavailableReason != TutorialUnavailableReason.UnsupportedContext)
                throw new ArgumentOutOfRangeException(nameof(unavailableReason), unavailableReason,
                    "화면 연결 사유는 None·NoContext·UnsupportedContext입니다. 초기화·정지는 Buffer가 정합니다.");
            if (unavailableReason == TutorialUnavailableReason.None && contextId == 0)
                throw new ArgumentException("사용할 수 있는 환경에는 화면 연결 번호가 있어야 합니다.", nameof(contextId));
            if (unavailableReason == TutorialUnavailableReason.NoContext && contextId != 0)
                throw new ArgumentException("연결이 없으면 화면 연결 번호는 0입니다.", nameof(contextId));
            if (contextId != 0 && contextId != _contextId && contextId <= _lastContextId)
                throw new ArgumentException(
                    $"화면 연결 번호는 다시 쓰지 않습니다. 새 번호={contextId}, 지금까지 쓴 가장 큰 번호={_lastContextId}.",
                    nameof(contextId));

            _contextId = contextId;
            _connectionReason = unavailableReason;
            if (contextId > _lastContextId)
                _lastContextId = contextId;
            _context = null;
        }

        public TutorialDefinitionRules GetRules(int tutorialKey)
        {
            if (_rules.TryGetValue(tutorialKey, out TutorialDefinitionRules rules))
                return rules;

            throw new InvalidOperationException($"정의 {tutorialKey}의 단계 규칙이 없습니다.");
        }

        public bool TryGetNextRequest(out TutorialRequest request)
        {
            if (_pending.Count == 0)
            {
                request = default;
                return false;
            }

            request = _pending[0];
            return true;
        }

        public void Publish(TutorialRequestResult result, TutorialSessionSnapshot session = null)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (_pending.Count == 0 || _pending[0].Number != result.RequestNumber)
                throw new InvalidOperationException(
                    $"결과는 다음 미처리 요청에만 게시합니다. 요청={result.RequestNumber}, 처리 번호={_handledRequestNumber}.");

            TutorialRequest request = _pending[0];
            RequireResultMatchesRequest(request, result, session);
            if (session != null)
                RequireInstallable(session, request.Data.Kind == TutorialCommandKind.Start);

            _results.Add(result.RequestNumber, result);
            _handledRequestNumber = result.RequestNumber;
            _pending.RemoveAt(0);
            if (session != null)
                Apply(session);
        }

        public void InstallSession(TutorialSessionSnapshot session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            RequireInstallable(session, false);
            Apply(session);
        }

        public void StopRuntime()
        {
            _isRuntimeStopped = true;
            _context = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ITutorialRequestBufferWriteSink>(this);
            sink.AddService<ITutorialResultBufferReadSink>(this);
            sink.AddService<ITutorialStateBufferReadSink>(this);
            sink.AddService<ITutorialBufferStore>(this);
        }

        private TutorialContextSnapshot CreateContext()
        {
            if (_isRuntimeStopped)
                return new TutorialContextSnapshot(0, TutorialUnavailableReason.RuntimeStopped, _activeSessionId);
            if (!_isInitialized)
                return new TutorialContextSnapshot(0, TutorialUnavailableReason.NotInitialized, _activeSessionId);
            return new TutorialContextSnapshot(_contextId, _connectionReason, _activeSessionId);
        }

        // 정의마다 단계 키 → 규칙 표를 만든다. 설명 구간의 첫 설명은 바로 앞 단계(실습)의 제한을 시작 조건 대기 동안 잇는다(설계 §5).
        private static Dictionary<int, Dictionary<int, StepEntry>> BuildSteps(IReadOnlyList<TutorialDefinitionRules> rules)
        {
            var result = new Dictionary<int, Dictionary<int, StepEntry>>();
            for (int i = 0; i < rules.Count; i++)
            {
                TutorialDefinitionRules definition = rules[i]
                    ?? throw new ArgumentException($"단계 규칙 {i}번이 비어 있습니다.", nameof(rules));
                if (result.ContainsKey(definition.TutorialKey))
                    throw new ArgumentException($"정의 키 {definition.TutorialKey}의 단계 규칙이 중복됩니다.", nameof(rules));

                var steps = new Dictionary<int, StepEntry>(definition.Steps.Count);
                TutorialStepRule previous = null;
                foreach (TutorialStepRule step in definition.Steps)
                {
                    TutorialStepKind previousKind = previous?.Kind ?? TutorialStepKind.None;
                    TutorialCombatInputRestriction bridge =
                        TutorialDefinitionRules.StartsExplanationSegment(previousKind, step.Kind) && previous != null
                            ? previous.Restriction
                            : TutorialCombatInputRestriction.None;
                    steps.Add(step.StepKey, new StepEntry(step.Kind, step.Restriction, bridge));
                    previous = step;
                }

                result.Add(definition.TutorialKey, steps);
            }

            return result;
        }

        // 결과가 요청과 맞는지 본다(설계 §5·§7). 세션 설치는 받아들인 결과만 하며, 새 세션은 받아들인 Start만 만든다.
        private void RequireResultMatchesRequest(TutorialRequest request, TutorialRequestResult result,
            TutorialSessionSnapshot session)
        {
            if (request.Data.Kind != result.CommandKind)
                throw new InvalidOperationException(
                    $"요청 {result.RequestNumber}의 명령 종류와 결과가 다릅니다. 요청={request.Data.Kind}, 결과={result.CommandKind}.");

            bool accepted = result.Disposition == TutorialDisposition.Accepted;
            if (request.Data.Kind == TutorialCommandKind.Start)
            {
                if (!accepted)
                {
                    if (result.SessionId != 0)
                        throw new InvalidOperationException("거절·실패한 Start에는 세션이 없습니다.");
                }
                else
                {
                    if (result.SessionId != request.Number)
                        throw new InvalidOperationException("받아들인 Start의 세션 번호는 요청 번호입니다.");
                    if (session == null || _sessions.ContainsKey(session.SessionId))
                        throw new InvalidOperationException("받아들인 Start는 새 세션을 함께 설치합니다.");
                    if (session.TutorialKey != request.Data.TutorialKey
                        || session.ContextId != request.Data.ExpectedContextId)
                        throw new InvalidOperationException("새 세션은 Start가 지정한 정의와 화면 연결이어야 합니다.");
                }
            }
            else if (result.SessionId != request.Data.SessionId)
            {
                throw new InvalidOperationException(
                    $"후속 명령 결과의 세션({result.SessionId})은 명령이 지정한 대상({request.Data.SessionId})이어야 합니다.");
            }

            if (session == null)
                return;
            if (!accepted)
                throw new InvalidOperationException("거절·실패 결과는 세션을 바꾸지 않습니다.");
            if (session.SessionId != result.SessionId)
                throw new InvalidOperationException(
                    $"결과의 세션({result.SessionId})과 설치할 세션({session.SessionId})이 다릅니다.");
        }

        // 설치 전에 모두 검사한다. 실패하면 Buffer는 그대로다(설계 §9.2).
        private void RequireInstallable(TutorialSessionSnapshot session, bool allowCreate)
        {
            if (_sessions.TryGetValue(session.SessionId, out TutorialSessionSnapshot existing))
            {
                RequireTransition(existing, session);
            }
            else
            {
                if (!allowCreate)
                    throw new InvalidOperationException("새 세션은 받아들인 Start 결과로만 만듭니다.");
                if (session.Status != TutorialSessionStatus.Starting)
                    throw new InvalidOperationException("새 세션은 Starting으로만 만듭니다.");
                if (!_steps.ContainsKey(session.TutorialKey))
                    throw new InvalidOperationException($"정의 {session.TutorialKey}의 단계 규칙이 없습니다.");
            }

            RequireStepInDefinition(session);
            if (session.IsActive && _activeSessionId != 0 && _activeSessionId != session.SessionId)
                throw new InvalidOperationException(
                    $"활성 세션은 하나뿐입니다. 활성={_activeSessionId}, 설치={session.SessionId}.");
        }

        private void RequireStepInDefinition(TutorialSessionSnapshot session)
        {
            if (session.CurrentStepKey == 0)
                return;
            if (!_steps[session.TutorialKey].TryGetValue(session.CurrentStepKey, out StepEntry step))
                throw new InvalidOperationException(
                    $"정의 {session.TutorialKey}에 단계 {session.CurrentStepKey}가 없습니다.");
            if (session.StepKind != TutorialStepKind.None && session.StepKind != step.Kind)
                throw new InvalidOperationException(
                    $"단계 {session.CurrentStepKey}의 종류가 정의({step.Kind})와 다릅니다: {session.StepKind}.");
        }

        // 이전 상태와의 관계를 검사한다(설계 §6.1·§8.2).
        private static void RequireTransition(TutorialSessionSnapshot previous, TutorialSessionSnapshot next)
        {
            int id = next.SessionId;
            if (previous.IsFinal)
                throw new InvalidOperationException($"세션 {id}은 이미 최종 상태로 확정됐습니다.");
            if (previous.ContextId != next.ContextId || previous.TutorialKey != next.TutorialKey)
                throw new InvalidOperationException($"세션 {id}의 화면 연결이나 정의는 바뀌지 않습니다.");
            if (previous.BattleInstanceId != next.BattleInstanceId
                || previous.StartResultSequence != next.StartResultSequence)
                throw new InvalidOperationException($"세션 {id}의 전투 기준점은 Start를 받아들일 때 정하고 바꾸지 않습니다.");

            bool leaving = next.Status == TutorialSessionStatus.Ending || next.IsFinal;
            if (next.StepRevision == previous.StepRevision)
            {
                if (next.CurrentStepKey != previous.CurrentStepKey)
                    throw new InvalidOperationException($"세션 {id}: revision이 같으면 단계도 같아야 합니다.");
            }
            else if (leaving || next.StepRevision != previous.StepRevision + 1)
            {
                throw new InvalidOperationException(
                    $"세션 {id}: revision은 새 단계에 들어갈 때만 1씩 늘어납니다. 이전={previous.StepRevision}, 다음={next.StepRevision}.");
            }

            switch (previous.Status)
            {
                case TutorialSessionStatus.Starting:
                case TutorialSessionStatus.Running:
                    if (next.Status == TutorialSessionStatus.Completed)
                        throw new InvalidOperationException($"세션 {id}: Completed는 Ending(Finished)을 거쳐야 합니다.");
                    if (previous.Status == TutorialSessionStatus.Running && next.Status == TutorialSessionStatus.Starting)
                        throw new InvalidOperationException($"세션 {id}: Running에서 Starting으로 돌아갈 수 없습니다.");
                    break;
                case TutorialSessionStatus.Ending:
                    if (!leaving)
                        throw new InvalidOperationException($"세션 {id}: Ending에서 진행 중으로 돌아갈 수 없습니다.");
                    if (!KeepsEndIntent(previous.EndReason, next))
                        throw new InvalidOperationException(
                            $"세션 {id}: Ending에서 고정한 종료 의도({previous.EndReason})를 바꿀 수 없습니다: {next.Status}/{next.EndReason}.");
                    break;
            }
        }

        // Ending의 종료 의도는 그대로 끝나거나, 정리 실패로 Faulted(CleanupFault)가 될 때만 달라진다.
        private static bool KeepsEndIntent(TutorialEndReason intent, TutorialSessionSnapshot next)
        {
            if (next.Status == TutorialSessionStatus.Faulted && next.EndReason == TutorialEndReason.CleanupFault)
                return true;
            if (next.EndReason != intent)
                return false;

            switch (next.Status)
            {
                case TutorialSessionStatus.Ending:
                    return true;
                case TutorialSessionStatus.Completed:
                    return intent == TutorialEndReason.Finished;
                case TutorialSessionStatus.Cancelled:
                    return TutorialSessionSnapshot.IsCancelReason(intent);
                case TutorialSessionStatus.Faulted:
                    return TutorialSessionSnapshot.IsFaultReason(intent);
                default:
                    return false;
            }
        }

        private void Apply(TutorialSessionSnapshot session)
        {
            _sessions[session.SessionId] = session;
            int active = session.IsActive
                ? session.SessionId
                : _activeSessionId == session.SessionId ? 0 : _activeSessionId;
            if (active != _activeSessionId)
            {
                _activeSessionId = active;
                _context = null;
            }
        }

        /// <summary>정의에서 미리 계산한 단계 하나의 규칙입니다.</summary>
        private readonly struct StepEntry
        {
            public TutorialStepKind Kind { get; }
            public TutorialCombatInputRestriction Restriction { get; }

            /// <summary>바로 앞 단계가 제한 실습일 때 이 설명이 시작 조건을 기다리는 동안 잇는 제한입니다.</summary>
            public TutorialCombatInputRestriction BridgeRestriction { get; }

            public StepEntry(TutorialStepKind kind, TutorialCombatInputRestriction restriction,
                TutorialCombatInputRestriction bridgeRestriction)
            {
                Kind = kind;
                Restriction = restriction;
                BridgeRestriction = bridgeRestriction;
            }
        }
    }

    #endregion
}
