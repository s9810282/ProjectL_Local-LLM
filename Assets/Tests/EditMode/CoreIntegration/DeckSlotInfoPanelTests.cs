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
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>StageSelect 씬의 덱 화면과 UISystem 경로에서 공용 상세창 표시·입력·수명을 확인합니다.</summary>
    public sealed class DeckSlotInfoPanelTests
    {
        private static readonly TraitKey[] Keys =
            { TraitKey.DoubleShot, TraitKey.LuckyBullet, TraitKey.ChainExplosion, TraitKey.ImpactBolt, TraitKey.Vanguard };
        private Scene _scene;
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
            _scene = EditorSceneManager.OpenPreviewScene(SceneIndices.StagePath);
            _view = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DeckEditUI>(true)).Single();
            using (var serialized = new SerializedObject(_view))
                _info = (DeckSlotInfoUI)serialized.FindProperty("slotInfoPanel").objectReferenceValue;
            _icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            _eventsObject = new GameObject("상세창 시험 입력", typeof(DeckMainPolicyTestEventSystem));
            EventSystem.current = _eventsObject.GetComponent<EventSystem>();
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
            OpenDeck();
        }

        [TearDown]
        public void TearDown()
        {
            if (_ui != null) _ui.Shutdown();
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
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
        public void Abilities_ShowAllContentsInOrderAndScrollToLast(int count)
        {
            var query = new Traits(_icon);
            ScrollRect scroll = Ref<ScrollRect>("abilities.scroll");
            RectTransform content = scroll.content;
            Transform unitSlots = Ref<RectTransform>("abilities.unit.template.root").parent;
            _info.Render(Card(1, count), "시험", null, query);
            var shown = unitSlots.Cast<RectTransform>().Where(slot => slot.gameObject.activeSelf).ToArray();
            Assert.That(shown.Length, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                Assert.That(TextComponent(shown[i].Find("Name")), Is.EqualTo("특성 " + Keys[i]));
                Assert.That(TextComponent(shown[i].Find("Description")), Is.EqualTo("설명 " + Keys[i]));
                Assert.That(shown[i].Find("Icon").GetComponent<Image>().sprite, Is.EqualTo(query.GetTraitVisual(Keys[i]).Icon));
                Assert.That(shown[i].GetComponent<Button>(), Is.Null);
            }
            _ui.OpenUI(UIId.DeckSlotInfo);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var bounds = new Vector3[4];
            Rect contentRect = content.rect;
            float nextTop = contentRect.yMax;
            foreach (RectTransform slot in shown)
            {
                slot.GetWorldCorners(bounds);
                foreach (Vector3 point in bounds)
                {
                    Vector3 local = content.InverseTransformPoint(point);
                    Assert.That(local.x, Is.InRange(contentRect.xMin - .1f, contentRect.xMax + .1f));
                    Assert.That(local.y, Is.InRange(contentRect.yMin - .1f, nextTop + .1f));
                }
                nextTop = content.InverseTransformPoint(bounds[0]).y;
            }
            if (count == Keys.Length)
            {
                Rect viewportRect = scroll.viewport.rect;
                Assert.That(contentRect.height, Is.GreaterThan(viewportRect.height));
                Assert.That(scroll.scrollSensitivity, Is.GreaterThan(0f));
                ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(EventSystem.current)
                {
                    scrollDelta = Vector2.down * (contentRect.height / scroll.scrollSensitivity)
                }, ExecuteEvents.scrollHandler);
                shown[count - 1].GetWorldCorners(bounds);
                foreach (Vector3 point in bounds)
                {
                    Vector3 local = scroll.viewport.InverseTransformPoint(point);
                    Assert.That(local.y, Is.InRange(viewportRect.yMin - .1f, viewportRect.yMax + .1f));
                }
            }
        }

        [Test]
        public void Abilities_ManyToFewClearsHiddenDataAndReusesClones()
        {
            var query = new Traits(_icon);
            Transform unitSlots = Ref<RectTransform>("abilities.unit.template.root").parent;
            _info.Render(Card(1, 5), "많음", null, query);
            var originalSlots = unitSlots.Cast<RectTransform>().ToArray();
            Assert.That(originalSlots.Length, Is.EqualTo(5));
            _info.Render(Card(2, 1), "적음", null, query);
            Assert.That(originalSlots[0].gameObject.activeSelf, Is.True);
            foreach (RectTransform slot in originalSlots.Skip(1))
            {
                Assert.That(slot.gameObject.activeSelf, Is.False);
                Assert.That(TextComponent(slot.Find("Name")), Is.Empty);
                Assert.That(TextComponent(slot.Find("Description")), Is.Empty);
                Assert.That(slot.Find("Icon").GetComponent<Image>().sprite, Is.Null);
            }
            _info.Render(Card(1, 5), "다시", null, query);
            Assert.That(unitSlots.Cast<RectTransform>().ToArray(), Is.EqualTo(originalSlots));
            Assert.That(originalSlots.All(slot => slot.gameObject.activeSelf), Is.True);
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
            OpenDeck();
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
        private Button DeckInfo()
        {
            var slot = (DeckEditSlotView)new SerializedObject(_view).FindProperty("deckSlots").GetArrayElementAtIndex(1).objectReferenceValue;
            return (Button)new SerializedObject(slot).FindProperty("infoButton").objectReferenceValue;
        }

        private void OpenDeck()
        {
            _ui.OpenUI(UIId.DeckEdit);
            // EditMode에서는 연출 프레임을 기다리지 않고 입력이 열리는 완료 상태를 적용합니다.
            var presentation = _view.ScrollPresentation;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            presentation.GetType().GetField("_progress", flags).SetValue(presentation, 1f);
            presentation.GetType().GetMethod("Apply", flags).Invoke(presentation, null);
        }

        private static void Dispatch(GameObject target, bool submit)
        {
            if (submit) ExecuteEvents.Execute(target, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            else ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }

        private static DeckEditDisplayModel Model() => new(new[] { Card(2, 0, false, 8) },
            new[] { null, Card(1, 0, true), null, null }, true, true, false, true);

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
