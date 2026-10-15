using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ProjectL.Tests.Framework.DataBaseTestAssets;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.Framework
{
    /// <summary>아티팩트 팝업 UI가 현강 UI 구조(UIBase·UIInputBinding·UISystem 이력)로 열고 닫히는지 확인합니다(UI 이전 설계 §4·§8).</summary>
    public sealed class ArtifactPopupUITests
    {
        private readonly List<GameObject> _objects = new();
        private ArtifactPopupUI _ui;
        private IUISystemService _system;
        private HostSpy _host;
        private BoardStub _board;
        private Button _open, _close;
        private GameObject _panel;
        private RectTransform _pool;
        private TMP_Text _status, _name, _description;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardStub(Snapshot());
            _host = new HostSpy();
            _ui = CreateUI();
            _system = CreateSystem();
            var registry = (IUIRegistry)_system;
            registry.AddUI(_ui.ID, _ui);
            ((ISystemPostBindService)_system).PostBindService(new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
            ((ISystemInitialize)_system).Initialize();
            Set(_ui, "_ui", _system);
            Set(_ui, "_board", _board);
            Set(_ui, "_views", new ViewsFake());
            PopupScreen.Connect(_host);
        }

        [TearDown]
        public void TearDown()
        {
            ((ISystemShutdown)_system).Shutdown();
            foreach (GameObject obj in _objects) if (obj != null) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        private IArtifactPopupScreen PopupScreen => _ui;

        [Test]
        public void Initialize_HidesPanelOnly_KeepsOpenButton_AndDoesNotNotifyHost()
        {
            Assert.That(_ui.ID, Is.EqualTo(UIId.Artifact));
            Assert.That(_ui.gameObject.activeSelf, Is.True, "Hide가 루트를 끄면 열기 버튼도 사라집니다.");
            Assert.That(_open.gameObject.activeInHierarchy, Is.True);
            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_host.Opened + _host.Hidden, Is.Zero);
        }

        [Test]
        public void Buttons_UseUIInputBinding_WithoutInspectorOnClick()
        {
            Assert.That(_open.GetComponent<UIInputBinding>(), Is.Not.Null);
            Assert.That(_close.GetComponent<UIInputBinding>(), Is.Not.Null);
            Assert.That(_open.onClick.GetPersistentEventCount(), Is.Zero);
            Assert.That(_close.onClick.GetPersistentEventCount(), Is.Zero);
        }

        [Test]
        public void OpenButton_OpensThroughUISystem_DrawsPool_AndNotifiesHostOnce()
        {
            Click(_open);

            Assert.That(_ui.IsOpen, Is.True);
            Assert.That(_panel.activeSelf, Is.True);
            Assert.That(_host.Opened, Is.EqualTo(1));
            Assert.That(_ui.VisibleSlotCount, Is.EqualTo(2));

            Click(_open);
            Assert.That(_host.Opened, Is.EqualTo(1), "열린 동안 열기 버튼은 다시 열지 않습니다.");
        }

        [Test]
        public void OpenButton_WhenHostCannotOpen_DoesNothing()
        {
            _host.CanOpen = false;

            Click(_open);

            Assert.That(_ui.IsOpen, Is.False);
            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_host.Opened, Is.Zero);
        }

        [Test]
        public void CloseButton_BackCloseUI_AndHideAllUI_AllHideAndNotifyHost()
        {
            Click(_open);
            Click(_close);
            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_host.Hidden, Is.EqualTo(1));

            Click(_open);
            _system.BackCloseUI();
            Assert.That(_ui.IsOpen, Is.False, "UISystem 뒤로가기도 같은 숨김 경로를 탑니다.");
            Assert.That(_host.Hidden, Is.EqualTo(2));

            Click(_open);
            _system.HideAllUI();
            Assert.That(_ui.IsOpen, Is.False);
            Assert.That(_host.Hidden, Is.EqualTo(3));

            _system.BackCloseUI();
            Assert.That(_host.Hidden, Is.EqualTo(3), "닫힌 뒤의 뒤로가기는 다시 알리지 않습니다.");
        }

        [Test]
        public void Disconnect_StopsHostNotification()
        {
            Click(_open);
            PopupScreen.Disconnect();

            _system.CloseUI(UIId.Artifact);

            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_host.Hidden, Is.Zero);
        }

        [Test]
        public void SlotHover_ShowsDetailFromViewDatabase_AndHideClearsTexts()
        {
            Click(_open);
            Button slot = _pool.GetChild(0).GetComponent<Button>();

            ExecuteEvents.Execute(slot.gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);

            Assert.That(_name.text, Is.EqualTo("회복 성배"));
            Assert.That(_description.text, Is.EqualTo("설명 5"));
            PopupScreen.ShowStatus("상태");
            Assert.That(_status.text, Is.EqualTo("상태"));

            Click(_close);
            Assert.That(_name.text, Is.Empty);
            Assert.That(_status.text, Is.Empty);
        }

        [Test]
        public void ScreenQueries_PickSlot_ReturnArea_AndBlockingArea()
        {
            Click(_open);
            Vector2 slotPoint = _pool.GetChild(0).position;

            Assert.That(PopupScreen.TryPickPool(slotPoint, out int artifactId), Is.True);
            Assert.That(artifactId, Is.EqualTo(5));
            Assert.That(PopupScreen.IsReturnArea(_pool.position), Is.True);
            Assert.That(PopupScreen.IsBlockingArea(_close.transform.position), Is.True);
            Assert.That(PopupScreen.IsBlockingArea(_name.transform.position), Is.True);
            Assert.That(PopupScreen.TryPickPool(new Vector2(-900, -900), out _), Is.False);
            Assert.That(PopupScreen.IsBlockingArea(new Vector2(-900, -900)), Is.False);
        }

        [Test]
        public void Reopen_RedrawsChangedPool_AndReusesSlots()
        {
            Click(_open);
            Click(_close);
            _board.Current = new ArtifactBufferSnapshot(new ArtifactState(4, 4,
                new[] { new ArtifactItemState(5, 1, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) },
                Array.Empty<ArtifactItemState>()));

            Click(_open);

            Assert.That(_ui.VisibleSlotCount, Is.EqualTo(1));
            Assert.That(_pool.childCount, Is.EqualTo(2), "줄어든 목록은 슬롯을 숨겨 재사용합니다.");
        }

        [Test]
        public void Validate_RequiresSeparatePanel_AndOpenButtonOutsidePanel()
        {
            Assert.DoesNotThrow(_ui.Validate);
            _open.transform.SetParent(_panel.transform, false);
            Assert.Throws<InvalidOperationException>(_ui.Validate);
        }

        /// <summary>EditMode 배치 실행에서는 Awake가 호출되지 않으므로 Play와 같은 초기 연결을 직접 실행합니다.</summary>
        internal static void InvokeAwake(ArtifactPopupUI ui)
        {
            typeof(ArtifactPopupUI).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(ui, null);
        }

        private static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }

        private static ArtifactBufferSnapshot Snapshot() => new(new ArtifactState(4, 4,
            new[]
            {
                new ArtifactItemState(5, 1, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }),
                new ArtifactItemState(6, 2, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero, new CellCoord(1, 0) }),
            },
            Array.Empty<ArtifactItemState>()));

        private static IUISystemService CreateSystem()
        {
            Type type = typeof(IUISystemService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.UISystem", true);
            return (IUISystemService)Activator.CreateInstance(type, true);
        }

        private ArtifactPopupUI CreateUI()
        {
            var root = Create("ArtifactPopupUI", null, Vector2.zero, new Vector2(1000, 1000));
            root.SetActive(false);
            _open = Create("Open", root.transform, new Vector2(-400, 400), new Vector2(50, 50)).AddComponent<Button>();
            _panel = Create("Panel", root.transform, Vector2.zero, new Vector2(1000, 1000));
            _close = Create("Close", _panel.transform, new Vector2(400, 400), new Vector2(50, 50)).AddComponent<Button>();
            _pool = (RectTransform)Create("Pool", _panel.transform, new Vector2(0, -300), new Vector2(400, 100)).transform;
            _status = Create("Status", _panel.transform, new Vector2(300, -100), new Vector2(100, 30)).AddComponent<TextMeshProUGUI>();
            _name = Create("Name", _panel.transform, new Vector2(300, 0), new Vector2(100, 30)).AddComponent<TextMeshProUGUI>();
            _description = Create("Description", _panel.transform, new Vector2(300, 100), new Vector2(100, 30)).AddComponent<TextMeshProUGUI>();
            var prefabs = Create("Prefabs", null, Vector2.zero, Vector2.one);
            prefabs.SetActive(false);
            var slot = Create("Slot", prefabs.transform, Vector2.zero, new Vector2(80, 80));
            slot.AddComponent<Image>();
            var slotButton = slot.AddComponent<Button>();

            var ui = root.AddComponent<ArtifactPopupUI>();
            Set(ui, "openButton", _open);
            Set(ui, "panel", _panel);
            Set(ui, "closeButton", _close);
            Set(ui, "poolArea", _pool);
            Set(ui, "slotPrefab", slotButton);
            Set(ui, "statusText", _status);
            Set(ui, "artifactName", _name);
            Set(ui, "effectDescription", _description);
            root.SetActive(true);
            InvokeAwake(ui);
            return ui;
        }

        private GameObject Create(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            if (parent != null) rect.SetParent(parent, false); else _objects.Add(obj);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return obj;
        }

        private sealed class HostSpy : IArtifactPopupHost
        {
            public bool CanOpen { get; set; } = true;
            public int Opened;
            public int Hidden;
            public void PopupOpened() => Opened++;
            public void PopupHidden() => Hidden++;
        }

        private sealed class BoardStub : IArtifactBufferReadSink
        {
            public BoardStub(ArtifactBufferSnapshot current) => Current = current;
            public ArtifactBufferSnapshot Current { get; set; }
        }

        private sealed class ViewsFake : IArtifactViewTableQuery
        {
            public ArtifactViewData GetArtifactView(int artifactId) =>
                TryGetArtifactView(artifactId, out var data) ? data : throw new KeyNotFoundException();

            public bool TryGetArtifactView(int artifactId, out ArtifactViewData data)
            {
                data = new ArtifactViewData();
                Set(data, "artifactId", artifactId);
                Set(data, "displayName", artifactId == 5 ? "회복 성배" : $"아티팩트 {artifactId}");
                Set(data, "description", $"설명 {artifactId}");
                return true;
            }

            public IReadOnlyCollection<ArtifactViewData> GetAllArtifactViews() => Array.Empty<ArtifactViewData>();
        }
    }
}
