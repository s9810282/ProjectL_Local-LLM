using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item
{
    /// <summary>
    /// 튜토리얼 정의의 단계 하나입니다(튜토리얼 설계 §15.11). 단계 순서는 정의의 목록 순서입니다.
    /// 설명 구간은 이어진 설명 단계 묶음이며 따로 적지 않습니다(설계 §8.1).
    /// 종류에 맞지 않는 칸은 비워 둬야 합니다(<see cref="Validate"/>).
    /// </summary>
    [Serializable]
    internal sealed class TutorialStepData
    {
        [Tooltip("정의 안에서 겹치지 않는 1 이상의 번호입니다. 1-1 정의의 실습 단계는 12·18·22·24입니다.")]
        [SerializeField, Min(1)] private int stepKey = 1;
        [SerializeField] private TutorialStepKind kind = TutorialStepKind.Explanation;

        [Header("설명 단계")]
        [Tooltip("설명 상자에 보일 문구입니다. 원본은 기획서 슬라이드 본문입니다.")]
        [TextArea(2, 4)]
        [SerializeField] private string explanationText;
        [Tooltip("덮개에서 보이게 둘 화면 대상입니다. 설명의 기준 대상도 이 목록에 포함합니다.")]
        [SerializeField] private List<TutorialFocusTarget> focusTargets = new();
        [Tooltip("설명 박스를 옆에 둘 기준 대상입니다. 강조 목록에 포함돼야 하며, 강조 없는 설명은 None입니다.")]
        [SerializeField] private TutorialFocusTarget explanationAnchor;
        [Tooltip("기준 대상의 어느 쪽에 설명을 먼저 배치할지 정합니다. 화면 경계와 가림 검사가 우선합니다. 강조 없는 설명은 None으로 중앙에 배치합니다.")]
        [SerializeField] private TutorialViewExplanationPlacement explanationPlacement;

        [Header("실습 단계")]
        [SerializeField] private TutorialPracticeGoal practiceGoal;
        [Tooltip("이 실습 동안 전투 입력에 거는 제한입니다.")]
        [SerializeField] private TutorialCombatInputRestriction inputRestriction;
        [Tooltip("통과할 때까지 화면 상단 중앙에 보일 짧은 안내입니다.")]
        [SerializeField] private string practiceGuideText;
        [Tooltip("입력 제한 때문에 조작이 막혔을 때 잠시 보일 안내입니다. 제한이 있을 때만 씁니다.")]
        [SerializeField] private string blockedInputText;
        [Tooltip("배치 국면의 실습에서 노란 점선으로 표시할 UI입니다.")]
        [SerializeField] private List<TutorialFocusTarget> practiceDeploymentTargets = new();
        [Tooltip("전투 국면의 실습에서 노란 점선으로 표시할 UI입니다.")]
        [SerializeField] private List<TutorialFocusTarget> practiceCombatTargets = new();

        public int StepKey => stepKey;
        public TutorialStepKind Kind => kind;
        public string ExplanationText => explanationText ?? string.Empty;
        public IReadOnlyList<TutorialFocusTarget> FocusTargets => focusTargets ?? (IReadOnlyList<TutorialFocusTarget>)Array.Empty<TutorialFocusTarget>();
        public TutorialFocusTarget ExplanationAnchor => explanationAnchor;
        public TutorialViewExplanationPlacement ExplanationPlacement => explanationPlacement;
        public TutorialPracticeGoal PracticeGoal => practiceGoal;
        public TutorialCombatInputRestriction InputRestriction => inputRestriction;
        public string PracticeGuideText => practiceGuideText ?? string.Empty;
        public string BlockedInputText => blockedInputText ?? string.Empty;
        public IReadOnlyList<TutorialFocusTarget> PracticeDeploymentTargets => practiceDeploymentTargets
            ?? (IReadOnlyList<TutorialFocusTarget>)Array.Empty<TutorialFocusTarget>();
        public IReadOnlyList<TutorialFocusTarget> PracticeCombatTargets => practiceCombatTargets
            ?? (IReadOnlyList<TutorialFocusTarget>)Array.Empty<TutorialFocusTarget>();

        /// <summary>
        /// 상태 Buffer와 TutorialPlan이 쓰는 단계 규칙을 만듭니다. 단계 키·종류·제한·통과 조건 사이의 규칙은
        /// <see cref="TutorialStepRule"/> 한 곳에서 검사합니다.
        /// </summary>
        internal TutorialStepRule CreateRule() => new(stepKey, kind, inputRestriction, practiceGoal);

        /// <summary>종류별로 칸이 맞는지 검사합니다. 틀리면 <see cref="InvalidOperationException"/>입니다.</summary>
        internal void Validate(int tutorialKey)
        {
            string at = $"Tutorial {tutorialKey}, step {stepKey}";

            try
            {
                CreateRule();
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"[TutorialStepData] {at}: {exception.Message}", exception);
            }

            if (kind == TutorialStepKind.Explanation)
                ValidateExplanation(at);
            else
                ValidatePractice(at);
        }

        private void ValidateExplanation(string at)
        {
            if (PracticeDeploymentTargets.Count > 0 || PracticeCombatTargets.Count > 0)
                throw new InvalidOperationException($"[TutorialStepData] {at}: 설명 단계는 실습 강조 대상을 가질 수 없습니다.");
            if (string.IsNullOrWhiteSpace(explanationText))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 설명 문구가 비어 있습니다.");
            if (!string.IsNullOrEmpty(practiceGuideText) || !string.IsNullOrEmpty(blockedInputText))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 설명 단계는 실습 안내 문구를 가질 수 없습니다.");

            var seen = new HashSet<TutorialFocusTarget>();
            foreach (TutorialFocusTarget target in FocusTargets)
            {
                if (target == TutorialFocusTarget.None || !System.Enum.IsDefined(typeof(TutorialFocusTarget), target))
                    throw new InvalidOperationException($"[TutorialStepData] {at}: 알 수 없는 보일 대상입니다. Value: {target}");
                if (!seen.Add(target))
                    throw new InvalidOperationException($"[TutorialStepData] {at}: 보일 대상 {target}이 중복됩니다.");
            }
            if (seen.Count == 0)
            {
                if (explanationAnchor != TutorialFocusTarget.None || explanationPlacement != TutorialViewExplanationPlacement.None)
                    throw new InvalidOperationException($"[TutorialStepData] {at}: 강조 없는 설명은 기준 대상·배치 방향이 None이어야 합니다.");
                return;
            }
            if (!seen.Contains(explanationAnchor))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 설명 기준 대상은 보일 대상 목록에 포함돼야 합니다. Value: {explanationAnchor}");
            if (explanationPlacement == TutorialViewExplanationPlacement.None ||
                !System.Enum.IsDefined(typeof(TutorialViewExplanationPlacement), explanationPlacement))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 설명 배치 방향이 올바르지 않습니다. Value: {explanationPlacement}");
        }

        private void ValidatePractice(string at)
        {
            ValidatePracticeTargets(PracticeDeploymentTargets, at, nameof(practiceDeploymentTargets));
            ValidatePracticeTargets(PracticeCombatTargets, at, nameof(practiceCombatTargets));
            if (explanationAnchor != TutorialFocusTarget.None || explanationPlacement != TutorialViewExplanationPlacement.None)
                throw new InvalidOperationException($"[TutorialStepData] {at}: 실습 단계의 설명 기준 대상·배치 방향은 None이어야 합니다.");
            if (!string.IsNullOrEmpty(explanationText) || FocusTargets.Count > 0)
                throw new InvalidOperationException($"[TutorialStepData] {at}: 실습 단계는 설명 문구·보일 대상을 가질 수 없습니다.");
            if (string.IsNullOrWhiteSpace(practiceGuideText))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 실습 안내 문구가 비어 있습니다.");

            bool restricted = inputRestriction != TutorialCombatInputRestriction.None;
            if (restricted && string.IsNullOrWhiteSpace(blockedInputText))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 입력 제한이 있으면 막힌 조작 안내 문구가 필요합니다.");
            if (!restricted && !string.IsNullOrEmpty(blockedInputText))
                throw new InvalidOperationException($"[TutorialStepData] {at}: 입력 제한이 없으면 막힌 조작 안내 문구를 비워야 합니다.");
        }

        private static void ValidatePracticeTargets(IReadOnlyList<TutorialFocusTarget> targets, string at, string field)
        {
            var seen = new HashSet<TutorialFocusTarget>();
            foreach (TutorialFocusTarget target in targets)
            {
                bool isUi = target == TutorialFocusTarget.Hand || target == TutorialFocusTarget.MainCharacterCard
                    || target == TutorialFocusTarget.EnterCombatButton || target == TutorialFocusTarget.MainCharacterHealth
                    || target == TutorialFocusTarget.ActionCost || target == TutorialFocusTarget.AttackButton
                    || target == TutorialFocusTarget.EndTurnButton;
                if (!isUi || !seen.Add(target))
                    throw new InvalidOperationException($"[TutorialStepData] {at}: {field}에는 중복 없는 UI 대상만 필요합니다. Value: {target}");
            }
        }
    }
}
