using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item
{
    /// <summary>
    /// 튜토리얼 정의 하나입니다(튜토리얼 설계 §15.11). 진행 순서·문구·덮개·실습 조건의 원본이며 런타임 상태는 담지 않습니다.
    /// 키는 공개 정의 목록(<see cref="TutorialDefinitionInfo"/>)과 같은 튜토리얼 키입니다.
    /// </summary>
    [Serializable]
    internal sealed class TutorialDefinitionData : IData
    {
        [SerializeField, Min(1)] private int tutorialKey = 1;
        [SerializeField] private string displayName;
        [Tooltip("이 맵(장)에서 처음 들어가는 전투에 튜토리얼을 실행합니다(설계 §15.12). 1장 = 1.")]
        [SerializeField, Min(1)] private int targetMapKey = 1;
        [Tooltip("설명 중 보일 대상 밖을 덮는 색입니다. 검정색, 투명도 5%(불투명도 95%, 알파 0.95)가 기준입니다.")]
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.95f);
        [SerializeField] private List<TutorialStepData> steps = new();

        public int TutorialKey => tutorialKey;
        public string DisplayName => displayName ?? string.Empty;
        public int TargetMapKey => targetMapKey;
        public Color OverlayColor => overlayColor;
        public IReadOnlyList<TutorialStepData> Steps => steps ?? (IReadOnlyList<TutorialStepData>)Array.Empty<TutorialStepData>();

        /// <summary>요청할 수 있는 정의로 공개할 값을 만듭니다(설계 §5).</summary>
        internal TutorialDefinitionInfo CreateInfo() => new(tutorialKey, DisplayName);

        /// <summary>상태 Buffer에 넘길 단계 규칙을 만듭니다. 순서는 <see cref="Steps"/> 순서입니다.</summary>
        internal TutorialDefinitionRules CreateRules()
        {
            var rules = new List<TutorialStepRule>(Steps.Count);
            foreach (TutorialStepData step in Steps)
                rules.Add(step?.CreateRule());
            return new TutorialDefinitionRules(tutorialKey, rules);
        }

        /// <summary>정의 전체를 검사합니다. 틀리면 <see cref="InvalidOperationException"/>입니다.</summary>
        internal void Validate()
        {
            if (tutorialKey < 1)
                throw new InvalidOperationException($"[TutorialDefinitionData] Tutorial key must be positive. Key: {tutorialKey}");

            string at = $"Tutorial {tutorialKey}";
            if (string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException($"[TutorialDefinitionData] {at}: 이름이 비어 있습니다.");
            if (targetMapKey < 1)
                throw new InvalidOperationException($"[TutorialDefinitionData] {at}: 대상 맵 키는 1 이상이어야 합니다. Value: {targetMapKey}");
            if (!IsUnit(overlayColor.r) || !IsUnit(overlayColor.g) || !IsUnit(overlayColor.b) || !IsUnit(overlayColor.a))
                throw new InvalidOperationException($"[TutorialDefinitionData] {at}: 덮개 색의 각 값은 0~1이어야 합니다. Value: {overlayColor}");
            if (Steps.Count == 0)
                throw new InvalidOperationException($"[TutorialDefinitionData] {at}: 단계가 하나 이상 있어야 합니다.");

            for (int i = 0; i < Steps.Count; i++)
            {
                TutorialStepData step = Steps[i]
                    ?? throw new InvalidOperationException($"[TutorialDefinitionData] {at}: 단계 {i}번이 비어 있습니다.");
                step.Validate(tutorialKey);
            }

            try
            {
                CreateRules();
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"[TutorialDefinitionData] {at}: {exception.Message}", exception);
            }
        }

        private static bool IsUnit(float value) => value >= 0f && value <= 1f;
    }

    [CreateAssetMenu(fileName = "NewTutorialDefinitionData", menuName = "ProjectL/Database/View/TutorialDefinitionData")]
    internal sealed class TutorialDefinitionDataSO : DatabaseItem
    {
        [SerializeField] private TutorialDefinitionData tutorialDefinitionData = new();

        public override IData GetData() => tutorialDefinitionData;
    }
}
