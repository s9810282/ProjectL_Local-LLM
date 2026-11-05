using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>공용 Overlay Canvas에서 유닛 HUD의 화면 투영·가시성과 선택 HUD의 표시 우선순위를 관리합니다.</summary>
    /// <remarks>
    /// Canvas/Scaler 설정과 HUD 생성·풀·파괴는 호출자가 소유합니다.
    /// 등록한 HUD는 이 Layer의 직접 자식이어야 하며 루트 CanvasGroup은 Layer가 제어합니다.
    /// 화면 밖 숨김은 HUD를 비활성화하지 않아 수치·피해 연출을 중단하지 않습니다.
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitHUDLayer : UIGroup
    {
        public UnitHUDLayer() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.UnitHUDLayer;
        private IUIRuntimeRegistry _runtimeRegistry;
        private IUISystemService _ui;
        private EntityId _selectedEntity;
        private UnitHUDInfoPanel _selectedInfo;
        private UnitHUDHoverPanel _selectedHover;
        private bool _selectedInfoShown;
        // 지난 갱신에서 찾은 선택 HUD. 참조 검사는 이 HUD가 바뀔 때만 한다.
        private UnitHUD _selectedHud;

        internal void AttachUISystem(IUISystemService ui)
        {
            // 기존 격리 연출 시험은 UISystem 없이 조립할 수 있다. 운영 From 경로는 항상 전달한다.
            if (ui == null) { Open(); return; }
            if (!(ui is IUIRuntimeRegistry registry))
                throw new InvalidOperationException("[UnitHUDLayer] UIId.UnitHUDLayer의 동적 UI 등록 계약이 필요합니다.");
            registry.RegisterRuntimeUI(this);
            _runtimeRegistry = registry;
            _ui = ui;
            ui.OpenUI(ID, true);
        }

        internal void SetSelectedEntity(EntityId entity)
        {
            _selectedEntity = entity;
            RefreshSelectedInfo();
        }

        private void RefreshSelectedInfo()
        {
            // UISystem 없는 격리 투영 시험에서는 독립 패널을 열지 않는다.
            if (_runtimeRegistry == null) return;
            UnitHUD selected = null;
            if (!_selectedEntity.IsEmpty && isActiveAndEnabled)
                for (int i = 0; i < _entries.Count; i++)
                    if (_entries[i].Hud != null && _entries[i].Visible &&
                        _entries[i].Hud.EntityId == _selectedEntity)
                    {
                        selected = _entries[i].Hud;
                        // 선택·보임이 바뀌어 다른 HUD가 잡혔을 때만 검사한다. HUD 개체는 Layer 모르게 바뀔 수 있어 찾기는 매번 한다.
                        if (!ReferenceEquals(selected, _selectedHud)) selected.ValidateReferences();
                        // 같은 Canvas에서는 뒤쪽 sibling이 앞에 그려진다. 선택 HUD 전체를 올려 두 패널도 함께 보호한다.
                        RectTransform selectedRect = _entries[i].Rect;
                        if (selectedRect.GetSiblingIndex() != transform.childCount - 1)
                            selectedRect.SetAsLastSibling();
                        break;
                    }
            _selectedHud = selected;
            UnitHUDInfoPanel next = selected == null ? null : selected.InfoPanel;
            if (ReferenceEquals(next, _selectedInfo))
            {
                if (next != null && !_selectedInfoShown)
                {
                    _ui.OpenUI(next.ID, true);
                    _selectedInfoShown = true;
                }
                return;
            }
            ReleaseSelectedInfo();
            if (next == null) return;
            _runtimeRegistry.RegisterRuntimeUI(next);
            try { _runtimeRegistry.RegisterRuntimeUI(next.HoverPanel); }
            catch
            {
                _runtimeRegistry.UnregisterRuntimeUI(next);
                throw;
            }
            _selectedInfo = next;
            _selectedHover = next.HoverPanel;
            next.SetRegistered(true);
            _ui.OpenUI(next.ID, true);
            _selectedInfoShown = true;
        }

        private void ReleaseSelectedInfo()
        {
            if (ReferenceEquals(_selectedInfo, null)) return;
            UnitHUDInfoPanel panel = _selectedInfo;
            UnitHUDHoverPanel hover = _selectedHover;
            _selectedInfo = null;
            _selectedHover = null;
            _selectedInfoShown = false;
            if (panel != null)
            {
                panel.ClearHover();
                panel.SetRegistered(false);
            }
            _runtimeRegistry.UnregisterRuntimeUI(hover);
            _runtimeRegistry.UnregisterRuntimeUI(panel);
        }
        public override void Open()
        {
            ThrowIfReleased();
            base.Open();
        }

        private readonly List<Entry> _entries = new();
        private readonly List<ProjectionLease> _widgets = new();

        /// <summary>일반 위젯은 입력을 막지 않으며 모델과 무관한 앵커를 사용합니다.</summary>
        internal IDisposable RegisterWidget(RectTransform rect, Vector3 worldPosition)
        {
            ThrowIfReleased();
            if (_canvas == null || rect == null || rect.parent != transform)
                throw new InvalidOperationException("[UnitHUDLayer] Configure 뒤 Layer 직계 자식 위젯을 등록해야 합니다.");
            if (!UnitHUDProjection.IsValidBaseScale(rect.localScale))
                throw new InvalidOperationException("[UnitHUDLayer] 위젯의 localScale은 유한한 양수여야 합니다.");
            foreach (ProjectionLease item in _widgets)
                if (item.Rect == rect) throw new InvalidOperationException("[UnitHUDLayer] 위젯 중복 등록입니다.");
            var lease = new ProjectionLease(this, rect, worldPosition);
            _widgets.Add(lease);
            return lease;
        }

        private sealed class ProjectionLease : IDisposable
        {
            private UnitHUDLayer _owner;
            internal readonly RectTransform Rect;
            internal readonly Vector3 Position;
            internal readonly Vector3 BaseScale;
            internal readonly CanvasGroup Group;
            internal ProjectionLease(UnitHUDLayer owner, RectTransform rect, Vector3 position)
            {
                _owner = owner;
                Rect = rect;
                Position = position;
                BaseScale = rect.localScale;
                // Unity의 미연결·파괴된 Object는 CLR null과 달라 ?? 대신 Unity null 비교로 검사합니다.
                CanvasGroup group = rect.GetComponent<CanvasGroup>();
                if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
                Group = group;
                Group.alpha = 0f;
                Group.interactable = Group.blocksRaycasts = false;
            }
            public void Dispose()
            {
                UnitHUDLayer owner = _owner;
                if (owner == null) return;
                _owner = null;
                owner._widgets.Remove(this);
                if (Group != null) Group.alpha = 0f;
                if (Rect != null) Rect.localScale = BaseScale;
            }
        }

        private bool Project(RectTransform rect, Vector3 world, Vector3 baseScale)
        {
            Camera camera = _outputCamera;
            if (camera == null || !camera.isActiveAndEnabled || !_canvas.isActiveAndEnabled ||
                camera.targetDisplay != _canvas.targetDisplay ||
                !UnitHUDProjection.TryProject(camera, _canvas, _rect, world, out Vector2 local, out float scale)) return false;
            Vector3 scaled = baseScale * scale;
            if (!UnitHUDProjection.IsValidBaseScale(scaled)) return false;
            // 같은 값을 다시 쓰면 Canvas가 다시 묶이므로 바뀔 때만 씁니다(GC 끊김 설계 GH5). 카메라·유닛이 멈추면 쓰지 않습니다.
            var position = new Vector3(local.x, local.y, 0f);
            if (rect.localPosition != position) rect.localPosition = position;
            if (rect.localScale != scaled) rect.localScale = scaled;
            return true;
        }
        private RectTransform _rect;
        private Canvas _canvas;
        private Camera _outputCamera;
        private bool _subscribed;
        private bool _released;

        private sealed class Entry
        {
            internal UnitHUD Hud;
            internal RectTransform Rect;
            internal CanvasGroup Group;
            internal IUnitHUDAnchor Anchor;
            internal bool Visible;
            internal Vector3 BaseLocalScale;
            internal bool Preparing;
            internal bool RenderPreparation;
            internal bool OwnerWasVisible;
        }

        /// <summary>로딩 화면 뒤의 실제 Canvas 중앙에서 준비합니다. 준비 HUD는 선택·투영·입력에서 제외합니다.</summary>
        internal void BeginPreparation(UnitHUD hud, bool isCovered)
        {
            ThrowIfReleased();
            Entry entry = FindEntry(hud);
            if (entry.Preparing)
                throw new InvalidOperationException("[UnitHUDLayer] HUD를 중복 준비할 수 없습니다.");
            if (!isActiveAndEnabled || _canvas == null || !_canvas.isActiveAndEnabled)
                throw new InvalidOperationException("[UnitHUDLayer] 준비할 실제 Canvas와 Layer가 활성 상태여야 합니다.");
            entry.OwnerWasVisible = hud.OwnerVisible;
            entry.Preparing = true;
            entry.RenderPreparation = isCovered;
            hud.SetOwnerVisible(true);
            PositionPreparation(entry);
        }

        internal void EndPreparation(UnitHUD hud)
        {
            // 씬 파괴 뒤의 중복 정리도 허용합니다.
            foreach (Entry entry in _entries)
            {
                if (!ReferenceEquals(entry.Hud, hud) || !entry.Preparing) continue;
                entry.Preparing = false;
                entry.Visible = true;
                List<Exception> errors = null;
                CleanupErrors.Try(() => SetVisible(entry, false), ref errors);
                CleanupErrors.Try(() =>
                {
                    if (entry.Rect != null) entry.Rect.localScale = entry.BaseLocalScale;
                }, ref errors);
                CleanupErrors.Try(() =>
                {
                    if (entry.Hud != null) entry.Hud.SetOwnerVisible(entry.OwnerWasVisible);
                }, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[UnitHUDLayer] 준비 표시 복원에 실패했습니다.");
                return;
            }
        }

        internal void ValidatePreparation(UnitHUD hud)
        {
            ThrowIfReleased();
            Entry entry = FindEntry(hud);
            if (!entry.Preparing || !hud.isActiveAndEnabled || !isActiveAndEnabled ||
                _canvas == null || !_canvas.isActiveAndEnabled || _rect.rect.width <= 0f || _rect.rect.height <= 0f)
                throw new InvalidOperationException("[UnitHUDLayer] 준비 중 HUD/Canvas의 활성 상태 또는 크기가 유효하지 않습니다.");
            if (!entry.RenderPreparation) return;
            foreach (CanvasGroup group in GetComponentsInParent<CanvasGroup>())
                if (group.alpha < 1f)
                    throw new InvalidOperationException("[UnitHUDLayer] 렌더 준비 중 부모 CanvasGroup이 HUD를 숨기고 있습니다.");
        }

        private Entry FindEntry(UnitHUD hud)
        {
            foreach (Entry entry in _entries)
                if (entry.Hud == hud) return entry;
            throw new InvalidOperationException("[UnitHUDLayer] 준비할 HUD가 등록되지 않았습니다.");
        }

        private void PositionPreparation(Entry entry)
        {
            entry.Visible = false;
            entry.Rect.localPosition = new Vector3(_rect.rect.center.x, _rect.rect.center.y, 0f);
            entry.Rect.localScale = entry.BaseLocalScale;
            entry.Group.alpha = entry.RenderPreparation ? 1f : 0f;
            entry.Group.interactable = entry.Group.blocksRaycasts = false;
        }

        /// <summary>조립이 끝난 Overlay Canvas 아래에서 실제 출력 Camera를 연결합니다.</summary>
        internal void Configure(Camera outputCamera)
        {
            ThrowIfReleased();
            if (outputCamera == null) throw new ArgumentNullException(nameof(outputCamera));
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("[UnitHUDLayer] Screen Space Overlay Canvas 아래에 연결해야 합니다.");
            _rect = (RectTransform)transform;
            _canvas = canvas.rootCanvas;
            SetOutputCamera(outputCamera);
        }

        /// <summary>Virtual Camera가 아니라 최종 화면을 그리는 Camera를 교체합니다. null이면 숨깁니다.</summary>
        internal void SetOutputCamera(Camera outputCamera)
        {
            ThrowIfReleased();
            _outputCamera = outputCamera;
            // 다음 렌더 전까지 이전 Camera 위치에 입력 가능한 HUD를 남기지 않는다.
            HideAll();
            ReleaseSelectedInfo();
        }

        /// <summary>생성된 HUD의 기준 배율을 보관합니다. 등록 중 위치·줌 배율·루트 CanvasGroup을 제어하고 해제 때 기준 배율을 복구합니다.</summary>
        internal void Register(UnitHUD hud, IUnitHUDAnchor anchor)
        {
            ThrowIfReleased();
            if (_canvas == null) throw new InvalidOperationException("[UnitHUDLayer] Configure 뒤 등록해야 합니다.");
            if (hud == null) throw new ArgumentNullException(nameof(hud));
            if (anchor == null || !IsAlive(anchor))
                throw new ArgumentNullException(nameof(anchor));
            if (hud.transform.parent != transform)
                throw new InvalidOperationException("[UnitHUDLayer] HUD는 Layer의 직접 자식이어야 합니다.");
            hud.ValidateReferences();
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Hud == hud)
                    throw new InvalidOperationException("[UnitHUDLayer] 같은 HUD를 중복 등록할 수 없습니다.");

            var rect = (RectTransform)hud.transform;
            Vector3 baseLocalScale = rect.localScale;
            if (!UnitHUDProjection.IsValidBaseScale(baseLocalScale))
                throw new InvalidOperationException($"[UnitHUDLayer] UIId.UnitHUDLayer, '{hud.name}': 등록할 HUD localScale은 유한한 양수여야 합니다.");
            if (!(_canvas.scaleFactor > 0f) || float.IsInfinity(_canvas.scaleFactor))
                throw new InvalidOperationException("[UnitHUDLayer] UIId.UnitHUDLayer: Canvas.scaleFactor는 유한한 양수여야 합니다.");
            AddInstance(hud);
            CanvasGroup group = hud.GetComponent<CanvasGroup>();
            if (group == null) group = hud.gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = false;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            hud.ClearStatusHover();
            _entries.Add(new Entry
            {
                Hud = hud,
                Rect = rect,
                Group = group,
                Anchor = anchor,
                BaseLocalScale = baseLocalScale
            });
            // 새 HUD가 마지막 자식으로 생성되어도 기존 선택 HUD의 우선순위를 즉시 복구한다.
            RefreshSelectedInfo();
        }

        /// <summary>HUD를 숨기고 등록 때의 배율을 복구합니다. Clear·풀 반환·파괴는 소유자가 수행합니다.</summary>
        internal void Unregister(UnitHUD hud)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                if (!ReferenceEquals(entry.Hud, hud)) continue;
                SetVisible(entry, false);
                if (entry.Rect != null) entry.Rect.localScale = entry.BaseLocalScale;
                RemoveInstance(entry.Hud);
                _entries.RemoveAt(i);
                RefreshSelectedInfo();
                return;
            }
        }

        protected override void OnUIOpened()
        {
            if (_released || _subscribed) return;
            // 일반 PlayerLoop에서는 Brain의 LateUpdate 뒤, UI Graphic 갱신 전에 투영한다.
            Canvas.preWillRenderCanvases += RefreshPositions;
            _subscribed = true;
        }

        protected override void OnUIHidden()
        {
            Unsubscribe();
            HideAll();
            // 층과 함께 꺼진 HUD는 표시 상태를 지우므로 기준점에 알려 HUD에 쓴 내용의 기억을 버리게 한다(GC 끊김 계획 §3.7 L1).
            for (int i = 0; i < _entries.Count; i++)
                if (IsAlive(_entries[i].Anchor)) _entries[i].Anchor.OnHudLayerHidden();
            // UISystem.HideAll의 등록부 순회 중에도 호출된다. 등록 해제는 소유자의 선택/반환/Release 경계에서 한다.
            if (_selectedInfo != null) _ui.CloseUI(_selectedInfo.ID);
            _selectedInfoShown = false;
        }

        /// <summary>렌더 직전 출력 Camera와 기준점으로 위치·비례 배율을 갱신합니다. 반복 호출해도 연출을 초기화하거나 배율을 누적하지 않습니다.</summary>
        internal void RefreshPositions()
        {
            if (_released || !isActiveAndEnabled || _canvas == null) return;
            Camera camera = _outputCamera;
            bool canProject = camera != null && camera.isActiveAndEnabled && _canvas.isActiveAndEnabled &&
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay && camera.targetDisplay == _canvas.targetDisplay;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                if (entry.Hud == null)
                {
                    RemoveInstance(entry.Hud);
                    _entries.RemoveAt(i);
                    continue;
                }

                if (entry.Preparing)
                {
                    PositionPreparation(entry);
                    continue;
                }

                bool anchorAlive = IsAlive(entry.Anchor);
                bool visible = canProject && anchorAlive && entry.Anchor.IsHudVisible && entry.Hud.isActiveAndEnabled;
                if (visible)
                {
                    visible = Project(entry.Rect, entry.Anchor.HudWorldPosition + entry.Hud.AdditionalWorldOffset, entry.BaseLocalScale);
                }
                SetVisible(entry, visible);
            }
            RefreshSelectedInfo();
            if (_selectedHover != null) _selectedHover.RefreshPosition();
            foreach (ProjectionLease item in _widgets)
                if (item.Rect != null)
                    item.Group.alpha = item.Rect.gameObject.activeInHierarchy && Project(item.Rect, item.Position, item.BaseScale) ? 1f : 0f;
        }

        private static void SetVisible(Entry entry, bool visible)
        {
            if (entry.Visible == visible) return;
            entry.Visible = visible;
            if (entry.Group != null)
            {
                entry.Group.alpha = visible ? 1f : 0f;
                entry.Group.interactable = visible;
                entry.Group.blocksRaycasts = visible;
            }
            if (!visible && entry.Hud != null) entry.Hud.ClearStatusHover();
        }

        // 기준점이 파괴된 Unity 오브젝트가 아닌지입니다. null 검사는 부르는 쪽이 합니다.
        private static bool IsAlive(IUnitHUDAnchor anchor) => !(anchor is UnityEngine.Object unityAnchor) || unityAnchor != null;

        private void HideAll()
        {
            for (int i = 0; i < _entries.Count; i++) SetVisible(_entries[i], false);
            foreach (ProjectionLease item in _widgets)
                if (item.Group != null) item.Group.alpha = 0f;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            Canvas.preWillRenderCanvases -= RefreshPositions;
            _subscribed = false;
        }

        /// <summary>렌더 콜백과 등록 참조를 해제합니다. 반복 호출해도 안전하며 이후 재사용하지 않습니다.</summary>
        internal void Release()
        {
            if (_released) return;
            _selectedEntity = EntityId.Empty;
            _selectedHud = null;
            foreach (ProjectionLease item in _widgets.ToArray()) item.Dispose();
            ReleaseSelectedInfo();
            _runtimeRegistry?.UnregisterRuntimeUI(this);
            _runtimeRegistry = null;
            _ui = null;
            while (_entries.Count > 0) Unregister(_entries[_entries.Count - 1].Hud);
            Hide();
            _released = true;
            Unsubscribe();
            HideAll();
            _entries.Clear();
            _outputCamera = null;
            _canvas = null;
            _rect = null;
        }

        private void OnDestroy() => Release();

        private void ThrowIfReleased()
        {
            if (_released) throw new InvalidOperationException("[UnitHUDLayer] 해제한 Layer는 다시 사용할 수 없습니다.");
        }
    }
}
