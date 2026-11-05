using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 공개된 지형(타일 위·바닥 교체)의 원본 ID별 View 연결입니다. 전투 상태를 복제하지 않습니다.
    /// View는 외형 열쇠(종류·이웃 모양)로 빌립니다. 어떤 열쇠로 그릴지는 생성 때 받은 외형 선택이 정하고, 이 클래스는 고르는 법을 모릅니다(지형 이웃 모양 설계 TC4).
    /// </summary>
    internal sealed class TerrainViewSet<TView> where TView : class
    {
        private sealed class Entry
        {
            internal TView View;
            internal TerrainLookKey Key;
            internal long Pass;
        }

        private readonly Func<TerrainLookKey, TView> _rent;
        private readonly Action<TerrainLookKey, TView> _return;
        private readonly Func<TerrainContentKind, bool> _canDisplay;
        private readonly Func<MovementTileFact, TerrainContentKind, TerrainLook> _lookOf;
        private readonly Dictionary<long, Entry> _entries = new();
        private readonly HashSet<long> _classified = new();
        private readonly List<long> _removal = new();
        private long _pass;

        internal TerrainViewSet(Func<TerrainLookKey, TView> rent, Action<TerrainLookKey, TView> returnView,
            Func<TerrainContentKind, bool> canDisplay, Func<MovementTileFact, TerrainContentKind, TerrainLook> lookOf)
        {
            _rent = rent ?? throw new ArgumentNullException(nameof(rent));
            _return = returnView ?? throw new ArgumentNullException(nameof(returnView));
            _canDisplay = canDisplay ?? throw new ArgumentNullException(nameof(canDisplay));
            _lookOf = lookOf ?? throw new ArgumentNullException(nameof(lookOf));
        }

        /// <summary>
        /// Player에게 공개된 활성 지형 중 그릴 수 있는 것을 적용하고 이번 표시에서 빠진 원본을 반환합니다.
        /// 같은 원본의 외형 열쇠가 바뀌면(예: 숨어 있던 이웃이 공개돼 모양이 바뀜) 이전 View를 반환하고 새로 빌립니다.
        /// </summary>
        internal void Render(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            Action<TView, MovementTileFact, TerrainLook> apply)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            Classify(bindings);
            _pass++;
            try
            {
                foreach (MovementTileFact source in environment.Tiles)
                {
                    if (!TryShow(source, out TerrainContentKind kind) || !_canDisplay(kind)) continue;
                    ShowOne(source, kind, apply);
                }

                _removal.Clear();
                foreach (var pair in _entries)
                    if (pair.Value.Pass != _pass) _removal.Add(pair.Key);
                foreach (long id in _removal) Remove(id);
            }
            catch (Exception error)
            {
                // 일부만 갱신된 표시를 남기지 않고 대여한 자원을 모두 반환합니다.
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

        /// <summary>
        /// 보여 줄 지형 원본마다 <paramref name="visit"/>를 부릅니다. 판단은 <see cref="Render"/>와 같은 하나이고(고지대 View 설계 HG1),
        /// 표시 연결은 바꾸지 않습니다. 칸 표면 높이 표와 이웃 모양용 이웃 표(지형 이웃 모양 설계 TC2)가 씁니다.
        /// </summary>
        internal void VisitShown(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            Action<MovementTileFact, TerrainContentKind> visit)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (visit == null) throw new ArgumentNullException(nameof(visit));

            Classify(bindings);
            try
            {
                foreach (MovementTileFact source in environment.Tiles)
                    if (TryShow(source, out TerrainContentKind kind)) visit(source, kind);
            }
            finally
            {
                _classified.Clear();
            }
        }

        /// <summary>현재 대여 중인 외형만 방문합니다. 목록을 변경하지 않는 진입 대상 수집에 씁니다.</summary>
        internal void VisitViews(Action<TView> visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));
            foreach (Entry entry in _entries.Values) visitor(entry.View);
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

        /// <summary>
        /// 이미 보이고 있는 원본 하나를 새 사실로 다시 보이게 합니다(약한 지반 붕괴 순간, 약한 지반 설계 WG6). 항목이 없으면 아무것도 하지 않고
        /// false를 돌려줍니다(결과 끝 <see cref="Render"/>가 맞춤). 항목이 있다는 것은 지난 Render가 분류를 확인했다는 뜻이라 분류는 다시 보지 않습니다.
        /// 새 사실이 꺼졌거나 공개되지 않았거나 그릴 수 없으면, 또는 외형 열쇠가 바뀌면 옛 View를 연결에서 빼 내보냅니다(<paramref name="leave"/>, 없으면 반환).
        /// 실패하면 예외를 그대로 올리고, 빠진 원본은 다음 Render가 다시 빌립니다.
        /// </summary>
        /// <param name="leave">
        /// 연결에서 빠지는 옛 View를 받습니다(약한 지반 붕괴의 떨림·가라앉기, 타일 교체 설계 TR8). 받은 쪽이 다 쓴 뒤 반환합니다.
        /// 없으면 바로 반환합니다. 연결은 어느 쪽이든 그 자리에서 바뀝니다.
        /// </param>
        internal bool TryReplace(MovementTileFact source, Action<TView, MovementTileFact, TerrainLook> apply,
            Action<TerrainLookKey, TView> leave = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            if (!_entries.ContainsKey(source.Id)) return false;

            TerrainContentKind kind = EnvironmentVisualKinds.TerrainKind(source);
            if (CombatEnvironmentFacts.IsShown(source) && _canDisplay(kind)) ShowOne(source, kind, apply, leave);
            else Remove(source.Id, leave);
            return true;
        }

        /// <summary>반환 하나가 실패해도 남은 View를 모두 반환하고 연결을 비웁니다.</summary>
        internal void Clear()
        {
            List<Exception> errors = null;
            foreach (Entry entry in _entries.Values)
            {
                try
                {
                    _return(entry.Key, entry.View);
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            _entries.Clear();
            _classified.Clear();
            _removal.Clear();
            if (errors != null) throw new AggregateException("[TerrainViewSet] View 반환에 실패했습니다.", errors);
        }

        // 보여 줄 원본 하나를 외형 열쇠로 연결하고 적용합니다. Render와 TryReplace가 함께 쓰는 한 곳입니다.
        // 같은 원본의 열쇠가 바뀌면(종류·이웃 모양) 이전 View를 내보내고(없으면 반환) 새로 빌립니다.
        private void ShowOne(MovementTileFact source, TerrainContentKind kind, Action<TView, MovementTileFact, TerrainLook> apply,
            Action<TerrainLookKey, TView> leave = null)
        {
            TerrainLook look = _lookOf(source, kind);
            if (look.Kind != kind)
                throw new InvalidOperationException($"[TerrainViewSet] 외형 선택이 다른 종류를 돌려줬습니다: {kind} → {look.Kind}.");
            if (_entries.TryGetValue(source.Id, out Entry entry) && entry.Key != look.Key)
            {
                Remove(source.Id, leave);
                entry = null;
            }
            if (entry == null)
            {
                TView view = _rent(look.Key) ?? throw new InvalidOperationException("[TerrainViewSet] 대여한 View가 없습니다.");
                entry = new Entry { View = view, Key = look.Key };
                _entries.Add(source.Id, entry);
            }
            entry.Pass = _pass;
            apply(entry.View, source, look);
        }

        // 연결을 끊고 View를 내보냅니다. 받는 쪽(leave)이 없으면 바로 반환합니다.
        private void Remove(long sourceId, Action<TerrainLookKey, TView> leave = null)
        {
            Entry entry = _entries[sourceId];
            _entries.Remove(sourceId);
            (leave ?? _return)(entry.Key, entry.View);
        }

        private void Classify(EnvironmentSemanticBindings bindings)
        {
            _classified.Clear();
            foreach (long id in bindings.TerrainSourceIds) _classified.Add(id);
        }

        /// <summary>
        /// 원본 하나를 보여 줄지 정하는 지형 표시의 판단입니다(Render·VisitShown이 함께 씀): Terrain 분류 · 외형 종류 · 보임.
        /// 보임(켜짐 · Player 공개)은 환경 표시·입력이 함께 쓰는 <see cref="CombatEnvironmentFacts.IsShown"/> 한 곳이 정합니다(평가 C1-05).
        /// 바닥 교체 지형도 같은 판단을 거치고, 그릴지는 외형(프리팹 유무)이 정합니다(고지대 View 설계 HG2).
        /// 분류된 원본의 형식이 지원되지 않으면 종류 해석이 계약 오류를 냅니다.
        /// </summary>
        private bool TryShow(MovementTileFact source, out TerrainContentKind kind)
        {
            kind = TerrainContentKind.None;
            if (!_classified.Contains(source.Id)) return false;
            kind = EnvironmentVisualKinds.TerrainKind(source);
            return CombatEnvironmentFacts.IsShown(source);
        }
    }
}
