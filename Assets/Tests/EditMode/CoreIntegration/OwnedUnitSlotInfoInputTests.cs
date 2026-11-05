using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>실제 Owned 프리팹에서 공용 상세창 의도가 EventSystem을 통해 전달되는지 확인합니다.</summary>
    public sealed class OwnedUnitSlotInfoInputTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void InfoButton_ClickAndSubmitForwardCurrentOwnedId(bool submit)
        {
            WithSlot((slot, receiver) =>
            {
                var info = (Component)new SerializedObject(slot).FindProperty("infoButton").objectReferenceValue;
                Dispatch(info.gameObject, submit);
                Assert.That(receiver.InfoIds, Is.EqualTo(new[] { new CharacterInstanceId(11) }));
                Assert.That(receiver.CardClicks, Is.Zero);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Card_ClickAndSubmitPreservePlacementPriority(bool consumed)
        {
            WithSlot((slot, receiver) =>
            {
                receiver.ConsumeCardClick = consumed;
                Dispatch(slot.gameObject, false);
                Dispatch(slot.gameObject, true);
                Assert.That(receiver.CardClicks, Is.EqualTo(2));
                Assert.That(receiver.InfoIds.Count, Is.EqualTo(consumed ? 0 : 2));
            });
        }

        [Test]
        public void ReusedSlot_UsesNewIdAndClearedSlotIgnoresInput()
        {
            WithSlot((slot, receiver) =>
            {
                slot.Render(Card(12), "새 유닛", null, null, false);
                Dispatch(slot.gameObject, false);
                Assert.That(receiver.InfoIds, Is.EqualTo(new[] { new CharacterInstanceId(12) }));
                Assert.That(slot.Card.Level, Is.EqualTo(4));
                slot.Clear();
                Dispatch(slot.gameObject, true);
                Assert.That(receiver.InfoIds.Count, Is.EqualTo(1));
            });
        }

        private static void Dispatch(GameObject target, bool submit)
        {
            if (submit) ExecuteEvents.Execute(target, new BaseEventData(null), ExecuteEvents.submitHandler);
            else ExecuteEvents.Execute(target, new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }

        private static void WithSlot(System.Action<OwnedUnitSlot, Receiver> action)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/Deck/OwnedUnitSlot.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                var slot = instance.GetComponent<OwnedUnitSlot>();
                var receiver = new Receiver();
                slot.BindInteraction(receiver);
                slot.Render(Card(11), "유닛", null, null, false);
                slot.Open();
                action(slot, receiver);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static DeckEditCardModel Card(int id) =>
            new(new CharacterInstanceId(id), EntityKey.Unit_Akimbo, 4, null, false, false);

        private sealed class Receiver : IDeckEditInteractionReceiver
        {
            internal readonly List<CharacterInstanceId> InfoIds = new();
            internal int CardClicks;
            internal bool ConsumeCardClick;
            public void ShowOwnedInfo(CharacterInstanceId id) => InfoIds.Add(id);
            public bool ClickOwned(OwnedUnitSlot slot) { CardClicks++; return ConsumeCardClick; }
            public void ShowSlotInfo(int index) { }
            public void BeginSlotChange(int index) { }
            public void DeleteSlot(int index) { }
            public void CancelSlotChange() { }
            public void CloseSlotInfo() { }
            public void SlotInfoHidden() { }
        }
    }
}
