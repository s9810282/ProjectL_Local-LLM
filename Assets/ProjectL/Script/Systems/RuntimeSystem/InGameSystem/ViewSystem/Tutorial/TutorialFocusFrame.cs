using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>한 번 해석한 현재 설명의 강조 대상입니다. 매 갱신에 목록을 재사용합니다.</summary>
    internal sealed class TutorialFocusFrame
    {
        private readonly List<Rect> _holes = new();
        private readonly List<Rect> _uiRects = new();
        private readonly List<RectTransform> _uiTargets = new();
        private readonly List<TutorialModelFocus> _models = new();
        private readonly HashSet<EntityId> _modelIds = new();

        internal IReadOnlyList<Rect> Holes => _holes;
        internal IReadOnlyList<Rect> UiRects => _uiRects;
        internal IReadOnlyList<RectTransform> UiTargets => _uiTargets;
        internal IReadOnlyList<TutorialModelFocus> Models => _models;
        internal Rect? Anchor { get; set; }

        internal void Clear()
        {
            _holes.Clear();
            _uiRects.Clear();
            _uiTargets.Clear();
            _models.Clear();
            _modelIds.Clear();
            Anchor = null;
        }

        internal void AddUi(Rect rect)
        {
            if (_uiRects.Contains(rect)) return;
            _uiRects.Add(rect);
            _holes.Add(rect);
        }

        internal void AddUiTarget(RectTransform target)
        {
            if (target != null && !_uiTargets.Contains(target)) _uiTargets.Add(target);
        }

        internal void AddModel(ICombatUnitHighlightTarget view, Rect rect, Owner owner)
        {
            if (!_modelIds.Add(view.EntityId)) return;
            _models.Add(new TutorialModelFocus(view, owner));
            _holes.Add(rect);
        }
    }

    /// <summary>같은 표시의 개체·재바인딩을 구분하는 스냅숏입니다. 재질 교체 수명은 모델 대여가 별도로 검사합니다.</summary>
    internal readonly struct TutorialModelFocus
    {
        internal ICombatUnitHighlightTarget View { get; }
        internal EntityId EntityId { get; }
        internal int BodyTrackingVersion { get; }
        internal Owner Owner { get; }

        internal TutorialModelFocus(ICombatUnitHighlightTarget view, Owner owner)
        {
            View = view;
            EntityId = view.EntityId;
            BodyTrackingVersion = view.BodyTrackingVersion;
            Owner = owner;
        }

        // 계약의 IsActive가 파괴된 표시도 false로 답하므로 참조 null만 따로 봅니다.
        internal bool IsCurrent => View != null && View.IsActive
            && View.EntityId == EntityId && View.BodyTrackingVersion == BodyTrackingVersion;

        internal bool Matches(in TutorialModelFocus other) => ReferenceEquals(View, other.View)
            && EntityId == other.EntityId && BodyTrackingVersion == other.BodyTrackingVersion && Owner == other.Owner;
    }
}
