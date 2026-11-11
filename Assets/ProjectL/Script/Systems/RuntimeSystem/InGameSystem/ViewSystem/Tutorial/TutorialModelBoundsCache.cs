using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>설명 동안 모델의 최초 경계를 고정해 Idle 변형이 덮개·흰 점선·설명 배치를 흔들지 않게 합니다.</summary>
    internal sealed class TutorialModelBoundsCache
    {
        private readonly Dictionary<EntityId, Entry> _entries = new();

        internal bool TryGet(ICombatUnitHighlightTarget view, out Bounds bounds)
        {
            bounds = default;
            if (view == null || !view.IsActive || view.EntityId.IsEmpty) return false;
            Matrix4x4 localToWorld = view.LocalToWorld;
            if (!_entries.TryGetValue(view.EntityId, out Entry entry) || !entry.Matches(view))
            {
                if (!view.TryGetModelBounds(out Bounds current)) return false;
                // 애니메이션이 적용되는 자식 Renderer가 아닌 개체 루트 기준으로 고정합니다.
                entry = new Entry(view, TransformBounds(current, localToWorld.inverse));
                _entries[view.EntityId] = entry;
            }
            // 실제 루트 이동/회전·배율과 카메라 투영은 계속 따릅니다. Animator는 정지시키지 않습니다.
            bounds = TransformBounds(entry.LocalBounds, localToWorld);
            return true;
        }

        internal void Clear() => _entries.Clear();

        private static Bounds TransformBounds(Bounds source, Matrix4x4 matrix)
        {
            Vector3 min = source.min;
            Vector3 max = source.max;
            var result = new Bounds(matrix.MultiplyPoint3x4(min), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                result.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return result;
        }

        private readonly struct Entry
        {
            private readonly ICombatUnitHighlightTarget _view;
            private readonly int _bodyTrackingVersion;
            internal Bounds LocalBounds { get; }

            internal Entry(ICombatUnitHighlightTarget view, Bounds localBounds)
            {
                _view = view;
                _bodyTrackingVersion = view.BodyTrackingVersion;
                LocalBounds = localBounds;
            }

            internal bool Matches(ICombatUnitHighlightTarget view) => ReferenceEquals(_view, view)
                && _bodyTrackingVersion == view.BodyTrackingVersion;
        }
    }
}
