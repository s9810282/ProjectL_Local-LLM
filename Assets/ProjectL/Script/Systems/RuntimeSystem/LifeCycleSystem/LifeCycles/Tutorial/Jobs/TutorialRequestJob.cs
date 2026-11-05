using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Jobs
{
    /// <summary>
    /// 미처리 요청 하나를 처리하고 결과를 게시합니다(튜토리얼 설계 §6·§7). 검사 순서는 설계 §7의 고정 순서입니다.
    /// 실행할 때 다음 미처리 번호가 등록 때와 다르면 이미 처리된 것이므로 바꾸지 않고 끝냅니다.
    /// </summary>
    internal sealed class TutorialRequestJob : IJobService
    {
        private readonly TutorialPlanSources _sources;
        private readonly int _requestNumber;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal TutorialRequestJob(TutorialPlanSources sources, int requestNumber, int priority)
        {
            _sources = sources;
            if (requestNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestNumber), requestNumber, "0보다 커야 합니다.");
            _requestNumber = requestNumber;
            Priority = priority;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (_sources.Store.TryGetNextRequest(out TutorialRequest request) && request.Number == _requestNumber)
            {
                switch (request.Data.Kind)
                {
                    case TutorialCommandKind.Start:
                        HandleStart(request);
                        break;
                    case TutorialCommandKind.Continue:
                    case TutorialCommandKind.Cancel:
                    case TutorialCommandKind.ReportBlockedInput:
                        HandleSessionCommand(request);
                        break;
                    default:
                        throw new InvalidOperationException($"[TutorialRequestJob] 알 수 없는 명령입니다: {request.Data.Kind}");
                }
            }

            IsCompleted = true;
        }

        // 환경 사용 가능 → Context 일치 → 정의 존재 → 활성 세션 없음(설계 §7).
        private void HandleStart(TutorialRequest request)
        {
            TutorialContextSnapshot context = _sources.State.Context;
            TutorialReason reason = TutorialReason.None;
            if (!context.IsAvailable)
                reason = TutorialReason.Unavailable;
            else if (context.ContextId != request.Data.ExpectedContextId)
                reason = TutorialReason.ContextChanged;
            else if (!IsAvailableDefinition(request.Data.TutorialKey))
                reason = TutorialReason.UnknownTutorial;
            else if (context.ActiveSessionId != 0)
                reason = TutorialReason.Busy;

            if (reason != TutorialReason.None)
            {
                _sources.Store.Publish(TutorialRequestResult.Rejected(request.Number, TutorialCommandKind.Start, 0, reason));
                return;
            }

            bool hasBattle = _sources.CombatState.HasBattle;
            TutorialSessionSnapshot session = TutorialSessionChanges.Starting(request.Number, context.ContextId,
                request.Data.TutorialKey, hasBattle ? _sources.CombatState.BattleInstanceId : 0,
                _sources.CombatState.LatestResultSequence);
            _sources.Store.Publish(
                TutorialRequestResult.Accepted(request.Number, TutorialCommandKind.Start, request.Number), session);
        }

        // 세션 존재 → 종료/종료 중 → 명령별 검사(설계 §7).
        private void HandleSessionCommand(TutorialRequest request)
        {
            TutorialCommandData command = request.Data;
            int sessionId = command.SessionId;
            if (!_sources.State.TryGetSession(sessionId, out TutorialSessionSnapshot session))
            {
                Reject(request, TutorialReason.UnknownSession);
                return;
            }

            if (session.IsFinal)
            {
                Reject(request, TutorialReason.SessionEnded);
                return;
            }

            if (session.Status == TutorialSessionStatus.Ending)
            {
                Reject(request, TutorialReason.SessionEnding);
                return;
            }

            switch (command.Kind)
            {
                case TutorialCommandKind.Continue:
                    HandleContinue(request, session);
                    break;
                case TutorialCommandKind.Cancel:
                    _sources.Store.Publish(TutorialRequestResult.Accepted(request.Number, command.Kind, sessionId),
                        TutorialSessionChanges.Ending(session, TutorialEndReason.ExternalCancelled));
                    _sources.Execution.BeginCleanup(sessionId);
                    break;
                default:
                    HandleBlockedInput(request, session);
                    break;
            }
        }

        // revision 일치 → 확인 준비. 받아들이면 같은 단계에서 설명을 닫는 전환에 들어가 CanContinue가 false가 된다(설계 §6.1).
        private void HandleContinue(TutorialRequest request, TutorialSessionSnapshot session)
        {
            if (request.Data.ExpectedStepRevision != session.StepRevision)
            {
                Reject(request, TutorialReason.StepChanged);
                return;
            }

            if (!session.CanContinue)
            {
                Reject(request, TutorialReason.NotReady);
                return;
            }

            TutorialSessionSnapshot closing = TutorialSessionChanges.Wait(session, TutorialWaitReason.Presentation);
            _sources.Store.Publish(
                TutorialRequestResult.Accepted(request.Number, TutorialCommandKind.Continue, session.SessionId), closing);
            TutorialProgression.Present(_sources, closing, TutorialViewPresentationKind.Transition);
        }

        // revision 일치 → 현재 단계가 입력을 제한하는 실습. 통과 뒤 다음 설명을 기다리는 유지 구간은 NotReady다(설계 §15.10).
        private void HandleBlockedInput(TutorialRequest request, TutorialSessionSnapshot session)
        {
            if (request.Data.ExpectedStepRevision != session.StepRevision)
            {
                Reject(request, TutorialReason.StepChanged);
                return;
            }

            bool restrictedPractice = session.Status == TutorialSessionStatus.Running
                && session.StepKind == TutorialStepKind.Practice
                && _sources.State.CombatInputRestriction != TutorialCombatInputRestriction.None;
            if (!restrictedPractice)
            {
                Reject(request, TutorialReason.NotReady);
                return;
            }

            _sources.Store.Publish(TutorialRequestResult.Accepted(request.Number,
                TutorialCommandKind.ReportBlockedInput, session.SessionId));
            _sources.Execution.ShowBlockedInputNotice(session.SessionId, session.StepRevision, session.TutorialKey,
                session.CurrentStepKey);
        }

        private void Reject(TutorialRequest request, TutorialReason reason)
        {
            _sources.Store.Publish(TutorialRequestResult.Rejected(request.Number, request.Data.Kind,
                request.Data.SessionId, reason));
        }

        private bool IsAvailableDefinition(int tutorialKey)
        {
            foreach (TutorialDefinitionInfo definition in _sources.State.AvailableDefinitions)
            {
                if (definition.TutorialKey == tutorialKey)
                    return true;
            }

            return false;
        }
    }
}
