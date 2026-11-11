using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 개체 ID → 표시 View 연결입니다(설계 §6.1, W19). 한 번 그릴 때 보인 ID는 기존 View를 다시 쓰고,
    /// 새로 보인 대상만 빌리며 사라진 대상만 돌려줍니다.
    /// </summary>
    /// <remarks>
    /// 표현 자원 연결일 뿐 게임 상태의 원본이 아닙니다. 대여·반환은 주입한 함수(종류별 풀)가 합니다.
    /// 그리기 한 번은 <see cref="Begin"/> → 보이는 대상마다 <see cref="Acquire"/> → <see cref="End"/>입니다.
    /// </remarks>
    internal sealed class EntityViewSet<TView> where TView : class
    {
        private sealed class Entry
        {
            internal TView View;
            internal EntityKey Key;
            internal int Pass;
        }

        private readonly Func<EntityKey, TView> _rent;
        private readonly Action<EntityKey, TView> _return;
        private readonly Dictionary<EntityId, Entry> _entries = new();
        private readonly List<EntityId> _removal = new();
        private int _pass;
        private bool _isRendering;

        internal EntityViewSet(Func<EntityKey, TView> rent, Action<EntityKey, TView> @return)
        {
            _rent = rent ?? throw new ArgumentNullException(nameof(rent));
            _return = @return ?? throw new ArgumentNullException(nameof(@return));
        }

        internal int Count => _entries.Count;

        internal void Begin()
        {
            if (_isRendering)
                throw new InvalidOperationException("[EntityViewSet] 이전 그리기가 끝나지 않았습니다.");

            _isRendering = true;
            _pass++;
        }

        /// <summary>이번 그리기에 보이는 대상의 View를 돌려줍니다. 처음 보이거나 종류가 바뀐 대상만 빌립니다.</summary>
        internal TView Acquire(EntityId id, EntityKey key)
        {
            if (!_isRendering)
                throw new InvalidOperationException("[EntityViewSet] Begin 뒤에만 View를 얻을 수 있습니다.");

            if (_entries.TryGetValue(id, out Entry entry))
            {
                if (entry.Key != key)
                {
                    // 반환·대여가 실패해도 빈 항목(View null, 옛 키)이 남아 끝 정리가 같은 View를 한 번 더 반환하지 않게 먼저 뺍니다(평가 C10-16).
                    // 성공하면 같은 항목 객체를 다시 넣으므로 할당이 없습니다.
                    _entries.Remove(id);
                    _return(entry.Key, entry.View);
                    entry.View = _rent(key);
                    entry.Key = key;
                    _entries.Add(id, entry);
                }

                entry.Pass = _pass;
                return entry.View;
            }

            TView view = _rent(key);
            _entries.Add(id, new Entry { View = view, Key = key, Pass = _pass });
            return view;
        }

        /// <summary>이번 그리기에 보이지 않은 대상만 돌려줍니다.</summary>
        internal void End()
        {
            if (!_isRendering)
                throw new InvalidOperationException("[EntityViewSet] Begin 없이 끝낼 수 없습니다.");

            _isRendering = false;
            _removal.Clear();
            foreach (KeyValuePair<EntityId, Entry> pair in _entries)
            {
                if (pair.Value.Pass != _pass)
                    _removal.Add(pair.Key);
            }

            for (int i = 0; i < _removal.Count; i++)
                Remove(_removal[i]);
            _removal.Clear();
        }

        internal bool TryGet(EntityId id, out TView view)
        {
            if (_entries.TryGetValue(id, out Entry entry))
            {
                view = entry.View;
                return true;
            }

            view = null;
            return false;
        }

        /// <summary>해당 ID의 같은 대여만 반환합니다. 이전 유지권이 새 대여를 반환할 수 없습니다.</summary>
        internal void Release(EntityId id, TView expected)
        {
            if (_entries.TryGetValue(id, out Entry entry) && ReferenceEquals(entry.View, expected))
                Remove(id);
        }

        /// <summary>모든 View를 돌려줍니다. 하나가 실패해도 나머지를 돌려준 뒤 첫 실패를 던집니다.</summary>
        internal void Clear()
        {
            _isRendering = false;
            Exception first = null;
            foreach (Entry entry in _entries.Values)
            {
                try
                {
                    _return(entry.Key, entry.View);
                }
                catch (Exception exception)
                {
                    first ??= exception;
                }
            }

            _entries.Clear();
            if (first != null)
                throw new InvalidOperationException("[EntityViewSet] View 반환 중 실패했습니다.", first);
        }

        private void Remove(EntityId id)
        {
            Entry entry = _entries[id];
            _entries.Remove(id);
            _return(entry.Key, entry.View);
        }
    }
}
