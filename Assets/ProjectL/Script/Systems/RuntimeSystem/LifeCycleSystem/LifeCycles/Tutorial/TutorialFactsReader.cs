using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
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
            bool presentedEnemyAttack = false;
            bool presentedPlayerTurnEnd = false;
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
                        // 종료 뒤에도 결과 이력은 남지만 대상 기록은 정리됩니다. 조회보다 수명 검사를 먼저 합니다.
                        if (sameBattle && hasBattle && combat.Current != null && combat.Completion == null
                            && sources.Progress.IsAttached && session.WaitReason == TutorialWaitReason.PracticeGoal)
                            ReadPracticeResults(sources.CombatResults.Results, session, goal, presented,
                                sources.Characters, combat.Current.Structures,
                                out enteredCombat, out presentedMove, out presentedEnemyAttack, out presentedPlayerTurnEnd);
                    }

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
                sources.Progress.IsAttached,
                sources.Progress.IsEntryPresentationPending,
                sources.Progress.IsSubmissionSuspended,
                sources.Progress.IsAbandonRequested,
                completedPresentation,
                hasCleanupResult,
                presentedEnemyAttack,
                presentedPlayerTurnEnd);
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
            TutorialPracticeGoal goal, int presentedSequence, ICharacterBufferReadSink characters,
            IReadOnlyList<StructureCombatFacts> structures, out bool enteredCombat, out bool presentedMove,
            out bool presentedEnemyAttack, out bool presentedPlayerTurnEnd)
        {
            enteredCombat = false;
            presentedMove = false;
            presentedEnemyAttack = false;
            presentedPlayerTurnEnd = false;
            int baseline = goal == TutorialPracticeGoal.EnterCombat
                ? session.StartResultSequence
                : session.PracticeResultSequence;

            for (int i = results.Count - 1; i >= 0; i--)
            {
                CombatResult result = results[i];
                if (result.Sequence <= baseline)
                    break;
                if (result.BattleInstanceId != session.BattleInstanceId
                    || result.Kind != CombatProcessingKind.Command || !result.Accepted)
                    continue;

                if (goal == TutorialPracticeGoal.EnterCombat && result.CommandKind == CombatCommandKind.EnterCombat)
                    enteredCombat = true;
                if (result.Sequence > presentedSequence)
                    continue;

                // 일반 Move 명령은 Core가 플레이어 소유/진영을 검증합니다. 자동 적 이동·턴 종료·재진입만으로 통과시키지 않습니다.
                // 선택한 아군의 수락된 이동이 한 칸 이상 진행되고 그 결과의 표시까지 끝나야 통과 사실입니다.
                if (goal == TutorialPracticeGoal.MoveAtLeastOneCell && result.CommandKind == CombatCommandKind.Move
                    && result.PathMovement != null && result.PathMovement.EnteredCells.Count >= 1)
                    presentedMove = true;
                if (goal == TutorialPracticeGoal.AttackEnemyOnce && result.CommandKind == CombatCommandKind.Attack
                    && result.Attack.HasValue && result.Attack.Value.Succeeded)
                    presentedEnemyAttack |= HasEnemyHit(result, characters, structures);
                if (goal == TutorialPracticeGoal.EndPlayerTurnOnce && result.CommandKind == CombatCommandKind.EndPlayerTurn)
                    presentedPlayerTurnEnd |= HasPlayerTurnEnded(result.Events);
            }
        }

        private static bool HasEnemyHit(CombatResult result, ICharacterBufferReadSink characters,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            bool hasEnemyHit = false;
            ReadOnlySpan<AttackHitResult> hits = result.Attack.Value.Hits.Span;
            for (int i = 0; i < hits.Length; i++)
            {
                // 한 대상이 인정돼도 나머지 ID의 계약 오류를 숨기지 않습니다. 피해량·현재 생존/배치는 조건이 아닙니다.
                hasEnemyHit |= IsEnemyTarget(hits[i].TargetId, result.Sequence, characters, structures);
            }
            return hasEnemyHit;
        }

        private static bool IsEnemyTarget(EntityId targetId, int resultSequence, ICharacterBufferReadSink characters,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            if (characters.TryGet(targetId, out CharacterData character))
                return character.Owner == Owner.Enemy;

            for (int i = 0; i < structures.Count; i++)
            {
                StructureCombatFacts structure = structures[i];
                if (structure.EntityId == targetId)
                    return structure.Owner == Owner.Enemy && structure.Definition.Key == EntityKey.Structure_Headquarters;
            }

            throw new InvalidOperationException(
                $"[TutorialFactsReader] 활성 전투의 공격 결과 {resultSequence}에서 피격 대상 {targetId}의 공개 기록을 찾을 수 없습니다.");
        }

        private static bool HasPlayerTurnEnded(IReadOnlyList<CombatEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is CombatTurnEvent turn && turn.Kind == CombatEventKind.PlayerTurnEnded)
                    return true;
            }
            return false;
        }
    }
}
