using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 전투 입력 수신 창구(SOLID·SSOT 개선 설계 D11)의 입구 검사·슬롯 연결·오류 보고를 씬 없이 확인합니다.
    /// Router 이후의 해석은 CombatInputRouterTests, 조정자와 함께 도는 동작은 CombatViewSystemTests가 확인합니다.
    /// </summary>
    public sealed class CombatInputReceiverTests
    {
        private readonly List<CombatInputRouter.Request> _routed = new();
        private readonly List<Exception> _logged = new();
        private RuntimeFaultBoundary _faults;
        private StageInputReceiverSlot _slot;
        private bool _canReceive;
        private bool _confirmOpen;
        private bool _throwOnRoute;
        private CombatInputReceiver _receiver;

        [SetUp]
        public void SetUp()
        {
            _routed.Clear();
            _logged.Clear();
            _faults = new RuntimeFaultBoundary(_logged.Add);
            _slot = new StageInputReceiverSlot();
            _canReceive = true;
            _confirmOpen = false;
            _throwOnRoute = false;
            _receiver = Create(_slot);
        }

        [Test]
        public void BeforeBind_InputIsIgnored_AfterBindItIsRouted()
        {
            _receiver.DrawRequested();
            Assert.That(_routed, Is.Empty);

            _receiver.Bind();
            _receiver.Bind();
            Assert.That(_receiver.IsBound, Is.True);
            _slot.DrawRequested();
            _slot.PointerMoved(1f, 2f);

            Assert.That(_routed, Is.EqualTo(new[] { CombatInputRouter.Request.Draw, CombatInputRouter.Request.PointerMoved }));
        }

        [Test]
        public void FaultOrInactiveEntry_IgnoresInput()
        {
            _receiver.Bind();

            _canReceive = false;
            _receiver.EndTurnRequested();
            _canReceive = true;
            _faults.Report(new InvalidOperationException("앞선 오류"));
            _receiver.EndTurnRequested();

            Assert.That(_routed, Is.Empty);
        }

        [Test]
        public void AbandonConfirmOpen_PassesOnlyPresentationCleanup()
        {
            _receiver.Bind();
            _confirmOpen = true;

            _receiver.EnterCombatRequested();
            _receiver.AttackModeRequested();
            _receiver.RequestReturn();
            _receiver.FocusLost();
            _receiver.SelectCanceled();

            Assert.That(_routed, Is.EqualTo(new[] { CombatInputRouter.Request.FocusLost, CombatInputRouter.Request.SelectCanceled }));
        }

        [Test]
        public void RouteFailure_IsReportedOnce_AndDoesNotEscapeTheUnityCallback()
        {
            _receiver.Bind();
            _throwOnRoute = true;

            Assert.DoesNotThrow(() => _receiver.MoveModeRequested());
            Assert.DoesNotThrow(() => _receiver.MoveModeRequested());

            Assert.That(_faults.IsFaulted, Is.True);
            Assert.That(_logged, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnbindDuringSelection_RoutesSelectCanceled_ThenStopsReceiving()
        {
            _receiver.Bind();
            _slot.SelectPressed(1f, 1f);
            _routed.Clear();

            _receiver.Unbind();

            // 슬롯이 해제 중에 보내는 SelectCanceled가 Router까지 가서 Drag·경로 표현을 정리한다(R2).
            Assert.That(_routed, Is.EqualTo(new[] { CombatInputRouter.Request.SelectCanceled }));
            Assert.That(_receiver.IsBound, Is.False);
            _slot.DrawRequested();
            _receiver.DrawRequested();
            Assert.That(_routed, Has.Count.EqualTo(1), "해제 뒤 입력은 받지 않는다.");
        }

        [Test]
        public void UnbindFailure_StillLeavesReceiverUnbound()
        {
            var slot = new ThrowingSlot();
            CombatInputReceiver receiver = Create(slot);
            receiver.Bind();

            Assert.Throws<InvalidOperationException>(receiver.Unbind);

            Assert.That(receiver.IsBound, Is.False);
            Assert.DoesNotThrow(receiver.Unbind);
        }

        private CombatInputReceiver Create(IStageInputReceiverSlot slot)
        {
            return new CombatInputReceiver(slot, _faults, () => _canReceive, () => _confirmOpen, (request, x, y) =>
            {
                _routed.Add(request);
                if (_throwOnRoute)
                    throw new InvalidOperationException("분배 실패(테스트)");
            });
        }

        private sealed class ThrowingSlot : IStageInputReceiverSlot
        {
            public void Bind(IStageInputReceiver receiver)
            {
            }

            public void Unbind(IStageInputReceiver receiver)
            {
                throw new InvalidOperationException("해제 실패(테스트)");
            }
        }
    }
}
