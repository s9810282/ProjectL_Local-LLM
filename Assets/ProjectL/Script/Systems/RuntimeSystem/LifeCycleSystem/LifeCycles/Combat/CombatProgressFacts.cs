using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 전투 진행의 다음 단계를 정할 때 쓰는 사실 묶음입니다(재구성 설계 J13).
    /// <see cref="CombatProgressReader"/>가 호출할 때마다 Buffer·View에서 새로 읽어 만들며, 저장하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 판단 시점의 일시적 사본입니다. 원본은 Core Buffer(국면·번호·Completion·결과·Run 결과), CombatViewBuffer(명령·미리보기·P),
    /// View(재생·제출·완료 표시·포기 요청)입니다. 판단 테스트는 이 묶음을 직접 만들어 넣습니다.
    /// </remarks>
    public readonly struct CombatProgressFacts
    {
        internal CombatProgressFacts(
            bool hasBattle,
            long battleInstanceId = 0,
            CombatFlowPhase phase = CombatFlowPhase.None,
            int handledCommandNumber = 0,
            int answeredPreviewNumber = 0,
            int latestResultSequence = 0,
            BattleCompletionChanges completion = null,
            CombatResult latestResult = null,
            int nextCommandNumber = 0,
            int previewNumber = 0,
            int presentedResultSequence = 0,
            bool isViewAttached = false,
            bool isSubmissionSuspended = false,
            CombatCompletionDisplay completionDisplay = CombatCompletionDisplay.None,
            bool isAbandonRequested = false,
            RunOutcome runOutcome = RunOutcome.InProgress,
            bool isEntryPresentationPending = false)
        {
            HasBattle = hasBattle;
            BattleInstanceId = battleInstanceId;
            Phase = phase;
            HandledCommandNumber = handledCommandNumber;
            AnsweredPreviewNumber = answeredPreviewNumber;
            LatestResultSequence = latestResultSequence;
            Completion = completion;
            LatestResult = latestResult;
            NextCommandNumber = nextCommandNumber;
            PreviewNumber = previewNumber;
            PresentedResultSequence = presentedResultSequence;
            IsViewAttached = isViewAttached;
            IsSubmissionSuspended = isSubmissionSuspended;
            CompletionDisplay = completionDisplay;
            IsAbandonRequested = isAbandonRequested;
            RunOutcome = runOutcome;
            IsEntryPresentationPending = isEntryPresentationPending;
        }

        public bool HasBattle { get; }

        public long BattleInstanceId { get; }

        /// <summary>Core의 처리 국면입니다.</summary>
        public CombatFlowPhase Phase { get; }

        /// <summary>Core가 마지막으로 처리한 명령 번호 H입니다.</summary>
        public int HandledCommandNumber { get; }

        /// <summary>Core가 마지막으로 응답한 미리보기 요청 번호입니다.</summary>
        public int AnsweredPreviewNumber { get; }

        /// <summary>Core의 결과 발행 순번 S입니다.</summary>
        public int LatestResultSequence { get; }

        /// <summary>Core가 기록한 전투 완료입니다. 없으면 null입니다.</summary>
        public BattleCompletionChanges Completion { get; }

        /// <summary>최신 처리 결과입니다. 없으면 null입니다. 거절된 결과도 결과입니다.</summary>
        public CombatResult LatestResult { get; }

        /// <summary>H보다 번호가 큰 첫 실행 명령의 번호 N입니다. 없으면 0입니다.</summary>
        public int NextCommandNumber { get; }

        /// <summary>현재 미리보기 요청 번호입니다. 요청이 없으면 0입니다.</summary>
        public int PreviewNumber { get; }

        /// <summary>View가 연출을 끝낸 결과의 마지막 순번 P입니다.</summary>
        public int PresentedResultSequence { get; }

        public bool IsViewAttached { get; }

        /// <summary>View 진입 연출의 대기입니다. Core 결과 재생(P &lt; S)과 독립적입니다.</summary>
        public bool IsEntryPresentationPending { get; }

        public bool IsSubmissionSuspended { get; }

        public CombatCompletionDisplay CompletionDisplay { get; }

        public bool IsAbandonRequested { get; }

        /// <summary>Core Run 진행의 결과입니다. Run이 이어지면 <c>InProgress</c>입니다(떠날 목적지를 정함).</summary>
        public RunOutcome RunOutcome { get; }

        public bool HasCompletion => Completion != null;

        public bool HasNextCommand => NextCommandNumber > 0;

        /// <summary>확인하지 않은 결과가 남았는지(P &lt; S)입니다.</summary>
        public bool HasUnpresentedResult => IsUnpresented(PresentedResultSequence, LatestResultSequence);

        /// <summary>
        /// 확인하지 않은 결과가 남았는지(P &lt; S)의 규칙 원본입니다. 골격(이 사실 묶음·Plan 튜토리얼 판정)과 전투 View가 이것을 부릅니다(C 단계 설계 C-3).
        /// 정리(EndBattle) 뒤에도 Core가 결과 순번을 남기므로 전투가 없을 때도 같은 뜻입니다.
        /// </summary>
        internal static bool IsUnpresented(int presentedResultSequence, int latestResultSequence) =>
            presentedResultSequence < latestResultSequence;

        /// <summary>
        /// 연출이 남았는지입니다(재구성 설계 J2의 "재생 중"). 재생은 확인하지 않은 결과가 있을 때만 하므로 P &lt; S와 같습니다(개선 설계 §7.1 D4).
        /// </summary>
        public bool IsPresenting => HasUnpresentedResult;
    }
}
