using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>실제 덱 프리팹과 UISystem 경로에서 공용 상세창 표시·입력·수명을 확인합니다.</summary>
    public sealed class DeckSlotInfoPanelTests
    {
        private static readonly TraitKey[] Keys =
            { TraitKey.DoubleShot, TraitKey.LuckyBullet, TraitKey.ChainExplosion, TraitKey.ImpactBolt, TraitKey.Vanguard };
        private GameObject _canvas;
        private GameObject _eventsObject;
        private DeckEditUI _view;
        private DeckSlotInfoUI _info;
        private UISystem _ui;
        private EventSystem _previousEvents;
        private Sprite _icon;

        [SetUp]
        public void SetUp()
        {
            _previousEvents = EventSystem.current;
            _icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            _eventsObject = new GameObject("상세창 시험 입력", typeof(DeckMainPolicyTestEventSystem));
            EventSystem.current = _eventsObject.GetComponent<EventSystem>();
            _canvas = new GameObject("상세창 시험 Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(1920f, 1080f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/Deck/DeckEditUI.prefab");
            _view = Object.Instantiate(prefab, _canvas.transform).GetComponent<DeckEditUI>();
            _info = _view.GetComponentInChildren<DeckSlotInfoUI>(true);
            _view.BindInput(new Input());
            _ui = new UISystem();
            _ui.AddUI(UIId.StageSelect, new Parent());
            _view.ExportUIService(_ui);
            _info.ExportUIService(_ui);
            _view.BindUISystemService(_ui);
            _info.BindUISystemService(_ui);
            _ui.PostBindService(new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
            _ui.Initialize();
            _ui.OpenUI(UIId.StageSelect);
            _view.Render(Model());
            _ui.OpenUI(UIId.DeckEdit);
        }

        [TearDown]
        public void TearDown()
        {
            if (_ui != null) _ui.Shutdown();
            if (_canvas != null) Object.DestroyImmediate(_canvas);
            if (_eventsObject != null) Object.DestroyImmediate(_eventsObject);
            if (_icon != null) Object.DestroyImmediate(_icon);
            if (_previousEvents != null) EventSystem.current = _previousEvents;
        }

        [Test]
        public void Stats_UseDedicatedFieldsAndPendingExperience()
        {
            _info.Render(Card(1, 0), "시험 유닛", _icon, null);
            Assert.That(Text("stats.category"), Is.EqualTo("Heavy"));
            Assert.That(Text("stats.unitName"), Is.EqualTo("시험 유닛"));
            Assert.That(Ref<Image>("stats.portrait").sprite, Is.SameAs(_icon));
            Assert.That(Ref<Image>("stats.portrait").enabled, Is.True);
            Assert.That(Text("stats.level"), Is.EqualTo("7"));
            Assert.That(Text("stats.health"), Is.EqualTo("40"));
            Assert.That(Text("stats.attack"), Is.EqualTo("13"));
            Assert.That(Text("stats.defense"), Is.EqualTo("6"));
            Assert.That(Text("stats.moveDistance"), Is.EqualTo("3"));
            Assert.That(Text("stats.attackDistance"), Is.EqualTo("5"));
            Assert.That(Ref<Image>("experience.fill").fillAmount, Is.Zero);
            Assert.That(Text("experience.remaining"), Is.EqualTo("-"));
            Assert.That(Ref<Button>("changeButton").gameObject.activeSelf, Is.False);
            _info.Render(Card(2, 0), "다음 유닛", null, null);
            Assert.That(Text("stats.category"), Is.EqualTo("Heavy"));
            Assert.That(Text("stats.unitName"), Is.EqualTo("다음 유닛"));
            Assert.That(Ref<Image>("stats.portrait").sprite, Is.Null);
            Assert.That(Ref<Image>("stats.portrait").enabled, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Abilities_ShowAllContentsInExactlyOneArea(int count)
        {
            var query = new Traits(_icon);
            _info.Render(Card(1, count), "시험", null, query);
            GridLayoutGroup area = Area(count >= 3 ? 3 : 2);
            Assert.That(area.gameObject.activeSelf, Is.True);
            Assert.That(Area(count >= 3 ? 2 : 3).gameObject.activeSelf, Is.False);
            Assert.That(area.constraintCount, Is.EqualTo(count >= 3 ? 3 : 2));
            var shown = area.transform.Cast<Transform>().Where(t => t.gameObject.activeSelf).ToArray();
            Assert.That(shown.Length, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                Transform content = shown[i].Find("Content");
                Assert.That(TextComponent(content.Find("Name")), Is.EqualTo("특성 " + Keys[i]));
                Assert.That(TextComponent(content.Find("Description")), Is.EqualTo("설명 " + Keys[i]));
                Assert.That(content.Find("Icon").GetComponent<Image>().sprite, Is.EqualTo(query.GetTraitVisual(Keys[i]).Icon));
                Assert.That(shown[i].GetComponent<Button>(), Is.Null);
            }
            _info.Open();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)area.transform);
            var bounds = new Vector3[4];
            foreach (Transform slot in shown)
            {
                ((RectTransform)slot).GetWorldCorners(bounds);
                foreach (Vector3 point in bounds)
                {
                    Vector3 local = area.transform.InverseTransformPoint(point);
                    Rect rect = ((RectTransform)area.transform).rect;
                    Assert.That(local.x, Is.InRange(rect.xMin - .1f, rect.xMax + .1f));
                    Assert.That(local.y, Is.InRange(rect.yMin - .1f, rect.yMax + .1f));
                }
            }
        }

        [Test]
        public void Abilities_ManyToFewClearsHiddenDataAndReusesClones()
        {
            var query = new Traits(_icon);
            _info.Render(Card(1, 5), "많음", null, query);
            GridLayoutGroup three = Area(3);
            Transform extra = three.transform.GetChild(4);
            _info.Render(Card(2, 1), "적음", null, query);
            Assert.That(three.gameObject.activeSelf, Is.False);
            foreach (Transform slot in three.transform)
            {
                Assert.That(slot.gameObject.activeSelf, Is.False);
                Assert.That(TextComponent(slot.Find("Content/Name")), Is.Empty);
                Assert.That(TextComponent(slot.Find("Content/Description")), Is.Empty);
                Assert.That(slot.Find("Content/Icon").GetComponent<Image>().sprite, Is.Null);
            }
            _info.Render(Card(1, 5), "다시", null, query);
            Assert.That(three.transform.childCount, Is.EqualTo(5));
            Assert.That(three.transform.GetChild(4), Is.SameAs(extra));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedSelection_CloseClickAndSubmitUseUISystem(bool submit)
        {
            var first = DeckInfo();
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            Dispatch(first.gameObject, false);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Ref<Button>("closeButton").gameObject));
            var owned = _view.GetComponentsInChildren<OwnedUnitSlot>(true).Single(s => s.Card?.OwnedId == new CharacterInstanceId(2));
            ExecuteEvents.Execute(owned.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Assert.That(Text("stats.category"), Is.EqualTo("Heavy"));
            Assert.That(Text("stats.unitName"), Is.EqualTo("Unit_Akimbo"));
            Dispatch(owned.gameObject, false);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            Assert.That(Text("stats.level"), Is.EqualTo("8"));
            Dispatch(Ref<Button>("closeButton").gameObject, submit);
            Assert.That(_info.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(first.gameObject));
            Dispatch(owned.gameObject, true);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            Dispatch(owned.gameObject, true);
            Assert.That(_info.gameObject.activeSelf, Is.False);
            Dispatch(first.gameObject, false);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            _ui.CloseUI(UIId.DeckEdit);
            Assert.That(_info.gameObject.activeInHierarchy, Is.False);
            _ui.OpenUI(UIId.DeckEdit);
            Dispatch(first.gameObject, false);
            Assert.That(_info.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Close_IgnoresRightClickDisabledAndHiddenInput()
        {
            Dispatch(DeckInfo().gameObject, false);
            Button close = Ref<Button>("closeButton");
            close.interactable = false;
            Dispatch(close.gameObject, true);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            close.interactable = true;
            ExecuteEvents.Execute(close.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Right }, ExecuteEvents.pointerClickHandler);
            Assert.That(_info.gameObject.activeSelf, Is.True);
            Dispatch(close.gameObject, false);
            Assert.That(_info.gameObject.activeSelf, Is.False);
            Dispatch(close.gameObject, true);
            Assert.That(_info.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MissingRequiredReference_ThrowsBeforeBinding()
        {
            var serialized = new SerializedObject(_info);
            serialized.FindProperty("stats.health").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var error = Assert.Throws<InvalidOperationException>(() => _info.Validate());
            Assert.That(error.Message, Does.Contain("UIId.DeckSlotInfo").And.Contain("stats.health"));
        }

        [Test]
        public void BodyHasNoCloseButton_AndMissingTraitsDoNotPartiallyRender()
        {
            Assert.That(_info.GetComponent<Button>(), Is.Null);
            _info.Render(Card(1, 0), "이전", _icon, null);
            Assert.Throws<InvalidOperationException>(() => _info.Render(Card(2, 1), "바뀌면 안 됨", null, null));
            Assert.That(Text("stats.category"), Is.EqualTo("Heavy"));
            Assert.That(Text("stats.unitName"), Is.EqualTo("이전"));
            Assert.That(Ref<Image>("stats.portrait").sprite, Is.SameAs(_icon));
        }

        private T Ref<T>(string property) where T : Object =>
            (T)new SerializedObject(_info).FindProperty(property).objectReferenceValue;
        private string Text(string property) => TextValue(Ref<Component>(property));
        private static string TextValue(Component text) => (string)text.GetType().GetProperty("text").GetValue(text);
        private static string TextComponent(Transform target) => TextValue(target.GetComponents<Component>().Single(c => c != null && c.GetType().Name == "TextMeshProUGUI"));
        private GridLayoutGroup Area(int columns) => Ref<GridLayoutGroup>($"abilities.area{columns}.layout");
        private Button DeckInfo()
        {
            var slot = (DeckEditSlotView)new SerializedObject(_view).FindProperty("deckSlots").GetArrayElementAtIndex(1).objectReferenceValue;
            return (Button)new SerializedObject(slot).FindProperty("infoButton").objectReferenceValue;
        }

        private static void Dispatch(GameObject target, bool submit)
        {
            if (submit) ExecuteEvents.Execute(target, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            else ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }

        private static DeckEditDisplayModel Model() => new(new[] { Card(2, 0, false, 8) },
            new[] { null, Card(1, 0, true), null, null }, string.Empty, string.Empty, true, true);

        private static DeckEditCardModel Card(int id, int count, bool inDeck = false, int level = 7) => new(
            new CharacterInstanceId(id), EntityKey.Unit_Akimbo, level,
            new CharacterBaselineStatsSnapshot(9, 40, 13, 6, MoveMethod.LiftAndPlace, 3, 5, 1,
                CategoryKey.Heavy, Keys.Take(count).ToArray()), false, inDeck);

        private sealed class Input : IDeckEditInput
        {
            public void PlaceOwned(CharacterInstanceId ownedId, int slotIndex) { }
            public void RemoveSlot(int index) { }
            public void Confirm() { }
            public void Reevaluate() { }
            public void Close() { }
        }

        private sealed class Parent : IUIService
        {
            public void BindUISystemService(IUISystemService service) { }
            public void ExportUIService(IUIRegistry registry) { }
            public void GetUIData(ProjectL.Framework.SystemBase.IServiceModule query) { }
            public void BindUIService(ProjectL.Framework.SystemBase.ISystemServiceStoreReader resolver) { }
            public void Open() { }
            public void Hide() { }
        }

        private sealed class Traits : ITraitVisualTableQuery
        {
            private readonly Dictionary<TraitKey, TraitVisualData> _data = new();
            internal Traits(Sprite icon)
            {
                foreach (TraitKey key in Keys)
                {
                    var data = new TraitVisualData();
                    Set(data, "key", key);
                    Set(data, "displayName", "특성 " + key);
                    Set(data, "description", "설명 " + key);
                    Set(data, "icon", icon);
                    _data.Add(key, data);
                }
            }
            private static void Set(object data, string field, object value) =>
                data.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(data, value);
            public TraitVisualData GetTraitVisual(TraitKey key) => _data[key];
            public bool TryGetTraitVisual(TraitKey key, out TraitVisualData data) => _data.TryGetValue(key, out data);
            public IReadOnlyCollection<TraitVisualData> GetAllTraitVisuals() => _data.Values;
        }
    }
}
