using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial
{
    /// <summary>
    /// TutorialPlan이 다음 한 걸음을 정할 때 쓰는 사실 묶음입니다(튜토리얼 설계 §9.1).
    /// <see cref="TutorialFactsReader"/>가 실행할 때마다 새로 읽어 만들며 저장하지 않습니다. 판단 시험은 이 묶음을 직접 만들어 넣습니다.
    /// </summary>
    /// <remarks>
    /// 원본은 튜토리얼 상태 Store(세션·요청), 전투 Buffer(국면·번호·승패·결과), 실행 소유자(만료·표시·정리·입력 독점),
    /// 상태 Store의 단계 규칙(단계 종류·통과 조건)입니다. 판단은 <see cref="Plans.TutorialPlan.Decide"/> 한 곳에서만 합니다.
    /// </remarks>
    internal readonly struct TutorialFacts
    {
        internal TutorialFacts(
            TutorialSessionSnapshot activeSession = null,
            int nextRequestNumber = 0,
            TutorialPracticeGoal practiceGoal = TutorialPracticeGoal.None,
            bool hasBattle = false,
            long battleInstanceId = 0,
            CombatFlowPhase phase = CombatFlowPhase.None,
            int latestResultSequence = 0,
            int presentedResultSequence = 0,
            bool hasUnhandledCommand = false,
            bool hasCompletion = false,
            bool hasEnteredCombat = false,
            bool hasPresentedMove = false,
            bool isInputMonopolized = false,
            bool isTimerExpired = false,
            TutorialViewPresentationKind completedPresentation = TutorialViewPresentationKind.None,
            bool hasCleanupResult = false)
        {
            ActiveSession = activeSession;
            NextRequestNumber = nextRequestNumber;
            PracticeGoal = practiceGoal;
            HasBattle = hasBattle;
            BattleInstanceId = battleInstanceId;
            Phase = phase;
            LatestResultSequence = latestResultSequence;
            PresentedResultSequence = presentedResultSequence;
            HasUnhandledCommand = hasUnhandledCommand;
            HasCompletion = hasCompletion;
            HasEnteredCombat = hasEnteredCombat;
            HasPresentedMove = hasPresentedMove;
            IsInputMonopolized = isInputMonopolized;
            IsTimerExpired = isTimerExpired;
            CompletedPresentation = completedPresentation;
            HasCleanupResult = hasCleanupResult;
        }

        /// <summary>Starting·Running·Ending 세션입니다. 없으면 null입니다.</summary>
        internal TutorialSessionSnapshot ActiveSession { get; }

        /// <summary>결과가 아직 없는 첫 요청 번호입니다. 없으면 0입니다.</summary>
        internal int NextRequestNumber { get; }

        /// <summary>활성 세션의 현재 단계가 실습이면 그 통과 조건입니다(상태 Store의 단계 규칙). 아니면 None입니다.</summary>
        internal TutorialPracticeGoal PracticeGoal { get; }

        internal bool HasBattle { get; }

        internal long BattleInstanceId { get; }

        internal CombatFlowPhase Phase { get; }

        /// <summary>Core의 결과 발행 순번 S입니다.</summary>
        internal int LatestResultSequence { get; }

        /// <summary>View가 연출을 끝낸 결과의 마지막 순번 P입니다.</summary>
        internal int PresentedResultSequence { get; }

        /// <summary>Core가 아직 처리하지 않은 전투 명령이 있는지입니다.</summary>
        internal bool HasUnhandledCommand { get; }

        /// <summary>지금 전투에 승패가 기록됐는지입니다. 세션의 전투와 같은지는 판단이 봅니다.</summary>
        internal bool HasCompletion { get; }

        /// <summary>세션 기준점 뒤 같은 전투에서 전투 진입이 받아들여졌는지입니다(설계 §15.5).</summary>
        internal bool HasEnteredCombat { get; }

        /// <summary>실습 기준점 뒤 같은 전투에서 한 칸 이상 실제 이동이 받아들여지고 그 연출이 끝났는지입니다(설계 §15.5).</summary>
        internal bool HasPresentedMove { get; }

        internal bool IsInputMonopolized { get; }

        /// <summary>현재 설명의 자동 진행 시간이 다 됐는지입니다.</summary>
        internal bool IsTimerExpired { get; }

        /// <summary>현재 단계에 마지막으로 요청한 표시가 끝났으면 그 종류, 아니면 None입니다.</summary>
        internal TutorialViewPresentationKind CompletedPresentation { get; }

        /// <summary>활성 세션의 정리가 끝났는지입니다.</summary>
        internal bool HasCleanupResult { get; }

        internal bool HasActiveSession => ActiveSession != null;

        internal bool HasPendingRequest => NextRequestNumber > 0;

        /// <summary>
        /// 세션을 시작한 전투에 승패가 기록됐는지입니다. 기준점 이전·다른 전투의 기록은 보지 않습니다(설계 §8.2).
        /// 승패와 전투 번호는 전투 종료 뒤에도 다음 전투 설치 전까지 남으므로 전투 존재 여부는 보지 않습니다.
        /// </summary>
        internal bool IsSessionBattleEnded => HasActiveSession && HasCompletion && IsSessionBattle(ActiveSession, BattleInstanceId);

        /// <summary>지금 전투가 세션을 시작한 전투인지입니다(설계 §8.2 전투 기준점). 같은 전투 판정은 여기 한 곳입니다.</summary>
        internal static bool IsSessionBattle(TutorialSessionSnapshot session, long battleInstanceId)
        {
            return session != null && session.BattleInstanceId != 0 && session.BattleInstanceId == battleInstanceId;
        }
    }
}
