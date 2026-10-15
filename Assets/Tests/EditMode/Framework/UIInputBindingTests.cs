using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Tests.Framework
{
    public sealed class UIInputBindingTests
    {
        private GameObject _root;
        private Button _button;
        private UIInputBinding _input;
        private Receiver _receiver;

        private sealed class Receiver : IUIInputReceiver
        {
            public int Clicks, Enters, Exits, Slot;
            public void Click(int slot) { Clicks++; Slot = slot; }
            public void HoverEnter(int slot) { Enters++; Slot = slot; }
            public void HoverExit(int slot) { Exits++; Slot = slot; }
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Input", typeof(RectTransform), typeof(Button));
            _button = _root.GetComponent<Button>();
            _receiver = new Receiver();
            _input = UIInputBinding.Attach(_button, _receiver, 7);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Dispatch_OnlyLeftClickAndSubmit_RespectsButtonState()
        {
            var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Right };
            ExecuteEvents.Execute(_root, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(_receiver.Clicks, Is.Zero);
            pointer.button = PointerEventData.InputButton.Left;
            ExecuteEvents.Execute(_root, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(_root, new BaseEventData(null), ExecuteEvents.submitHandler);
            Assert.That(_receiver.Clicks, Is.EqualTo(2));
            Assert.That(_receiver.Slot, Is.EqualTo(7));
            _button.interactable = false;
            ExecuteEvents.Execute(_root, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(_root, new BaseEventData(null), ExecuteEvents.submitHandler);
            Assert.That(_receiver.Clicks, Is.EqualTo(2));
            _button.interactable = true;
            _button.enabled = false;
            _input.OnPointerClick(pointer);
            Assert.That(_receiver.Clicks, Is.EqualTo(2));
        }

        [Test]
        public void Hover_ExitAndDisable_ClearExactlyOnce_AndReenableAllowsEntry()
        {
            ExecuteEvents.Execute(_root, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            _input.OnHoverEnter();
            Assert.That(_receiver.Enters, Is.EqualTo(1));
            _root.SetActive(false);
            _input.OnHoverExit();
            Assert.That(_receiver.Exits, Is.EqualTo(1));
            _root.SetActive(true);
            _input.OnHoverEnter();
            ExecuteEvents.Execute(_root, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            Assert.That(_receiver.Enters, Is.EqualTo(2));
            Assert.That(_receiver.Exits, Is.EqualTo(2));
        }

        [Test]
        public void Hover_InteractabilityLossAndRebind_ClearOldReceiver()
        {
            _input.OnHoverEnter();
            _button.interactable = false;
            typeof(UIInputBinding).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_input, null);
            _input.OnHoverExit();
            Assert.That(_receiver.Exits, Is.EqualTo(1));
            _button.interactable = true;
            _input.OnHoverEnter();
            var next = new Receiver();
            Assert.That(UIInputBinding.Attach(_button, next, 9), Is.SameAs(_input));
            Assert.That(_receiver.Exits, Is.EqualTo(2));
            _input.OnHoverEnter();
            Assert.That(next.Enters, Is.EqualTo(1));
            Assert.That(next.Slot, Is.EqualTo(9));
        }
    }
}
