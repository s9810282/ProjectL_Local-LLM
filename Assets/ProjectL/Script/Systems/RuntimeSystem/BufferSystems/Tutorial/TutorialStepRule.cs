using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 정의 단계 하나의 진행 규칙입니다(튜토리얼 설계 §5·§10). 상태 Buffer가 단계 검사와 입력 제한 계산에,
    /// TutorialPlan이 단계 순서와 실습 통과 판정에 씁니다. 원본은 정의 데이터이며, 실행 소유자가 초기화 때 한 번 넘깁니다.
    /// 문구·표시 값은 담지 않습니다. 단계 종류·제한·통과 조건 사이의 규칙은 여기 한 곳에서 검사합니다.
    /// </summary>
    internal sealed class TutorialStepRule
    {
        public int StepKey { get; }
        public TutorialStepKind Kind { get; }

        /// <summary>이 단계가 전투 입력에 거는 제한입니다. 실습 단계만 가질 수 있습니다.</summary>
        public TutorialCombatInputRestriction Restriction { get; }

        /// <summary>실습 단계를 통과시키는 게임 결과의 종류입니다. 실습 단계는 반드시 가지며 설명 단계는 None입니다.</summary>
        public TutorialPracticeGoal PracticeGoal { get; }

        public TutorialStepRule(int stepKey, TutorialStepKind kind,
            TutorialCombatInputRestriction restriction = TutorialCombatInputRestriction.None,
            TutorialPracticeGoal practiceGoal = TutorialPracticeGoal.None)
        {
            if (stepKey <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepKey), stepKey, "0보다 커야 합니다.");
            if (kind == TutorialStepKind.None || !System.Enum.IsDefined(typeof(TutorialStepKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "단계 종류가 없습니다.");
            if (!System.Enum.IsDefined(typeof(TutorialCombatInputRestriction), restriction))
                throw new ArgumentOutOfRangeException(nameof(restriction), restriction, "알 수 없는 제한입니다.");
            if (!System.Enum.IsDefined(typeof(TutorialPracticeGoal), practiceGoal))
                throw new ArgumentOutOfRangeException(nameof(practiceGoal), practiceGoal, "알 수 없는 통과 조건입니다.");
            if (restriction != TutorialCombatInputRestriction.None && kind != TutorialStepKind.Practice)
                throw new ArgumentException("입력 제한은 실습 단계만 가질 수 있습니다.", nameof(restriction));
            if (kind == TutorialStepKind.Practice && practiceGoal == TutorialPracticeGoal.None)
                throw new ArgumentException("실습 단계에는 통과 조건이 있어야 합니다.", nameof(practiceGoal));
            if (kind != TutorialStepKind.Practice && practiceGoal != TutorialPracticeGoal.None)
                throw new ArgumentException("통과 조건은 실습 단계만 가질 수 있습니다.", nameof(practiceGoal));
            if (kind == TutorialStepKind.Practice && !CanPass(practiceGoal, restriction))
                throw new ArgumentException(
                    $"통과 조건 {practiceGoal}과 입력 제한 {restriction}은 함께 쓸 수 없습니다.", nameof(restriction));

            StepKey = stepKey;
            Kind = kind;
            Restriction = restriction;
            PracticeGoal = practiceGoal;
        }

        /// <summary>
        /// 그 제한 아래에서 통과 조작을 할 수 있는지입니다. 제한이 통과 조작까지 막으면 실습을 끝낼 수 없으므로,
        /// “이동만 허용”은 이동으로 통과하는 실습에만 둡니다(설계 §15.10).
        /// </summary>
        private static bool CanPass(TutorialPracticeGoal goal, TutorialCombatInputRestriction restriction)
        {
            return goal switch
            {
                TutorialPracticeGoal.EnterCombat => restriction == TutorialCombatInputRestriction.None,
                TutorialPracticeGoal.MoveAtLeastOneCell => restriction == TutorialCombatInputRestriction.None
                    || restriction == TutorialCombatInputRestriction.MoveOnly,
                _ => false
            };
        }
    }

    /// <summary>정의 하나의 단계 순서입니다. 순서는 목록 순서이며 단계 키는 겹치지 않습니다.</summary>
    internal sealed class TutorialDefinitionRules
    {
        public int TutorialKey { get; }
        public IReadOnlyList<TutorialStepRule> Steps { get; }

        public TutorialDefinitionRules(int tutorialKey, IReadOnlyList<TutorialStepRule> steps)
        {
            if (tutorialKey <= 0)
                throw new ArgumentOutOfRangeException(nameof(tutorialKey), tutorialKey, "0보다 커야 합니다.");
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (steps.Count == 0)
                throw new ArgumentException("단계가 하나 이상 있어야 합니다.", nameof(steps));

            var copied = new List<TutorialStepRule>(steps.Count);
            var keys = new HashSet<int>();
            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStepRule step = steps[i] ?? throw new ArgumentException($"단계 {i}번이 비어 있습니다.", nameof(steps));
                if (!keys.Add(step.StepKey))
                    throw new ArgumentException($"단계 키 {step.StepKey}가 중복됩니다.", nameof(steps));
                copied.Add(step);
            }

            TutorialKey = tutorialKey;
            Steps = new ReadOnlyCollection<TutorialStepRule>(copied);
        }

        /// <summary>단계 규칙입니다. 정의에 없는 단계면 상태와 정의가 어긋난 것이므로 예외입니다.</summary>
        public TutorialStepRule Find(int stepKey) => Steps[IndexOf(stepKey)];

        /// <summary>목록에서 다음 단계입니다. 마지막 단계면 null입니다. 첫 단계 이전(단계 키 0)이면 첫 단계입니다.</summary>
        public TutorialStepRule NextAfter(int stepKey)
        {
            int next = stepKey == 0 ? 0 : IndexOf(stepKey) + 1;
            return next < Steps.Count ? Steps[next] : null;
        }

        /// <summary>그 단계가 설명 구간의 첫 설명인지입니다(<see cref="StartsExplanationSegment(TutorialStepKind, TutorialStepKind)"/>).</summary>
        public bool StartsExplanationSegment(int stepKey)
        {
            int index = IndexOf(stepKey);
            TutorialStepKind previous = index == 0 ? TutorialStepKind.None : Steps[index - 1].Kind;
            return StartsExplanationSegment(previous, Steps[index].Kind);
        }

        /// <summary>
        /// 이 단계가 설명 구간의 첫 설명인지입니다. 설명 구간은 이어진 설명 단계 묶음이며 따로 저장하지 않습니다(설계 §8.1·§15.11).
        /// 상태 Buffer의 제한 잇기와 TutorialPlan의 시작 조건 대기가 이 규칙 하나를 같이 씁니다.
        /// </summary>
        /// <param name="previousKind">목록에서 바로 앞 단계의 종류입니다. 첫 단계면 <see cref="TutorialStepKind.None"/>입니다.</param>
        internal static bool StartsExplanationSegment(TutorialStepKind previousKind, TutorialStepKind kind)
        {
            return kind == TutorialStepKind.Explanation && previousKind != TutorialStepKind.Explanation;
        }

        private int IndexOf(int stepKey)
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].StepKey == stepKey)
                    return i;
            }

            throw new InvalidOperationException($"정의 {TutorialKey}에 단계 {stepKey}가 없습니다.");
        }
    }
}
