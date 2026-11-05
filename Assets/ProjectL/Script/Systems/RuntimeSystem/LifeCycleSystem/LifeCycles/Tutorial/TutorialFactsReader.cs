using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial
{
    /// <summary>
    /// 사실 묶음을 한 번 읽어 만듭니다(튜토리얼 설계 §9.1). 판단하지 않고 Buffer·실행 소유자의 값을 옮기기만 합니다.
    /// 실습 통과 사실은 Core 규칙을 복제하지 않고 공개 전투 결과에서 읽습니다(설계 §15.5).
    /// </summary>
    internal static class TutorialFactsReader
    {
        internal static TutorialFacts Read(in TutorialPlanSources sources)
        {
            TutorialSessionSnapshot session = ReadActiveSession(sources.State);
            int nextRequest = sources.Store.TryGetNextRequest(out TutorialRequest request) ? request.Number : 0;

            ICombatStateBufferReadSink combat = sources.CombatState;
            bool hasBattle = combat.HasBattle;
            long battleInstanceId = combat.BattleInstanceId;
            int presented = sources.CombatInput.PresentedResultSequence;
            bool hasUnhandledCommand = sources.CombatInput.TryGetNextCommand(combat.HandledCommandNumber, out _);

            TutorialPracticeGoal goal = TutorialPracticeGoal.None;
            bool enteredCombat = false;
            bool presentedMove = false;
            bool timerExpired = false;
            bool hasCleanupResult = false;
            TutorialViewPresentationKind completedPresentation = TutorialViewPresentationKind.None;
            ITutorialViewExecution execution = sources.Execution;

            if (session != null)
            {
                bool sameBattle = TutorialFacts.IsSessionBattle(session, battleInstanceId);
                if (session.Status == TutorialSessionStatus.Running)
                {
                    if (session.StepKind == TutorialStepKind.Practice)
                    {
                        goal = sources.Store.GetRules(session.TutorialKey).Find(session.CurrentStepKey).PracticeGoal;
                        if (sameBattle && session.WaitReason == TutorialWaitReason.PracticeGoal)
                            ReadPracticeResults(sources.CombatResults.Results, session, goal, presented,
                                out enteredCombat, out presentedMove);
                    }

                    timerExpired = execution.IsTimerExpired(session.SessionId, session.StepRevision);
                    if (!execution.TryGetCompletedPresentation(session.SessionId, session.StepRevision,
                            out completedPresentation))
                        completedPresentation = TutorialViewPresentationKind.None;
                }
                else if (session.Status == TutorialSessionStatus.Ending)
                {
                    hasCleanupResult = execution.TryGetCleanupResult(session.SessionId, out _);
                }
            }

            return new TutorialFacts(
                session,
                nextRequest,
                goal,
                hasBattle,
                battleInstanceId,
                combat.Phase,
                combat.LatestResultSequence,
                presented,
                hasUnhandledCommand,
                combat.Completion != null,
                enteredCombat,
                presentedMove,
                execution.IsInputMonopolized,
                timerExpired,
                completedPresentation,
                hasCleanupResult);
        }

        /// <summary>활성 세션입니다. 없으면 null입니다. 활성 번호가 가리키는 세션이 없거나 끝났으면 상태 Store 불변식 위반이라 예외입니다.</summary>
        internal static TutorialSessionSnapshot ReadActiveSession(ITutorialStateBufferReadSink state)
        {
            int activeId = state.Context.ActiveSessionId;
            if (activeId == 0)
                return null;
            if (!state.TryGetSession(activeId, out TutorialSessionSnapshot session) || !session.IsActive)
                throw new InvalidOperationException($"[TutorialFactsReader] 활성 세션 {activeId}을 읽을 수 없습니다.");
            return session;
        }

        // 결과는 순번 오름차순이고 전투가 바뀌면 비워진다. 기준점 뒤의 결과만 뒤에서부터 본다.
        private static void ReadPracticeResults(IReadOnlyList<CombatResult> results, TutorialSessionSnapshot session,
            TutorialPracticeGoal goal, int presentedSequence, out bool enteredCombat, out bool presentedMove)
        {
            enteredCombat = false;
            presentedMove = false;
            int baseline = goal == TutorialPracticeGoal.EnterCombat
                ? session.StartResultSequence
                : session.PracticeResultSequence;

            for (int i = results.Count - 1; i >= 0; i--)
            {
                CombatResult result = results[i];
                if (result.Sequence <= baseline)
                    break;
                if (result.Kind != CombatProcessingKind.Command || !result.Accepted)
                    continue;

                if (result.CommandKind == CombatCommandKind.EnterCombat)
                    enteredCombat = true;
                // 일반 Move 명령은 Core가 플레이어 소유/진영을 검증합니다. 자동 적 이동·턴 종료·재진입만으로 통과시키지 않습니다.
                // 선택한 아군의 수락된 이동이 한 칸 이상 진행되고 그 결과의 표시까지 끝나야 통과 사실입니다.
                if (result.CommandKind == CombatCommandKind.Move && result.PathMovement != null
                    && result.PathMovement.EnteredCells.Count >= 1 && result.Sequence <= presentedSequence)
                    presentedMove = true;
            }
        }
    }
}
