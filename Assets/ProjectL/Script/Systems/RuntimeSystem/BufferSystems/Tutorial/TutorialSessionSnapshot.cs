using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 세션 한 회차의 불변 조회 결과입니다(튜토리얼 설계 §8). 상태 Buffer가 원본이며, 바뀔 때마다 새 Snapshot으로 바꿔 둡니다.
    /// 이미 받은 Snapshot은 바뀌지 않습니다. <see cref="CanContinue"/>는 저장하지 않고 상태에서 계산합니다.
    /// </summary>
    public sealed class TutorialSessionSnapshot
    {
        /// <summary>받아들인 Start의 요청 번호입니다.</summary>
        public int SessionId { get; }

        /// <summary>세션을 시작한 화면 연결 번호입니다.</summary>
        public int ContextId { get; }

        /// <summary>세션이 실행하는 정의 키입니다.</summary>
        public int TutorialKey { get; }

        /// <summary>회차 상태입니다. Starting·Running·Ending이 활성이고 Completed·Cancelled·Faulted가 최종입니다.</summary>
        public TutorialSessionStatus Status { get; }

        /// <summary>현재 단계 키입니다. 첫 단계 이전은 0이며, 종료 중·최종 상태에서는 마지막 단계 키를 남깁니다.</summary>
        public int CurrentStepKey { get; }

        /// <summary>단계에 들어갈 때마다 1씩 늘어나는 입력 유효성 번호입니다. 첫 단계 이전은 0입니다.</summary>
        public int StepRevision { get; }

        /// <summary>현재 단계의 종류입니다. 첫 단계 이전과 종료 중·최종 상태에서는 None입니다.</summary>
        public TutorialStepKind StepKind { get; }

        /// <summary>지금 기다리는 것입니다(설계 §8 표). Job이 Runner를 점유한다는 뜻이 아닙니다.</summary>
        public TutorialWaitReason WaitReason { get; }

        /// <summary>Ending에서 고정한 종료 의도이거나 최종 상태의 이유입니다. 진행 중이면 None입니다.</summary>
        public TutorialEndReason EndReason { get; }

        /// <summary>표시된 설명이 사용자 확인을 받을 수 있는지입니다. 상태에서 계산합니다.</summary>
        public bool CanContinue => Status == TutorialSessionStatus.Running
            && StepKind == TutorialStepKind.Explanation
            && WaitReason == TutorialWaitReason.Continue;

        /// <summary>Starting·Running·Ending입니다. 한 런타임에 하나만 있습니다.</summary>
        public bool IsActive => Status == TutorialSessionStatus.Starting
            || Status == TutorialSessionStatus.Running
            || Status == TutorialSessionStatus.Ending;

        /// <summary>Completed·Cancelled·Faulted입니다. 한 번 확정되면 바뀌지 않습니다.</summary>
        public bool IsFinal => Status == TutorialSessionStatus.Completed
            || Status == TutorialSessionStatus.Cancelled
            || Status == TutorialSessionStatus.Faulted;

        /// <summary>Start를 받아들일 때 기록한 전투 번호입니다. 이 전투의 기록만 봅니다(설계 §8.2 전투 기준점).</summary>
        internal long BattleInstanceId { get; }

        /// <summary>Start를 받아들일 때의 최신 전투 결과 순번입니다. 그 뒤의 결과만 전투 진입 성공으로 봅니다.</summary>
        internal int StartResultSequence { get; }

        /// <summary>현재 실습에 들어갈 때의 최신 전투 결과 순번입니다. 그 뒤의 결과만 실습 통과로 봅니다.</summary>
        internal int PracticeResultSequence { get; }

        internal TutorialSessionSnapshot(int sessionId, int contextId, int tutorialKey, TutorialSessionStatus status,
            int currentStepKey, int stepRevision, TutorialStepKind stepKind, TutorialWaitReason waitReason,
            TutorialEndReason endReason, long battleInstanceId = 0, int startResultSequence = 0,
            int practiceResultSequence = 0)
        {
            RequirePositive(sessionId, nameof(sessionId));
            RequirePositive(contextId, nameof(contextId));
            RequirePositive(tutorialKey, nameof(tutorialKey));
            RequireDefined(status, nameof(status));
            RequireDefined(stepKind, nameof(stepKind));
            RequireDefined(waitReason, nameof(waitReason));
            RequireDefined(endReason, nameof(endReason));
            if (status == TutorialSessionStatus.None)
                throw new ArgumentOutOfRangeException(nameof(status), status, "세션 상태가 없습니다.");
            RequireNotNegative(currentStepKey, nameof(currentStepKey));
            RequireNotNegative(stepRevision, nameof(stepRevision));
            if (battleInstanceId < 0)
                throw new ArgumentOutOfRangeException(nameof(battleInstanceId), battleInstanceId, "0 이상이어야 합니다.");
            RequireNotNegative(startResultSequence, nameof(startResultSequence));
            RequireNotNegative(practiceResultSequence, nameof(practiceResultSequence));

            SessionId = sessionId;
            ContextId = contextId;
            TutorialKey = tutorialKey;
            Status = status;
            CurrentStepKey = currentStepKey;
            StepRevision = stepRevision;
            StepKind = stepKind;
            WaitReason = waitReason;
            EndReason = endReason;
            BattleInstanceId = battleInstanceId;
            StartResultSequence = startResultSequence;
            PracticeResultSequence = practiceResultSequence;

            RequireConsistent();
        }

        // 한 Snapshot 안의 값 사이 관계를 한곳에서 검사한다. 이전 상태와의 관계(전이)는 Buffer가 설치할 때 검사한다.
        private void RequireConsistent()
        {
            if (StepKind != TutorialStepKind.None && (CurrentStepKey == 0 || StepRevision == 0))
                throw new ArgumentException("단계 종류가 있으면 단계 키와 revision이 있어야 합니다.");

            switch (Status)
            {
                case TutorialSessionStatus.Starting:
                    if (CurrentStepKey != 0 || StepRevision != 0 || WaitReason != TutorialWaitReason.None)
                        throw new ArgumentException("Starting은 첫 단계에 들어가기 전이며 기다리는 것이 없습니다.");
                    break;
                case TutorialSessionStatus.Running:
                    RequireRunningWait();
                    break;
                case TutorialSessionStatus.Ending:
                    if (StepKind != TutorialStepKind.None)
                        throw new ArgumentException("Ending은 단계를 떠난 종료 정리 중이라 단계 종류가 없습니다.");
                    if (WaitReason != TutorialWaitReason.None && WaitReason != TutorialWaitReason.Cleanup)
                        throw new ArgumentException("Ending은 정리만 기다립니다.");
                    break;
                default:
                    if (StepKind != TutorialStepKind.None || WaitReason != TutorialWaitReason.None)
                        throw new ArgumentException("최종 상태는 단계 종류와 기다리는 것이 없습니다.");
                    break;
            }

            RequireEndReason();
        }

        private void RequireRunningWait()
        {
            if (StepKind == TutorialStepKind.None)
                throw new ArgumentException("Running은 단계 안에 있어야 합니다.");

            switch (WaitReason)
            {
                case TutorialWaitReason.StartCondition:
                case TutorialWaitReason.Continue:
                    if (StepKind != TutorialStepKind.Explanation)
                        throw new ArgumentException($"{WaitReason}는 설명 단계만 기다립니다.");
                    break;
                case TutorialWaitReason.PracticeGoal:
                    if (StepKind != TutorialStepKind.Practice)
                        throw new ArgumentException("PracticeGoal은 실습 단계만 기다립니다.");
                    break;
                case TutorialWaitReason.Cleanup:
                    throw new ArgumentException("Cleanup은 Ending만 기다립니다.");
            }
        }

        private void RequireEndReason()
        {
            bool hasEnd = EndReason != TutorialEndReason.None;
            if ((Status == TutorialSessionStatus.Starting || Status == TutorialSessionStatus.Running) && hasEnd)
                throw new ArgumentException("진행 중인 세션에는 종료 이유가 없습니다.");
            if ((Status == TutorialSessionStatus.Ending || IsFinal) && !hasEnd)
                throw new ArgumentException("종료 중이거나 끝난 세션에는 종료 이유가 있어야 합니다.");
            if (Status == TutorialSessionStatus.Completed && EndReason != TutorialEndReason.Finished)
                throw new ArgumentException("Completed의 종료 이유는 Finished입니다.");
            if (Status == TutorialSessionStatus.Cancelled && !IsCancelReason(EndReason))
                throw new ArgumentException("Cancelled의 종료 이유는 취소·화면 해제·전투 승패·런타임 정지입니다.");
            if (Status == TutorialSessionStatus.Faulted && !IsFaultReason(EndReason))
                throw new ArgumentException("Faulted의 종료 이유는 실행 또는 정리 오류입니다.");
        }

        /// <summary>종료 이유가 취소 종류(Cancelled로 끝남)인지입니다.</summary>
        internal static bool IsCancelReason(TutorialEndReason reason)
        {
            switch (reason)
            {
                case TutorialEndReason.ExternalCancelled:
                case TutorialEndReason.ContextDetached:
                case TutorialEndReason.BattleEnded:
                case TutorialEndReason.RuntimeStopped:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>종료 이유가 오류 종류(Faulted로 끝남)인지입니다.</summary>
        internal static bool IsFaultReason(TutorialEndReason reason)
        {
            return reason == TutorialEndReason.ExecutionFault || reason == TutorialEndReason.CleanupFault;
        }

        private static void RequirePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name, value, "0보다 커야 합니다.");
        }

        private static void RequireNotNegative(int value, string name)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(name, value, "0 이상이어야 합니다.");
        }

        private static void RequireDefined<T>(T value, string name) where T : struct, System.Enum
        {
            if (!System.Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(name, value, "알 수 없는 값입니다.");
        }
    }
}
