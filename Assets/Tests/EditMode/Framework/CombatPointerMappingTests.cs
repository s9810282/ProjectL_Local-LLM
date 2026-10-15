using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectL.Tests.Framework
{
    /// <summary>V1: 실제 씬 좌표·UI Raycast와 입력 데이터의 EventSystem 수명을 확인합니다.</summary>
    public sealed class CombatPointerMappingTests
    {
        private readonly List<GameObject> _objects = new();
        private EventSystem[] _previousSystems;
        private EventSystem _previousCurrent;
        private Scene _scene;
        private CombatSceneRoot _root;
        private CombatSceneDisplay _display;
        private ColorSystem _colors;
        private EventSystem _eventSystem;
        private CombatPointerProbeRaycaster _probe;
        private Vector2 _screen;
        private CombatDisplayValues _values;

        [SetUp]
        public void SetUp()
        {
            _display = null;
            _colors = null;
            _scene = default;
            _previousSystems = null;
            _previousCurrent = null;
            if (SceneManager.GetSceneByPath("Assets/Scenes/Combat.unity").isLoaded)
                Assert.Ignore("Combat 씬이 이미 열려 있습니다. 사용자 씬을 닫지 않도록 다른 씬에서 실행합니다.");

            _previousCurrent = EventSystem.current;
            _previousSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(system => system.enabled).ToArray();
            foreach (EventSystem system in _previousSystems)
                system.enabled = false;

            _scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
            _root = _scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<CombatSceneRoot>(true)).Single();
            _eventSystem = _scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<EventSystem>(true)).Single();
            EventSystem.current = _eventSystem;
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(
                AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
            _colors = new ColorSystem();
            _display = new CombatSceneDisplay(_root, database.ViewQuery.StageFieldViewTableQuery.Config,
                database.ViewQuery.UnitVisualTableQuery, database.ViewQuery.StageViewPropTableQuery, _colors,
                database.ViewQuery.TraitVisualTableQuery, new CombatViewSystemTests.FakeAreaOutlines());
            var buffers = CombatPreviewPublicationTests.Create(out _);
            _values = new CombatDisplayValues(buffers.CombatStateReadSink.Current, buffers.CharacterReadSink.Characters);
            _display.Install(_values);
            _screen = _root.WorldCamera.WorldToScreenPoint(_root.Field.Mapper.CellCenterToWorld(new CellCoord(0, 0)));
            _probe = NewObject("Non-UI raycast probe").AddComponent<CombatPointerProbeRaycaster>();
        }

        [TearDown]
        public void TearDown()
        {
            try { _display?.Release(); }
            finally
            {
                _colors?.Shutdown();
                foreach (GameObject item in _objects)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                _objects.Clear();
                if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
                if (_previousSystems != null)
                    foreach (EventSystem system in _previousSystems)
                        if (system != null) system.enabled = true;
                if (_previousCurrent != null)
                    EventSystem.current = _previousCurrent;
            }
        }

        [Test]
        public void DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange()
        {
            var buffers = CombatPreviewPublicationTests.Create(out var service);
            var actor = buffers.CharacterReadSink.Characters[0];
            var origin = actor.Anchor.Value;
            var endpoint = origin + CellCoord.Up;
            var path = new MovementPath(new[] { origin, endpoint });
            _root.Field.SetLayerCells(FieldHighlightLayer.Range, path.Cells);

            var retained = new MovementPath(new[] { origin });
            _display.ShowDraft(retained, null, true, endpoint);
            Assert.That(_root.Field.HighlightState.HoverState, Is.EqualTo(CombatHoverState.Neutral));
            Assert.That(ShownColor(origin), Is.EqualTo(FieldValue<Color>("movementDraftColor")));
            Assert.That(ShownColor(endpoint), Is.EqualTo(FieldValue<Color>("hoverColor")));
            Assert.That(Layers().Count(
                layer => (layer & FieldHighlightLayer.MovementDraft) != 0), Is.EqualTo(1));
            _display.ShowDraft(retained, null, false, endpoint);
            Assert.That(ShownColor(endpoint), Is.EqualTo(FieldValue<Color>("hoverInvalidColor")));
            Assert.That(Layers().Count(
                layer => (layer & FieldHighlightLayer.MovementDraft) != 0), Is.EqualTo(1), "거절 칸은 경로 층에 포함하지 않는다.");

            buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor.EntityId, path));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Move.Value.CanMove, Is.True);
            _display.ShowDraft(path, preview.Move, true);
            Assert.That(ShownColor(endpoint), Is.EqualTo(FieldValue<Color>("hoverValidColor")));
            _display.ShowDraft(path, preview.Move, false);
            Assert.That(ShownColor(endpoint), Is.EqualTo(FieldValue<Color>("hoverInvalidColor")));

            // 경로를 되돌리면 이전 끝점의 초안/호버가 지워지고 원래 범위색만 남는다.
            _display.ShowDraft(new MovementPath(new[] { origin }), null, true);
            Assert.That(ShownColor(endpoint), Is.EqualTo(FieldValue<Color>("rangeColor")));
            _display.ClearDraft();
            _display.ClearDraft();
            Assert.That(ShownColor(origin), Is.EqualTo(FieldValue<Color>("rangeColor")));
            Assert.That(Layers().Any(
                layer => (layer & FieldHighlightLayer.MovementDraft) != 0), Is.False);
            Assert.That(_root.Field.HighlightState.HoverIndex, Is.EqualTo(-1));
        }

        private T FieldValue<T>(string name) => (T)typeof(FieldView)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_root.Field);

        private IEnumerable<FieldHighlightLayer> Layers()
        {
            IFieldHighlightStateReader state = _root.Field.HighlightState;
            return Enumerable.Range(0, state.CellCount).Select(state.LayersAt);
        }

        private Color ShownColor(CellCoord cell)
        {
            var renderer = (FieldCellColorRenderer)FieldValue<IFieldHighlightRenderer>("_renderer");
            return (Color)typeof(FieldCellHandle).GetField("_highlight", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(renderer.CellAt(cell.Y * _root.Field.Mapper.ColumnCount + cell.X));
        }

        [Test]
        public void SameEventSystem_ReusesData_AndStillRaycastsEveryPosition()
        {
            Assert.That(_display.TryMapPointer(_screen, out _, out _), Is.True);
            PointerEventData first = _probe.LastData;
            first.Use();
            first.displayIndex = 4;
            Vector2 moved = _screen + new Vector2(3, 2);

            _display.TryMapPointer(moved, out _, out _);

            Assert.That(_probe.LastData, Is.SameAs(first));
            Assert.That(_probe.Calls, Is.EqualTo(2));
            Assert.That(_probe.LastData.position, Is.EqualTo(moved));
            Assert.That(_probe.LastData.displayIndex, Is.Zero);
            Assert.That(_probe.LastData.used, Is.False);
        }

        [Test]
        public void UiAndNonUiHits_OnlyGraphicsCancel_AndSamePositionIsRechecked()
        {
            Assert.That(_display.TryMapPointer(_screen, out _, out bool noUi), Is.True);
            Assert.That(noUi, Is.False, "비UI hit만으로는 취소하지 않는다.");
            GameObject canvas = NewObject("Pointer UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject image = NewObject("Blocking UI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            image.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = _screen;
            rect.sizeDelta = new Vector2(80, 80);
            Canvas.ForceUpdateCanvases();

            Assert.That(_display.TryMapPointer(_screen, out _, out bool onUi), Is.True);
            Assert.That(onUi, Is.True, "앞선 비UI hit 뒤의 실제 GraphicRaycaster hit도 검사한다.");
            image.SetActive(false);
            Assert.That(_display.TryMapPointer(_screen, out _, out bool uiRemoved), Is.True);
            Assert.That(uiRemoved, Is.False, "포인터가 같아도 UI 변경을 다시 검사한다.");
            Assert.That(Hits, Is.Empty, "Raycast 결과의 씬 참조를 호출 뒤 보관하지 않는다.");
        }

        [Test]
        public void EventSystemReplacement_RecreatesDataBoundToNewSystem()
        {
            _display.TryMapPointer(_screen, out _, out _);
            PointerEventData first = _probe.LastData;
            EventSystem replacement = NewObject("Replacement EventSystem").AddComponent<EventSystem>();
            EventSystem.current = replacement;
            GameObject selected = NewObject("Selected in replacement");
            replacement.SetSelectedGameObject(selected);

            _display.TryMapPointer(_screen, out _, out _);

            Assert.That(_probe.LastData, Is.Not.SameAs(first));
            Assert.That(_probe.LastData.selectedObject, Is.SameAs(selected));
            Assert.That(_probe.Calls, Is.EqualTo(2));
        }

        [Test]
        public void MissingOrDestroyedEventSystem_DropsCache_AndKeepsFieldMapping()
        {
            _display.TryMapPointer(_screen, out _, out _);
            PointerEventData first = _probe.LastData;
            _eventSystem.enabled = false;
            Assert.That(EventSystem.current, Is.Null);

            Assert.That(_display.TryMapPointer(_screen, out _, out bool cancelled), Is.True);
            Assert.That(cancelled, Is.False);
            Assert.That(CachedData, Is.Null);
            Assert.That(Hits, Is.Empty);
            Assert.That(_probe.Calls, Is.EqualTo(1));

            _eventSystem.enabled = true;
            _display.TryMapPointer(_screen, out _, out _);
            Assert.That(_probe.LastData, Is.Not.SameAs(first));
            UnityEngine.Object.DestroyImmediate(_eventSystem.gameObject);
            Assert.That(_display.TryMapPointer(_screen, out _, out _), Is.True);
            Assert.That(CachedData, Is.Null);
        }

        [Test]
        public void SamePosition_UsesCurrentCamera_AndKeepsHandAndOutsideBoardMeaning()
        {
            Assert.That(_display.TryMapPointer(_screen, out Vector2 before, out _), Is.True);
            _root.WorldCamera.transform.position += _root.Field.transform.right * .1f;
            _display.TryMapPointer(_screen, out Vector2 after, out _);
            Assert.That(Vector2.Distance(before, after), Is.GreaterThan(.001f));
            _eventSystem.enabled = false;
            // 손패는 화면 공간 카드 영역이다(손패 카드화 H2). 이 결과는 전투 단계라 영역이 꺼져 있으므로 보이는 상태를 만든다.
            var hand = new SerializedObject(_root.Hand);
            var cardRoot = (RectTransform)hand.FindProperty("cardRoot").objectReferenceValue;
            cardRoot.gameObject.SetActive(true);
            Canvas canvas = cardRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 handScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, cardRoot.TransformPoint(cardRoot.rect.center));
            _display.TryMapPointer(handScreen, out _, out bool handCancel);
            Assert.That(handCancel, Is.True, "EventSystem이 없어도 손패 취소 영역은 유지한다.");
            Vector3 outside = _root.Field.Mapper.CellCenterToWorld(new CellCoord(0, 0)) + _root.Field.transform.right * 100;
            Assert.That(_display.TryMapPointer(_root.WorldCamera.WorldToScreenPoint(outside), out _, out _), Is.False);
        }

        [Test]
        public void RaycastFailure_ClearsHits_AndNextCallCanReuseData()
        {
            _probe.ThrowAfterHit = true;
            Assert.Throws<InvalidOperationException>(() => _display.TryMapPointer(_screen, out _, out _));
            PointerEventData first = _probe.LastData;
            Assert.That(Hits, Is.Empty);
            _probe.ThrowAfterHit = false;
            Assert.That(_display.TryMapPointer(_screen, out _, out _), Is.True);
            Assert.That(_probe.LastData, Is.SameAs(first));
        }

        [Test]
        public void Release_ClearsReferences_AndLateCallDoesNotRaycastOrRecreateData()
        {
            _display.TryMapPointer(_screen, out _, out _);
            _display.Release();
            _display.Release();

            Assert.That(_display.TryMapPointer(_screen, out _, out bool cancelled), Is.False);
            Assert.That(cancelled, Is.False);
            Assert.That(CachedData, Is.Null);
            Assert.That(ReadField<EventSystem>("_pointerEventSystem"), Is.Null);
            Assert.That(Hits, Is.Empty);
            Assert.That(_probe.Calls, Is.EqualTo(1));
        }

        [Test]
        public void WarmMapping_DoesNotCreateMorePointerEventData()
        {
            for (int i = 0; i < 32; i++) _display.TryMapPointer(_screen, out _, out _);
            int calls = _probe.Calls;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 512; i++) _display.TryMapPointer(_screen, out _, out _);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(_probe.Calls - calls, Is.EqualTo(512));
            Assert.That(_probe.DataInstances, Is.EqualTo(1));
            TestContext.WriteLine($"V1 warm mapping: 512 calls, {allocated} managed bytes including Unity raycasters; PointerEventData instances=1 across 544 calls.");
        }

        // 구조 개선 VR4: PointerMapper 수명(Bind·Clear/재설치·부분 실패·Release)과 해제 뒤 표시 호출.
        [Test]
        public void Clear_UnbindsMapping_DropsUiCache_AndReinstallMapsAgain()
        {
            Assert.That(_display.TryPickCell(_screen.x, _screen.y, out CellCoord before), Is.True);
            Assert.That(before, Is.EqualTo(new CellCoord(0, 0)));
            _display.TryMapPointer(_screen, out _, out _);
            Assert.That(CachedData, Is.Not.Null);

            _display.Clear();
            Assert.That(_display.TryPickCell(_screen.x, _screen.y, out _), Is.False, "정리된 전투의 격자로 집지 않는다.");
            Assert.That(_display.TryMapPointer(_screen, out _, out _), Is.False);
            Assert.That(CachedData, Is.Null);

            _display.Install(_values);
            Assert.That(_display.TryPickCell(_screen.x, _screen.y, out CellCoord after), Is.True);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void BuildFailure_UnbindsMapping_EvenWhenOldFieldStillExists()
        {
            // 기존 필드는 살아 있지만 다음 Build의 루트 검증이 실패하는 경로(SceneEventPresenterTests와 같은 방법).
            var invalidState = (ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateData)
                typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_values.State, null);
            typeof(ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateData)
                .GetField("<BoardWidth>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(invalidState, _values.State.BoardWidth + 1);
            var invalid = new CombatDisplayValues(invalidState, _values.Characters);
            var cellsRoot = (Transform)new SerializedObject(_root.Field).FindProperty("cellsRoot").objectReferenceValue;
            cellsRoot.localScale = Vector3.one * 2f;
            try
            {
                Assert.Throws<InvalidOperationException>(() => _display.Install(invalid));
                Assert.That(_display.TryPickCell(_screen.x, _screen.y, out _), Is.False, "설치 실패 뒤 이전 격자로 집지 않는다.");
            }
            finally
            {
                cellsRoot.localScale = Vector3.one;
            }

            _display.Clear();
            _display.Install(_values);
            Assert.That(_display.TryPickCell(_screen.x, _screen.y, out _), Is.True);
        }

        [Test]
        public void AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed()
        {
            var cell = new CellCoord(0, 0);
            _display.Release();

            Assert.DoesNotThrow(() => _display.ShowDraft(new MovementPath(new[] { cell }), null, true));
            Assert.DoesNotThrow(() => _display.ClearDraft());
            Assert.DoesNotThrow(() => _display.SetReturnState(null));
            Assert.DoesNotThrow(() => _display.BindReturn(() => { }));
            Assert.DoesNotThrow(() => _display.BindReturn(null), "정리 콜백(연결 해제)은 해제 뒤에도 실행된다.");
            Assert.That(_display.TryPickCell(_screen.x, _screen.y, out _), Is.False);
        }

        private PointerEventData CachedData => ReadField<PointerEventData>("_pointerEventData");
        private List<RaycastResult> Hits => ReadField<List<RaycastResult>>("_uiHits");
        // VR4: UI Raycast 재사용 데이터는 SceneDisplay 내부의 CombatPointerMapper가 소유한다.
        private T ReadField<T>(string name)
        {
            object pointer = typeof(CombatSceneDisplay)
                .GetField("_pointer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_display);
            return (T)typeof(CombatPointerMapper)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pointer);
        }

        private GameObject NewObject(string name, params Type[] components)
        {
            var item = new GameObject(name, components);
            _objects.Add(item);
            SceneManager.MoveGameObjectToScene(item, _scene);
            return item;
        }
    }

    public sealed class CombatPointerProbeRaycaster : BaseRaycaster
    {
        public int Calls { get; private set; }
        public int DataInstances { get; private set; }
        public PointerEventData LastData { get; private set; }
        public bool ThrowAfterHit { get; set; }
        public override Camera eventCamera => null;
        public override int sortOrderPriority => int.MaxValue;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            Calls++;
            if (!ReferenceEquals(LastData, eventData)) DataInstances++;
            LastData = eventData;
            resultAppendList.Add(new RaycastResult { gameObject = gameObject, module = this, distance = 0 });
            if (ThrowAfterHit) throw new InvalidOperationException("V1 raycast failure probe");
        }
    }
}
