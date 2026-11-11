namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Plans
{
    /// <summary>TutorialPlan이 고른 다음 한 걸음의 종류입니다(튜토리얼 설계 §9.1 Job 종류).</summary>
    internal enum TutorialPlanStepKind
    {
        /// <summary>지금은 할 일이 없습니다. Job을 만들지 않습니다.</summary>
        None = 0,
        /// <summary>미처리 요청 하나를 처리합니다.</summary>
        Request = 1,
        /// <summary>세션을 시작한 전투에 승패가 기록돼 종료 정리를 시작합니다.</summary>
        End = 2,
        /// <summary>정리가 끝난 세션의 최종 상태를 게시합니다.</summary>
        Finalize = 3,
        /// <summary>설명 구간의 첫 설명을 시작합니다(입력을 빌리고 표시).</summary>
        StartExplanation = 4,
        /// <summary>표시·전환이 끝난 단계를 다음 대기로 옮기거나 다음 단계에 들어갑니다. Starting의 첫 단계 진입도 이것입니다.</summary>
        Advance = 5,
        // 6은 제거한 시간 만료 전이의 번호이므로 재사용하지 않습니다.
        /// <summary>통과 조건을 채운 실습에서 다음 단계로 갑니다.</summary>
        PassPractice = 7
    }

    /// <summary>다음 한 걸음과 그 걸음이 다시 확인할 번호입니다. 요청은 요청 번호, 그 밖에는 세션 번호와 단계 revision입니다.</summary>
    internal readonly struct TutorialPlanStep
    {
        internal static readonly TutorialPlanStep Nothing = new(TutorialPlanStepKind.None, 0, 0);

        internal TutorialPlanStep(TutorialPlanStepKind kind, int number, int stepRevision)
        {
            Kind = kind;
            Number = number;
            StepRevision = stepRevision;
        }

        internal TutorialPlanStepKind Kind { get; }

        /// <summary>요청이면 요청 번호, 그 밖에는 세션 번호입니다.</summary>
        internal int Number { get; }

        /// <summary>판단할 때 본 단계 revision입니다. 요청·종료·최종 게시는 0입니다.</summary>
        internal int StepRevision { get; }

        public override string ToString() => Kind == TutorialPlanStepKind.None ? "None" : $"{Kind}({Number}, r{StepRevision})";
    }
}
