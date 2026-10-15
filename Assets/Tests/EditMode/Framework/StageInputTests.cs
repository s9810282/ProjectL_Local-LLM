using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using InputRouter = ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View.InputSystem;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 스테이지 내부 Controller와 수신 슬롯의 순수 계약을 실제 InputSystem 라우팅으로 확인합니다.
    /// Unity 장치 없이 Submit으로 입력을 넣고, Layer는 가짜 런타임이 저장합니다.
    /// </summary>
    public sealed class StageInputTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Layer_InitialAndClearedStatesDropInput(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            var receiver = rig.BindNew();

            rig.Pointer(target.Map, 1, 2);
            Assert.That(receiver.Events, Is.Empty);

            rig.Router.ChangeLayer(target.Layer);
            rig.Pointer(target.Map, 1, 2);
            Assert.That(receiver.Events, Is.EqualTo(new[] { "Pointer 1,2" }));

            rig.Router.ClearLayer();
            rig.Pointer(target.Map, 3, 4);
            Assert.That(receiver.Events.Count, Is.EqualTo(1));

            rig.Router.ChangeLayer(target.Layer);
            rig.Pointer(target.Map, 5, 6);
            Assert.That(receiver.Events[receiver.Events.Count - 1], Is.EqualTo("Pointer 5,6"));

            Assert.Throws<ArgumentException>(() => rig.Router.ChangeLayer(default));
        }

        [Test]
        public void DeploymentLayer_BlocksCombatMap()
        {
            var rig = new Rig();
            var receiver = rig.BindNew();
            rig.Router.ChangeLayer(StageInputLayers.Deployment);

            rig.Pointer(StageInputNames.Combat.Map, 1, 1);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.EndTurn);
            Assert.That(receiver.Events, Is.Empty);

            rig.Pointer(StageInputNames.Deployment.Map, 2, 2);
            Assert.That(receiver.Events, Is.EqualTo(new[] { "Pointer 2,2" }));
        }

        [Test]
        public void ClearLayer_RequiresConfiguredAndLiveRouter()
        {
            Assert.Throws<InvalidOperationException>(() => new InputRouter().ClearLayer());

            var rig = new Rig();
            rig.Router.Shutdown();
            Assert.Throws<ObjectDisposedException>(() => rig.Router.ClearLayer());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Select_PressAndNormalReleaseArriveOnceAtLastPointer(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            var receiver = rig.BindNew();
            rig.Router.ChangeLayer(target.Layer);

            rig.Pointer(target.Map, 3, 4);
            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Performed, true);
            rig.Select(target, InputPhase.Performed, false);
            // 누름 없이 온 false는 전달하지 않습니다.
            rig.Select(target, InputPhase.Performed, false);

            Assert.That(receiver.Events, Is.EqualTo(new[] { "Pointer 3,4", "Pressed 3,4", "Released 3,4" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Select_CanceledAfterPressIsCancelNotRelease(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            var receiver = rig.BindNew();
            rig.Router.ChangeLayer(target.Layer);

            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Canceled, false);

            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Performed, false);
            // 정상 뗌 뒤에 이어지는 Canceled는 추가 취소를 만들지 않습니다.
            rig.Select(target, InputPhase.Canceled, false);

            Assert.That(receiver.Events, Is.EqualTo(new[] { "Pressed 0,0", "Canceled", "Pressed 0,0", "Released 0,0" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pointer_CanceledDoesNotOverwritePosition(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            var receiver = rig.BindNew();
            rig.Router.ChangeLayer(target.Layer);

            rig.Pointer(target.Map, 5, 6);
            rig.Pointer(target.Map, 0, 0, InputPhase.Canceled);
            rig.Select(target, InputPhase.Started, true);

            Assert.That(receiver.Events, Is.EqualTo(new[] { "Pointer 5,6", "Pressed 5,6" }));
        }

        [Test]
        public void Slot_EmptyDropsInputAndRejectsInvalidBinds()
        {
            var rig = new Rig();
            var target = Target.Of(false);
            rig.Router.ChangeLayer(target.Layer);

            Assert.DoesNotThrow(() => rig.Pointer(target.Map, 1, 1));
            Assert.DoesNotThrow(() => rig.Select(target, InputPhase.Started, true));
            Assert.DoesNotThrow(() => rig.Select(target, InputPhase.Performed, false));

            Assert.Throws<ArgumentNullException>(() => rig.Slot.Bind(null));
            var a = new RecordingReceiver();
            var b = new RecordingReceiver();
            rig.Slot.Bind(a);
            Assert.Throws<InvalidOperationException>(() => rig.Slot.Bind(a));
            Assert.Throws<InvalidOperationException>(() => rig.Slot.Bind(b));

            // 연결되지 않은 대상의 Unbind는 무시합니다.
            rig.Slot.Unbind(b);
            rig.Pointer(target.Map, 2, 2);
            Assert.That(a.Events, Is.EqualTo(new[] { "Pointer 2,2" }));
            Assert.That(b.Events, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Slot_UnbindDuringSelectionCancelsOldOwnerOnly(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            rig.Router.ChangeLayer(target.Layer);
            var a = rig.BindNew();

            rig.Select(target, InputPhase.Started, true);
            rig.Slot.Unbind(a);
            var b = rig.BindNew();
            rig.Select(target, InputPhase.Performed, false);

            Assert.That(a.Events, Is.EqualTo(new[] { "Pressed 0,0", "Canceled" }));
            Assert.That(b.Events, Is.Empty);

            // 같은 대상을 다시 연결해도 옛 누름의 뗌은 전달되지 않습니다.
            rig.Slot.Unbind(b);
            rig.Slot.Bind(a);
            rig.Select(target, InputPhase.Started, true);
            rig.Slot.Unbind(a);
            rig.Slot.Bind(a);
            rig.Select(target, InputPhase.Performed, false);
            Assert.That(a.Events, Is.EqualTo(new[] { "Pressed 0,0", "Canceled", "Pressed 0,0", "Canceled" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Slot_ButtonHeldBeforeBindDoesNotStartSelection(bool combat)
        {
            var rig = new Rig();
            var target = Target.Of(combat);
            rig.Router.ChangeLayer(target.Layer);

            rig.Select(target, InputPhase.Started, true);
            var b = rig.BindNew();
            rig.Select(target, InputPhase.Performed, true);
            rig.Select(target, InputPhase.Performed, false);
            Assert.That(b.Events, Is.Empty);

            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Performed, false);
            Assert.That(b.Events, Is.EqualTo(new[] { "Pressed 0,0", "Released 0,0" }));
        }

        [Test]
        public void Slot_UnbindInsidePressCallbackCancelsOnce()
        {
            var rig = new Rig();
            var target = Target.Of(true);
            rig.Router.ChangeLayer(target.Layer);
            var a = new RecordingReceiver();
            a.OnPressed = () => rig.Slot.Unbind(a);
            rig.Slot.Bind(a);

            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Performed, false);

            Assert.That(a.Events, Is.EqualTo(new[] { "Pressed 0,0", "Canceled" }));
        }

        [Test]
        public void Slot_BindInsideCancelCallbackKeepsNewBinding()
        {
            var rig = new Rig();
            var target = Target.Of(true);
            rig.Router.ChangeLayer(target.Layer);
            var a = new RecordingReceiver();
            var c = new RecordingReceiver();
            a.OnCanceled = () => rig.Slot.Bind(c);
            rig.Slot.Bind(a);

            rig.Select(target, InputPhase.Started, true);
            rig.Slot.Unbind(a);
            // C는 연결되었지만 옛 선택의 소유자가 아니므로 뗌을 받지 않습니다.
            rig.Select(target, InputPhase.Performed, false);
            rig.Pointer(target.Map, 7, 8);
            rig.Select(target, InputPhase.Started, true);
            rig.Select(target, InputPhase.Performed, false);

            Assert.That(a.Events, Is.EqualTo(new[] { "Pressed 0,0", "Canceled" }));
            Assert.That(c.Events, Is.EqualTo(new[] { "Pointer 7,8", "Pressed 7,8", "Released 7,8" }));
        }

        [Test]
        public void SingleRequests_EachPerformedSendsOnlyItsRequest()
        {
            var rig = new Rig();
            var receiver = rig.BindNew();

            rig.Router.ChangeLayer(StageInputLayers.Deployment);
            rig.Press(StageInputNames.Deployment.Map, StageInputNames.Deployment.Cancel);
            rig.Press(StageInputNames.Deployment.Map, StageInputNames.Deployment.EnterCombat);
            rig.Press(StageInputNames.Deployment.Map, StageInputNames.Deployment.DrawCharacter);

            rig.Router.ChangeLayer(StageInputLayers.Combat);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.Cancel);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.EndTurn);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.MoveMode);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.AttackMode);
            rig.Press(StageInputNames.Combat.Map, StageInputNames.Combat.Return);
            // Press 바인딩은 Performed에서만 반응합니다.
            rig.Router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.EndTurn,
                InputPhase.Started, InputValueData.FromButton(true), 0);

            Assert.That(receiver.Events, Is.EqualTo(new[]
            {
                "Cancel", "EnterCombat", "Draw",
                "Cancel", "EndTurn", "MoveMode", "AttackMode", "Return"
            }));
        }

        private readonly struct Target
        {
            internal string Map { get; }
            internal string Select { get; }
            internal InputLayer Layer { get; }

            private Target(string map, string select, InputLayer layer)
            {
                Map = map;
                Select = select;
                Layer = layer;
            }

            internal static Target Of(bool combat) => combat
                ? new Target(StageInputNames.Combat.Map, StageInputNames.Combat.Select, StageInputLayers.Combat)
                : new Target(StageInputNames.Deployment.Map, StageInputNames.Deployment.Press, StageInputLayers.Deployment);
        }

        private sealed class Rig
        {
            internal InputRouter Router { get; } = new();
            internal StageInputReceiverSlot Slot { get; } = new();
            private readonly FakeLayerRuntime _layers = new();

            internal Rig()
            {
                Router.Configure(_layers, InputConfiguration.Create(
                    new DeploymentInputController(Slot),
                    new CombatInputController(Slot)));
            }

            internal RecordingReceiver BindNew()
            {
                var receiver = new RecordingReceiver();
                Slot.Bind(receiver);
                return receiver;
            }

            internal void Pointer(string map, float x, float y, InputPhase phase = InputPhase.Performed)
            {
                // 배치와 전투 맵은 포인터 액션 이름이 같습니다.
                Router.Submit(map, StageInputNames.Combat.PointerPosition, phase,
                    InputValueData.FromVector2(x, y), 0);
            }

            internal void Select(Target target, InputPhase phase, bool value)
            {
                Router.Submit(target.Map, target.Select, phase, InputValueData.FromButton(value), 0);
            }

            internal void Press(string map, string action)
            {
                Router.Submit(map, action, InputPhase.Performed, InputValueData.FromButton(true), 0);
            }
        }

        private sealed class FakeLayerRuntime : IInputLayerRuntime
        {
            public InputLayer CurrentLayer { get; private set; }

            public void ChangeLayer(InputLayer nextLayer) => CurrentLayer = nextLayer;

            public void ClearLayer() => CurrentLayer = default;
        }

        private sealed class RecordingReceiver : IStageInputReceiver
        {
            internal List<string> Events { get; } = new();
            internal Action OnPressed { get; set; }
            internal Action OnCanceled { get; set; }

            public void PointerMoved(float screenX, float screenY) => Events.Add($"Pointer {screenX},{screenY}");

            public void SelectPressed(float screenX, float screenY)
            {
                Events.Add($"Pressed {screenX},{screenY}");
                OnPressed?.Invoke();
            }

            public void SelectReleased(float screenX, float screenY) => Events.Add($"Released {screenX},{screenY}");

            public void SelectCanceled()
            {
                Events.Add("Canceled");
                OnCanceled?.Invoke();
            }

            public void Cancel() => Events.Add("Cancel");
            public void EnterCombatRequested() => Events.Add("EnterCombat");
            public void DrawRequested() => Events.Add("Draw");
            public void EndTurnRequested() => Events.Add("EndTurn");
            public void MoveModeRequested() => Events.Add("MoveMode");
            public void AttackModeRequested() => Events.Add("AttackMode");
            public void RequestReturn() => Events.Add("Return");
        }
    }
}
