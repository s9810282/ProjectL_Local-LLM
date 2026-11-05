using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text
{
    /// <summary>프리팹 대여의 시간과 화면 투영만 진행하는 비차단 창구입니다.</summary>
    internal sealed class CombatBodyTextWindow : ICombatPresentationPlayer
    {
        // 전투 설치 때 프리팹마다 미리 만들 글자 수입니다(GC 끊김 계획 TP-6). 한 발동에 원인·대상 둘이 함께 뜨는 일이 흔해 둘입니다.
        private const int PrewarmedPerPrefab = 2;
        private readonly CombatBodyTextResources _resources;
        private readonly Camera _camera;
        private readonly GameObject _defaultPrefab;
        private readonly float _gap;
        private readonly Func<EntityId, Func<Vector3?>> _track;
        private readonly List<Entry> _active = new();
        private bool _subscribed;
        private bool _released;
        private sealed class Entry
        {
            internal EntityId Unit;
            internal Func<Vector3?> Position;
            internal CombatBodyTextPool.Rental Rental;
            internal CombatBodyTextData Data;
            internal int Slot;
            internal float Age;
        }

        internal CombatBodyTextWindow(CombatBodyTextResources resources, Camera camera, GameObject defaultPrefab, float gap, Func<EntityId, Func<Vector3?>> track)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            if (!CombatPresentationValueRules.IsNonNegative(gap))
                throw new ArgumentOutOfRangeException(nameof(gap));
            _defaultPrefab = defaultPrefab;
            _gap = gap;
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        /// <summary>발동 줄에 프리팹 덮어쓰기가 없을 때 쓰는 공통 글자 프리팹입니다(없으면 null). 미리 만들 프리팹을 고를 때 씁니다.</summary>
        internal GameObject DefaultPrefab => _defaultPrefab;

        /// <summary>
        /// 처음 띄울 때 만들던 글자를 전투 설치 때 프리팹마다 정해진 수만큼 꺼진 채 만들어 둡니다(GC 끊김 설계 GH6).
        /// 어떤 프리팹을 쓸지는 부르는 쪽(특성 발동 줄을 아는 사건 Presenter)이 정합니다.
        /// </summary>
        internal void Prewarm(IReadOnlyList<GameObject> prefabs)
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatBodyTextWindow));
            if (prefabs == null)
                throw new ArgumentNullException(nameof(prefabs));
            for (int i = 0; i < prefabs.Count; i++)
                _resources.Pool.Prewarm(prefabs[i], PrewarmedPerPrefab);
        }

        internal void Show(EntityId unit, string text, CombatBodyTextData data)
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatBodyTextWindow));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Func<Vector3?> position = _track(unit);
            if (position == null)
                return;
            GameObject prefab = data.PrefabOverride != null ? data.PrefabOverride : _defaultPrefab;
            if (prefab == null)
                throw new InvalidOperationException("[CombatBodyText] Prefab Override 또는 Body Text Prefab이 필요합니다.");
            int slot = 0;
            while (IsSlotTaken(unit, slot))
                slot++;
            CombatBodyTextPool.Rental rental = _resources.Pool.Rent(prefab, text, data.Color, data.FontSize);
            _active.Add(new Entry { Unit = unit, Position = position, Rental = rental, Data = data, Slot = slot });
            if (!_subscribed)
            {
                Canvas.willRenderCanvases += Project;
                _subscribed = true;
            }
        }

        private void Project()
        {
            // 씬 해제 중 출력 카메라/호스트가 먼저 사라지면 남은 표시를 회수합니다.
            if (_camera == null || _resources.Root == null)
            {
                Cancel();
                return;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Entry entry = _active[i];
                Vector3? body = entry.Position();
                CombatBodyTextView view = entry.Rental.View;
                if (!body.HasValue || view == null)
                {
                    ReturnAt(i);
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(body.Value);
                Vector2 point = default;
                bool visible = screen.z > 0 && _camera.pixelRect.Contains(screen) && RectTransformUtility.ScreenPointToLocalPointInRectangle(_resources.Root, screen, null, out point);
                if (visible)
                    view.Rect.anchoredPosition = point + entry.Data.PositionOffset + Vector2.up * (_gap * entry.Slot);
                view.SetVisible(visible);
            }
        }

        public void BeginResult(CombatResult result)
        {
        }

        public CombatStartBlock FindBlock(in CombatStartQuery query) => CombatStartBlock.None;
        public void BeginFrame(in CombatPlaybackFrame frame)
        {
        }

        public bool Tick(in CombatPlaybackFrame frame)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Entry entry = _active[i];
                entry.Age += frame.DeltaTime;
                if (entry.Age >= entry.Data.HoldSeconds + entry.Data.FadeSeconds)
                {
                    ReturnAt(i);
                    continue;
                }

                if (entry.Rental.View != null)
                    entry.Rental.View.SetOpacity(entry.Age <= entry.Data.HoldSeconds ? 1f : 1f - Mathf.Clamp01((entry.Age - entry.Data.HoldSeconds) / entry.Data.FadeSeconds));
            }

            return true;
        }

        // 그 유닛의 그 줄이 이미 떠 있는지입니다. 람다 없이 찾습니다(GC 끊김 설계 GH4).
        private bool IsSlotTaken(EntityId unit, int slot)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Unit == unit && _active[i].Slot == slot)
                    return true;
            return false;
        }

        private void ReturnAt(int index)
        {
            Entry entry = _active[index];
            _active.RemoveAt(index);
            try
            {
                entry.Rental.Dispose();
            }
            finally
            {
                if (_active.Count == 0)
                    Unsubscribe();
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            Canvas.willRenderCanvases -= Project;
            _subscribed = false;
        }

        public bool IsComplete => true;
        public bool BlocksFinalFacts => false;

        public void Cancel()
        {
            Unsubscribe();
            List<Exception> errors = null;
            while (_active.Count > 0)
            {
                try
                {
                    ReturnAt(_active.Count - 1);
                }
                catch (Exception error)
                {
                    (errors ??= new()).Add(error);
                }
            }

            if (errors != null)
                throw new AggregateException("[CombatBodyText] 텍스트 회수에 실패했습니다.", errors);
        }

        public void Release()
        {
            if (_released)
                return;
            _released = true;
            try
            {
                Cancel();
            }
            finally
            {
                _resources.Dispose();
            }
        }
    }
}
