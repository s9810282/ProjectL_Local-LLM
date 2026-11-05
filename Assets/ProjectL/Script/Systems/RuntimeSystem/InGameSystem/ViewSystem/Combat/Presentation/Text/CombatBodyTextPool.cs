using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text
{
    /// <summary>프리팹별 재사용과 인스턴스 반환 소유권을 관리합니다.</summary>
    internal sealed class CombatBodyTextPool : IDisposable
    {
        internal sealed class Rental : IDisposable
        {
            private readonly CombatBodyTextPool _owner;
            internal Rental(CombatBodyTextPool owner, GameObject prefab, CombatBodyTextView view)
            {
                _owner = owner;
                Prefab = prefab;
                View = view;
            }

            internal GameObject Prefab { get; }
            internal CombatBodyTextView View { get; }

            public void Dispose() => _owner.Return(this);
        }

        private sealed class Bucket
        {
            internal Bucket(CombatBodyTextView template) => Template = template;
            internal CombatBodyTextView Template { get; }
            internal Stack<CombatBodyTextView> Available { get; } = new();
            // 이 프리팹으로 만든 글자 수입니다(빌려 간 것 포함). 미리 만들기가 모자란 수를 셉니다(GC 끊김 설계 GH6).
            internal int Created { get; set; }
        }

        private readonly RectTransform _parent;
        private readonly Dictionary<GameObject, Bucket> _buckets = new();
        private readonly Dictionary<CombatBodyTextView, Rental> _rented = new();
        private bool _released;
        internal CombatBodyTextPool(RectTransform parent) => _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
        internal Rental Rent(GameObject prefab, string text, Color color, float fontSize)
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatBodyTextPool));
            if (prefab == null)
                throw new InvalidOperationException("[CombatBodyTextPool] 대여할 텍스트 프리팹 참조가 필요합니다.");
            Bucket bucket = BucketOf(prefab);

            CombatBodyTextView view = null;
            while (bucket.Available.Count > 0 && view == null)
                view = bucket.Available.Pop();
            if (view == null)
                view = CreateView(bucket);

            var rental = new Rental(this, prefab, view);
            _rented.Add(view, rental);
            try
            {
                view.Prepare(text, color, fontSize);
            }
            catch
            {
                Return(rental);
                throw;
            }

            return rental;
        }

        /// <summary>
        /// 그 프리팹으로 만든 글자가 <paramref name="count"/>보다 적으면 모자란 만큼 꺼진 채 만들어 둡니다(GC 끊김 설계 GH6). 빌려 간 것도 만든 수에 듭니다.
        /// 처음 대여 때 만들던 것을 전투 설치 때로 옮깁니다. 만든 글자는 대여 때 새로 만든 것과 같은 상태입니다.
        /// </summary>
        internal void Prewarm(GameObject prefab, int count)
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatBodyTextPool));
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            Bucket bucket = BucketOf(prefab);
            while (bucket.Created < count)
                bucket.Available.Push(CreateView(bucket));
        }

        // 그 프리팹의 묶음입니다. 처음 부를 때 프리팹을 검사하고 만듭니다.
        private Bucket BucketOf(GameObject prefab)
        {
            if (!_buckets.TryGetValue(prefab, out Bucket bucket))
            {
                bucket = new Bucket(CombatBodyTextView.RequirePrefab(prefab));
                _buckets.Add(prefab, bucket);
            }
            return bucket;
        }

        // 글자 하나를 꺼진 채 만듭니다. 실패하면 만든 것을 지우고 던집니다.
        private CombatBodyTextView CreateView(Bucket bucket)
        {
            CombatBodyTextView view = UnityEngine.Object.Instantiate(bucket.Template, _parent, false);
            try
            {
                view.gameObject.SetActive(false);
                view.InitializeInstance();
            }
            catch
            {
                CombatBodyTextObjects.Destroy(view.gameObject);
                throw;
            }
            bucket.Created++;
            return view;
        }

        private void Return(Rental rental)
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatBodyTextPool));
            if (!_rented.TryGetValue(rental.View, out Rental current) || !ReferenceEquals(current, rental))
                throw new InvalidOperationException("[CombatBodyTextPool] 소유하지 않은 대여 또는 중복 반환입니다.");
            _rented.Remove(rental.View);
            if (rental.View == null)
                return;
            rental.View.ResetForPool();
            _buckets[rental.Prefab].Available.Push(rental.View);
        }

        public void Dispose()
        {
            if (_released)
                return;
            _released = true;
            foreach (CombatBodyTextView view in _rented.Keys)
                if (view != null)
                    CombatBodyTextObjects.Destroy(view.gameObject);
            _rented.Clear();
            foreach (Bucket bucket in _buckets.Values)
                while (bucket.Available.Count > 0)
                {
                    CombatBodyTextView view = bucket.Available.Pop();
                    if (view != null)
                        CombatBodyTextObjects.Destroy(view.gameObject);
                }

            _buckets.Clear();
        }
    }

    internal static class CombatBodyTextObjects
    {
        internal static void Destroy(UnityEngine.Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(value);
            else
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
