using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    internal enum CombatActivationCueTiming
    {
        ActivationStart = 0,
        StateApplied = 1
    }

    internal enum CombatBodyTextSource
    {
        TraitDisplayName = 0,
        Override = 1
    }

    internal enum CombatBodyTextTarget
    {
        Source = 0,
        Targets = 1,
        All = 2
    }

    internal enum CombatBodyTextCount
    {
        None = 0,
        CompletedMovementCells = 1
    }

    /// <summary>발동 줄의 선택적 몸체 텍스트 입력입니다.</summary>
    [Serializable]
    internal sealed class CombatBodyTextInput
    {
        [SerializeField]
        private bool enabled;
        [SerializeField]
        private CombatBodyTextSource textSource;
        [SerializeField, TextArea(2, 4)]
        private string overrideText;
        [Tooltip("Source는 발동 유닛, Targets는 대상마다, All은 발동 유닛과 대상 전체에 표시합니다. 같은 유닛은 한 번만 표시합니다.")]
        [SerializeField]
        private CombatBodyTextTarget target;
        [SerializeField]
        private CombatBodyTextCount count;
        [SerializeField]
        private Color color = Color.white;
        [SerializeField, Min(.01f)]
        private float fontSize = 28f;
        [Tooltip("몸 중앙 기준 Canvas 좌표 보정입니다. +X는 오른쪽, +Y는 위쪽이며 CanvasScaler의 기준 해상도를 따릅니다.")]
        [SerializeField]
        private Vector2 positionOffset;
        [Tooltip("비우면 공통 Body Text Prefab을 사용합니다. 폰트·머티리얼·배경·장식은 프리팹/Variant에서 편집합니다.")]
        [SerializeField]
        private GameObject prefabOverride;
        [SerializeField, Min(0)]
        private float holdSeconds = .25f;
        [SerializeField, Min(0)]
        private float fadeSeconds = .6f;
        internal CombatBodyTextData ToData() => enabled ? new CombatBodyTextData(textSource, overrideText, target, count,
            color, fontSize, positionOffset, prefabOverride, holdSeconds, fadeSeconds) : null;
    }

    /// <summary>검증된 텍스트 표시 값입니다. 표시 상태는 보관하지 않습니다.</summary>
    internal sealed class CombatBodyTextData
    {
        internal CombatBodyTextData(CombatBodyTextSource source, string text, CombatBodyTextTarget target, CombatBodyTextCount count,
            Color color, float fontSize, Vector2 positionOffset, GameObject prefabOverride, float hold, float fade)
        {
            if (!Enum.IsDefined(typeof(CombatBodyTextSource), source) || !Enum.IsDefined(typeof(CombatBodyTextTarget), target) || !Enum.IsDefined(typeof(CombatBodyTextCount), count))
                throw new ArgumentException("몸체 텍스트 종류가 정의되지 않았습니다.");
            if (source == CombatBodyTextSource.Override && string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("덮어쓰기 텍스트가 비었습니다.", nameof(text));
            CombatPresentationValueRules.RequireFinite(color.r, nameof(color));
            CombatPresentationValueRules.RequireFinite(color.g, nameof(color));
            CombatPresentationValueRules.RequireFinite(color.b, nameof(color));
            CombatPresentationValueRules.RequireFinite(color.a, nameof(color));
            CombatPresentationValueRules.RequirePositive(fontSize, nameof(fontSize));
            CombatPresentationValueRules.RequireFinite(positionOffset.x, nameof(positionOffset));
            CombatPresentationValueRules.RequireFinite(positionOffset.y, nameof(positionOffset));
            if (!CombatPresentationValueRules.IsNonNegative(hold) || !CombatPresentationValueRules.IsNonNegative(fade) || !CombatPresentationValueRules.IsPositive(hold + fade))
                throw new ArgumentOutOfRangeException(nameof(hold), "몸체 텍스트 유지/페이드 시간은 유한한 0 이상이고 합은 양수여야 합니다.");
            Source = source;
            OverrideText = text ?? string.Empty;
            Target = target;
            Count = count;
            Color = color;
            FontSize = fontSize;
            PositionOffset = positionOffset;
            PrefabOverride = prefabOverride;
            HoldSeconds = hold;
            FadeSeconds = fade;
        }

        internal CombatBodyTextSource Source { get; }
        internal string OverrideText { get; }
        internal CombatBodyTextTarget Target { get; }
        internal CombatBodyTextCount Count { get; }
        internal Color Color { get; }
        internal float FontSize { get; }
        internal Vector2 PositionOffset { get; }
        internal GameObject PrefabOverride { get; }
        internal float HoldSeconds { get; }
        internal float FadeSeconds { get; }

        internal string Format(string traitName, int? cells) => (Source == CombatBodyTextSource.Override ? OverrideText : traitName) + (Count == CombatBodyTextCount.CompletedMovementCells && cells.HasValue ? $" X {cells.Value}" : string.Empty);
    }
}
