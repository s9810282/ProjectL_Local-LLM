using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 외형 프리팹 루트 BoxCollider의 모양(루트 로컬 <c>center</c>·<c>size</c>) 캐시입니다(히트 타이밍 설계 H4).
    /// 표시 Presenter마다 하나를 두고, 설정 구간(Configure·Clear) 안에서 프리팹마다 한 번 읽습니다. 콜라이더가 없거나 XZ 크기가 0이면
    /// 그때 한 번 경고합니다(점 몸 대체). 프리팹이 없으면 대상 이름마다 한 번 경고합니다. 콜라이더의 켜짐 여부와 관계없이 값만 읽고 물리는 쓰지 않습니다.
    /// </summary>
    internal sealed class CombatBodyShapeCache
    {
        private readonly struct Shape
        {
            internal Shape(bool hasBox, Vector3 center, Vector3 size)
            {
                HasBox = hasBox;
                Center = center;
                Size = size;
            }

            internal bool HasBox { get; }
            internal Vector3 Center { get; }
            internal Vector3 Size { get; }
        }

        private readonly string _owner;
        private readonly bool _includeRootScale;
        private readonly Dictionary<GameObject, Shape> _shapes = new();
        private readonly HashSet<object> _missingPrefabLabels = new();

        /// <param name="owner">경고 앞에 붙일 표시 이름입니다(예: <c>[FieldObjectPresenter]</c>).</param>
        /// <param name="includeRootScale">
        /// true면 프리팹 루트 크기를 곱한 모양을 돌려줍니다(인스턴스 크기를 따로 곱하는 표시용). 크기 0 판정도 곱한 값으로 합니다.
        /// false면 콜라이더 값 그대로입니다(인스턴스 Transform으로 배치하는 표시용).
        /// </param>
        internal CombatBodyShapeCache(string owner, bool includeRootScale = false)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("표시 이름이 필요합니다.", nameof(owner));
            _owner = owner;
            _includeRootScale = includeRootScale;
        }

        /// <summary>프리팹 루트 BoxCollider의 로컬 중심·크기(절댓값)입니다. 없거나 XZ 크기가 0이면 false입니다.</summary>
        /// <param name="label">경고에 쓸 대상 이름(외형 키·종류)입니다.</param>
        internal bool TryGet<TLabel>(GameObject prefab, TLabel label, out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.zero;
            if (prefab == null)
            {
                if (_missingPrefabLabels.Add(label)) Warn(label, null);
                return false;
            }
            if (!_shapes.TryGetValue(prefab, out Shape shape))
            {
                shape = Read(prefab, _includeRootScale);
                _shapes.Add(prefab, shape);
                if (!shape.HasBox) Warn(label, prefab);
            }
            center = shape.Center;
            size = shape.Size;
            return shape.HasBox;
        }

        /// <summary>설정 구간을 끝냅니다. 다음 조회는 프리팹을 다시 읽고 다시 경고합니다.</summary>
        internal void Clear()
        {
            _shapes.Clear();
            _missingPrefabLabels.Clear();
        }

        private static Shape Read(GameObject prefab, bool includeRootScale)
        {
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            if (box == null) return new Shape(false, Vector3.zero, Vector3.zero);
            Vector3 rootScale = includeRootScale ? prefab.transform.localScale : Vector3.one;
            Vector3 size = Vector3.Scale(box.size, rootScale);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            if (!(size.x > 0f || size.z > 0f) || !float.IsFinite(size.x) || !float.IsFinite(size.z))
                return new Shape(false, Vector3.zero, Vector3.zero);
            return new Shape(true, Vector3.Scale(box.center, rootScale), size);
        }

        private void Warn<TLabel>(TLabel label, GameObject prefab) =>
            Debug.LogWarning(prefab == null
                    ? $"{_owner} 외형 프리팹이 없어 표시 중심의 점 몸으로 접촉을 계산합니다: {label}."
                    : $"{_owner} 외형 프리팹 루트에 BoxCollider가 없거나 XZ 크기가 0이라 표시 중심의 점 몸으로 접촉을 계산합니다: {label}.",
                prefab);
    }
}
