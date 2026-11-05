using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>
    /// 튜토리얼의 전투 접점입니다(View 2라운드 W6-4): 튜토리얼이 건 전투 입력 제한을 읽고, Router가 실제로 막은 입력 시도를 알립니다.
    /// 튜토리얼 상태 읽기·요청 쓰기 Sink 둘은 이 클래스만 갖습니다. Router는 지금처럼 알림 대리자 하나(<see cref="Report"/>)를 받습니다.
    /// </summary>
    /// <remarks>두 Sink 모두 필수입니다(View 2라운드 W6-7). 시험 조립은 실제 Buffer 창구를 넘기며, 기본 제한은 없음입니다.</remarks>
    internal sealed class CombatBlockedInputReport
    {
        private readonly ITutorialStateBufferReadSink _tutorialState;
        private readonly ITutorialRequestBufferWriteSink _tutorialRequests;

        internal CombatBlockedInputReport(ITutorialStateBufferReadSink tutorialState, ITutorialRequestBufferWriteSink tutorialRequests)
        {
            _tutorialState = tutorialState ?? throw new ArgumentNullException(nameof(tutorialState));
            _tutorialRequests = tutorialRequests ?? throw new ArgumentNullException(nameof(tutorialRequests));
        }

        /// <summary>튜토리얼이 건 전투 입력 제한입니다.</summary>
        internal TutorialCombatInputRestriction Restriction => _tutorialState.CombatInputRestriction;

        /// <summary>현재 전투의 실습 목표 대기 사실입니다. 실습 통과 뒤 다음 설명의 시작 대기는 포함하지 않습니다.</summary>
        internal bool IsPracticeGoalPending(long battleInstanceId)
        {
            int sessionId = _tutorialState.Context.ActiveSessionId;
            return sessionId > 0 && _tutorialState.TryGetSession(sessionId, out TutorialSessionSnapshot session)
                && session.BattleInstanceId == battleInstanceId
                && session.Status == TutorialSessionStatus.Running
                && session.StepKind == TutorialStepKind.Practice
                && session.WaitReason == TutorialWaitReason.PracticeGoal;
        }

        /// <summary>Router가 막은 실제 시도 하나만 현재 세션·revision으로 알립니다. 입력 안에서는 Job을 등록하지 않습니다.</summary>
        internal void Report()
        {
            int sessionId = _tutorialState.Context.ActiveSessionId;
            if (sessionId <= 0 || !_tutorialState.TryGetSession(sessionId, out TutorialSessionSnapshot session)
                || !session.IsActive || session.StepRevision <= 0)
                return;

            _tutorialRequests.TrySubmit(TutorialCommandData.ReportBlockedInput(sessionId, session.StepRevision), out _, out _);
        }
    }
}
