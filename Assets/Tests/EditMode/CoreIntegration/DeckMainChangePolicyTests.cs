using System;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
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

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>실제 덱 프리팹에서 임시 메인 변경 정책과 입력 우회 차단을 검증합니다.</summary>
    public sealed class DeckMainChangePolicyTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Open_AppliesSettingToMainButtonsAndClickPlacement(bool allow)
        {
            WithView((view, input) =>
            {
                Assert.That(view.AllowMainCharacterChange, Is.False);
                view.AllowMainCharacterChange = allow;
                view.Open();
                DeckEditSlotView main = Slot(view, 0);
                Assert.That(Button(main, "changeButton").interactable, Is.EqualTo(allow));
                Assert.That(Button(main, "changeButton").gameObject.activeSelf, Is.EqualTo(allow));
                Assert.That(Button(main, "deleteButton").gameObject.activeSelf, Is.False);
                Assert.That(Label(main).gameObject.activeSelf, Is.True);
                Assert.That(LabelText(main), Is.EqualTo(allow ? "Main" : "Main 변경 불가"));
                Assert.That(Label(Slot(view, 1)).gameObject.activeSelf, Is.True);
                Assert.That(LabelText(Slot(view, 1)), Is.EqualTo("Heavy"));
                Assert.That(Button(Slot(view, 1), "changeButton").interactable, Is.True);
                Assert.That(Button(main, "infoButton").interactable, Is.True);
                Click(Button(main, "changeButton").gameObject);
                Assert.That(view.ChangingSlotIndex, Is.EqualTo(allow ? 0 : -1));
                Click(OwnedMain(view).gameObject);
                Assert.That(input.Placements, Is.EqualTo(allow ? 1 : 0));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MainDragAndDrop_ObeySameSetting(bool allow)
        {
            WithView((view, input) =>
            {
                view.AllowMainCharacterChange = allow;
                view.Open();
                OwnedUnitSlot source = OwnedMain(view);
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left, pointerId = 1, pointerDrag = source.gameObject
                };
                ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(Slot(view, 0).gameObject, pointer, ExecuteEvents.dropHandler);
                Assert.That(input.Placements, Is.EqualTo(allow ? 1 : 0));
                ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.endDragHandler);
            });
        }

        [Test]
        public void LockWhileOpen_ClearsChangeModeAndMovesFocusToInfo()
        {
            WithView((view, input) =>
            {
                GameObject eventsObject = new("Policy Test EventSystem", typeof(DeckMainPolicyTestEventSystem));
                try
                {
                    EventSystem events = eventsObject.GetComponent<EventSystem>();
                    EventSystem.current = events;
                    view.AllowMainCharacterChange = true;
                    view.Open();
                    DeckEditSlotView main = Slot(view, 0);
                    Click(Button(main, "changeButton").gameObject);
                    events.SetSelectedGameObject(Button(main, "deleteButton").gameObject);
                    view.AllowMainCharacterChange = false;
                    Assert.That(view.ChangingSlotIndex, Is.EqualTo(-1));
                    Assert.That(Button(main, "changeButton").interactable, Is.False);
                    Assert.That(Button(main, "changeButton").gameObject.activeSelf, Is.False);
                    Assert.That(Button(main, "deleteButton").gameObject.activeSelf, Is.False);
                    Assert.That(events.currentSelectedGameObject, Is.EqualTo(Button(main, "infoButton").gameObject));
                    ExecuteEvents.Execute(Button(main, "changeButton").gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
                    ExecuteEvents.Execute(Button(main, "deleteButton").gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
                    Assert.That(input.Removals, Is.Zero);
                    Assert.That(view.ChangingSlotIndex, Is.EqualTo(-1));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(eventsObject);
                }
            });
        }

        [Test]
        public void Open_RefreshesSerializedSettingAndClearsPreviousChangeMode()
        {
            WithView((view, input) =>
            {
                view.AllowMainCharacterChange = true;
                view.Open();
                Click(Button(Slot(view, 0), "changeButton").gameObject);
                Assert.That(view.ChangingSlotIndex, Is.EqualTo(0));
                var serialized = new SerializedObject(view);
                serialized.FindProperty("allowMainCharacterChange").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                view.Open();
                Assert.That(view.ChangingSlotIndex, Is.EqualTo(-1));
                Assert.That(Button(Slot(view, 0), "changeButton").interactable, Is.False);
                Assert.That(Button(Slot(view, 0), "changeButton").gameObject.activeSelf, Is.False);
                Assert.That(LabelText(Slot(view, 0)), Is.EqualTo("Main 변경 불가"));
            });
        }

        [Test]
        public void EmptySlot_LockAppendsNoticeAndUnlockRestoresOriginalLabel()
        {
            WithView((view, input) =>
            {
                view.Open();
                DeckEditSlotView empty = Slot(view, 2);
                Assert.That(LabelText(empty), Is.EqualTo("Rider - 비어 있음"));
                view.Render(Model(false));
                Assert.That(Button(empty, "changeButton").gameObject.activeSelf, Is.False);
                Assert.That(Button(empty, "deleteButton").gameObject.activeSelf, Is.False);
                Assert.That(Label(empty).gameObject.activeSelf, Is.True);
                Assert.That(LabelText(empty), Is.EqualTo("Rider - 비어 있음 변경 불가"));
                view.Open();
                Assert.That(LabelText(empty), Is.EqualTo("Rider - 비어 있음 변경 불가"));
                view.Render(Model(true));
                Assert.That(Button(empty, "changeButton").gameObject.activeSelf, Is.True);
                Assert.That(LabelText(empty), Is.EqualTo("Rider - 비어 있음"));
            });
        }

        [Test]
        public void MainOverride_DoesNotOverrideOverallEditRestriction()
        {
            WithView((view, input) =>
            {
                view.AllowMainCharacterChange = true;
                view.Render(Model(false));
                view.Open();
                Assert.That(Button(Slot(view, 0), "changeButton").interactable, Is.False);
                Assert.That(Button(Slot(view, 1), "changeButton").interactable, Is.False);
            });
        }

        private static void Click(GameObject target) => ExecuteEvents.Execute(target,
            new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

        private static DeckEditSlotView Slot(DeckEditUI view, int index) =>
            (DeckEditSlotView)new SerializedObject(view).FindProperty("deckSlots").GetArrayElementAtIndex(index).objectReferenceValue;

        private static Button Button(DeckEditSlotView slot, string field) =>
            (Button)new SerializedObject(slot).FindProperty(field).objectReferenceValue;

        private static Component Label(DeckEditSlotView slot) =>
            (Component)new SerializedObject(slot).FindProperty("emptyText").objectReferenceValue;

        private static string LabelText(DeckEditSlotView slot)
        {
            Component label = Label(slot);
            return (string)label.GetType().GetProperty("text").GetValue(label);
        }

        private static OwnedUnitSlot OwnedMain(DeckEditUI view)
        {
            foreach (OwnedUnitSlot slot in view.GetComponentsInChildren<OwnedUnitSlot>(true))
                if (slot.Card?.IsMain == true) return slot;
            throw new InvalidOperationException("시험용 미편성 메인 슬롯이 없습니다.");
        }

        private static void WithView(Action<DeckEditUI, Input> action)
        {
            GameObject canvas = new("Deck Policy Test Canvas", typeof(RectTransform), typeof(Canvas));
            var ui = new UISystem();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/Deck/DeckEditUI.prefab");
                var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
                var view = instance.GetComponent<DeckEditUI>();
                var input = new Input();
                view.BindInput(input);
                // 공용 상세창 활성화 뒤에는 변경 불가 유닛의 정보 입력도 실제 UI 수명으로 검증합니다.
                DeckSlotInfoUI info = view.GetComponentInChildren<DeckSlotInfoUI>(true);
                ui.AddUI(UIId.StageSelect, new Parent());
                view.ExportUIService(ui);
                info.ExportUIService(ui);
                view.BindUISystemService(ui);
                info.BindUISystemService(ui);
                ui.PostBindService(new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
                ui.Initialize();
                ui.OpenUI(UIId.StageSelect);
                view.Render(Model(true));
                ui.OpenUI(UIId.DeckEdit);
                action(view, input);
            }
            finally
            {
                ui.Shutdown();
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private sealed class Parent : IUIService
        {
            public void BindUISystemService(IUISystemService service) { }
            public void ExportUIService(IUIRegistry registry) { }
            public void GetUIData(IServiceModule query) { }
            public void BindUIService(ISystemServiceStoreReader resolver) { }
            public void Open() { }
            public void Hide() { }
        }

        private static DeckEditDisplayModel Model(bool canEdit) => new(
            new[] { Card(3, true, false) },
            new[] { Card(1, true, true), Card(2, false, true), null, null }, canEdit, false, false, true);

        private static DeckEditCardModel Card(int id, bool main, bool inDeck) => new(
            new CharacterInstanceId(id), main ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Akimbo, 1,
            new CharacterBaselineStatsSnapshot(10, 10, 3, 2, MoveMethod.LiftAndPlace, 2, 2, 1,
                main ? CategoryKey.Main : CategoryKey.Heavy, Array.Empty<TraitKey>()), main, inDeck);

        private sealed class Input : IDeckEditInput
        {
            internal int Placements;
            internal int Removals;
            public void PlaceOwned(CharacterInstanceId ownedId, int slotIndex) => Placements++;
            public void RemoveSlot(int index) => Removals++;
            public void Confirm() { }
            public void Reevaluate() { }
            public void Close() { }
        }
    }

    /// <summary>EditMode에서도 기본 EventSystem의 등록·해제를 수행하는 시험 전용 구성입니다.</summary>
    [ExecuteAlways]
    public sealed class DeckMainPolicyTestEventSystem : EventSystem
    {
    }
}
