using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>공개된 Object 원본 ID별 View 연결입니다. 위치·규칙 상태를 복제하지 않습니다.</summary>
    internal sealed class ObjectViewSet<TView> where TView : class
    {
        private sealed class Entry
        {
            internal TView View;
            internal ObjectContentKind Kind;
            internal long Pass;
        }

        private readonly Func<ObjectContentKind, TView> _rent;
        private readonly Action<ObjectContentKind, TView> _return;
        private readonly Dictionary<long, Entry> _entries = new();
        private readonly HashSet<long> _classified = new();
        private readonly List<long> _removal = new();
        private long _pass;

        internal ObjectViewSet(Func<ObjectContentKind, TView> rent, Action<ObjectContentKind, TView> returnView)
        {
            _rent = rent ?? throw new ArgumentNullException(nameof(rent));
            _return = returnView ?? throw new ArgumentNullException(nameof(returnView));
        }

        /// <summary>Player에게 공개된 활성 Object만 적용하고 이번 표시에서 빠진 원본을 반환합니다.</summary>
        internal void Render(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            Action<TView, ObjectSourceFact> apply)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            _classified.Clear();
            foreach (long id in bindings.ObjectSourceIds) _classified.Add(id);
            _pass++;
            try
            {
                foreach (MovementTileFact tile in environment.Tiles)
                {
                    if (!_classified.Contains(tile.Id)) continue;
                    if (tile is not ObjectSourceFact source)
                        throw new InvalidOperationException($"[ObjectViewSet] Object 분류와 원본 형식이 다릅니다: {tile.Id}.");
                    if (!CombatEnvironmentFacts.IsShown(source)) continue;

                    ObjectContentKind kind = EnvironmentVisualKinds.ObjectKind(source);
                    if (_entries.TryGetValue(source.Id, out Entry entry) && entry.Kind != kind)
                    {
                        Remove(source.Id);
                        entry = null;
                    }
                    if (entry == null)
                    {
                        TView view = _rent(kind) ?? throw new InvalidOperationException("[ObjectViewSet] 대여한 View가 없습니다.");
                        entry = new Entry { View = view, Kind = kind };
                        _entries.Add(source.Id, entry);
                    }
                    entry.Pass = _pass;
                    apply(entry.View, source);
                }

                _removal.Clear();
                foreach (var pair in _entries)
                    if (pair.Value.Pass != _pass) _removal.Add(pair.Key);
                foreach (long id in _removal) Remove(id);
            }
            catch (Exception error)
            {
                // 일부만 갱신된 표시를 남기지 않고, 대여한 자원을 모두 반환합니다.
                try
                {
                    Clear();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            finally
            {
                _classified.Clear();
                _removal.Clear();
            }
        }

        internal bool TryGet(long sourceId, out TView view)
        {
            if (_entries.TryGetValue(sourceId, out Entry entry))
            {
                view = entry.View;
                return true;
            }
            view = null;
            return false;
        }

        /// <summary>반환 하나가 실패해도 남은 View를 모두 반환하고 연결을 비웁니다.</summary>
        internal void Clear()
        {
            List<Exception> errors = null;
            foreach (Entry entry in _entries.Values)
            {
                try
                {
                    _return(entry.Kind, entry.View);
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            _entries.Clear();
            _classified.Clear();
            _removal.Clear();
            if (errors != null) throw new AggregateException("[ObjectViewSet] View 반환에 실패했습니다.", errors);
        }

        private void Remove(long sourceId)
        {
            Entry entry = _entries[sourceId];
            _entries.Remove(sourceId);
            _return(entry.Kind, entry.View);
        }
    }
}
