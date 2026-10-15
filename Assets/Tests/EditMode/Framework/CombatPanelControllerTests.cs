using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// CombatViewSystem에서 뽑은 포기 확인창·개발 Layer 패널 컨트롤러를 씬 없이 확인합니다(SOLID·SSOT 개선 설계 D2·D3).
    /// 조정자와 함께 도는 동작은 CombatViewSystemTests의 포기·개발 패널 테스트가 그대로 확인합니다.
    /// </summary>
    public sealed class CombatPanelControllerTests
    {
        private readonly List<Exception> _logged = new();
        private RuntimeFaultBoundary _faults;
        private CombatInputFacts _facts;
        private bool _canAct;
        private int _cancelCalls;

        [SetUp]
        public void SetUp()
        {
            _logged.Clear();
            _faults = new RuntimeFaultBoundary(_logged.Add);
            _facts = Facts(CombatExecutionMode.Live);
            _canAct = true;
            _cancelCalls = 0;
        }

        #region 포기 확인창

        [Test]
        public void Abandon_Attach_BindsAndClosesConfirm_LiveShowsDisabledButton_RecordHides()
        {
            var display = new AbandonDisplay { ConfirmShown = true };
            CreateAbandon(CombatExecutionMode.Live).Attach(display, new AbandonRequest());
            Assert.That(display.Open, Is.Not.Null);
            Assert.That(display.ConfirmShown, Is.False);
            Assert.That((display.Visible, display.Interactable), Is.EqualTo((true, false)));

            var record = new AbandonDisplay();
            CreateAbandon(CombatExecutionMode.RecordInputOnly).Attach(record, new AbandonRequest());
            Assert.That((record.Visible, record.Interactable), Is.EqualTo((false, false)), "기록 모드는 포기할 수 없다(C7).");
        }

        [Test]
        public void Abandon_Open_CancelsInputDisplay_Confirm_RequestsOnceAndCloses()
        {
            var display = new AbandonDisplay();
            var request = new AbandonRequest();
            CombatAbandonController abandon = CreateAbandon(CombatExecutionMode.Live);
            abandon.Attach(display, request);
            abandon.Sync();
            Assert.That(display.Interactable, Is.True);

            display.Open();
            Assert.That(abandon.IsConfirmOpen, Is.True);
            Assert.That(display.ConfirmShown, Is.True);
            Assert.That(_cancelCalls, Is.EqualTo(1), "확인창을 열기 전에 입력 중 표시를 정리한다(P3).");

            display.Confirm();
            display.Confirm();
            Assert.That(request.Requests, Is.EqualTo(1), "닫힌 뒤의 확인은 다시 요청하지 않는다.");
            Assert.That(abandon.IsConfirmOpen, Is.False);
            Assert.That(display.ConfirmShown, Is.False);
        }

        [Test]
        public void Abandon_SyncWhenExitStarted_DisablesAndClosesOpenConfirm()
        {
            var display = new AbandonDisplay();
            var request = new AbandonRequest();
            CombatAbandonController abandon = CreateAbandon(CombatExecutionMode.Live);
            abandon.Attach(display, request);
            display.Open();

            request.Status = CombatExitStatus.Draining;
            abandon.Sync();

            Assert.That(display.Interactable, Is.False);
            Assert.That(abandon.IsConfirmOpen, Is.False);
            Assert.That(display.ConfirmShown, Is.False);
            display.Open();
            Assert.That(abandon.IsConfirmOpen, Is.False, "이탈 중에는 열지 않는다.");
        }

        [Test]
        public void Abandon_StopAndDetach_CloseConfirm_AndLateCallbacksDoNothing()
        {
            var display = new AbandonDisplay();
            var request = new AbandonRequest();
            CombatAbandonController abandon = CreateAbandon(CombatExecutionMode.Live);
            abandon.Attach(display, request);
            abandon.Sync();
            display.Open();

            abandon.Stop();
            Assert.That(abandon.IsConfirmOpen, Is.False);
            Assert.That(display.ConfirmShown, Is.False);
            Assert.That(display.Interactable, Is.False);

            _canAct = false;
            display.Open();
            Assert.That(abandon.IsConfirmOpen, Is.False, "오류·비활성이면 열지 않는다.");

            _canAct = true;
            abandon.Detach();
            display.Open();
            display.Confirm();
            abandon.Sync();
            Assert.That(display.ConfirmShown, Is.False);
            Assert.That(request.Requests, Is.Zero, "연결 해제 뒤 늦은 버튼 콜백은 아무것도 하지 않는다.");
        }

        [Test]
        public void Abandon_RequestFailure_IsReportedToFaultBoundary()
        {
            var display = new AbandonDisplay();
            CombatAbandonController abandon = CreateAbandon(CombatExecutionMode.Live);
            abandon.Attach(display, new AbandonRequest { Throw = true });
            display.Open();

            Assert.DoesNotThrow(() => display.Confirm());
            Assert.That(_faults.IsFaulted, Is.True);
            Assert.That(_logged, Has.Count.EqualTo(1));
        }

        #endregion

        #region 개발 Layer 패널

        [Test]
        public void DevLayer_RecordInputOnly_ShowsPanel_ButtonsChangeLayerAndCancelDisplay()
        {
            _facts = Facts(CombatExecutionMode.RecordInputOnly);
            var display = new DevDisplay();
            var layers = new Layers { CurrentLayer = StageInputLayers.Deployment };
            CreateDevLayer(CombatExecutionMode.RecordInputOnly).Attach(display, layers);
            Assert.That(display.ShowCount, Is.EqualTo(1));

            display.Combat();
            Assert.That(layers.CurrentLayer, Is.EqualTo(StageInputLayers.Combat));
            Assert.That(_cancelCalls, Is.EqualTo(1));

            display.Combat();
            Assert.That(layers.ChangeCount, Is.EqualTo(1), "같은 Layer면 바꾸지 않는다.");
            display.Deployment();
            Assert.That(layers.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
        }

        [Test]
        public void DevLayer_Live_HidesPanel_AndNeverConnects()
        {
            var display = new DevDisplay();
            CreateDevLayer(CombatExecutionMode.Live).Attach(display, new Layers());
            Assert.That(display.ShowCount, Is.Zero);
            Assert.That(display.HideCount, Is.EqualTo(1));
            Assert.That(display.Combat, Is.Null);
        }

        [Test]
        public void DevLayer_IgnoresButtons_WhenNotReceivingPendingOrDetached()
        {
            var display = new DevDisplay();
            var layers = new Layers { CurrentLayer = StageInputLayers.Deployment };
            CombatDevLayerController dev = CreateDevLayer(CombatExecutionMode.RecordInputOnly);
            dev.Attach(display, layers);
            Action combat = display.Combat;

            _facts = Facts(CombatExecutionMode.RecordInputOnly);
            _canAct = false;
            combat();
            _canAct = true;
            _facts = Facts(CombatExecutionMode.RecordInputOnly, hasPendingCommand: true);
            combat();
            Assert.That(layers.ChangeCount, Is.Zero, "입력 미연결·대기 중에는 바꾸지 않는다.");

            _facts = Facts(CombatExecutionMode.RecordInputOnly);
            dev.Hide();
            Assert.That(display.Combat, Is.Null);
            dev.Detach();
            combat();
            Assert.That(layers.ChangeCount, Is.Zero, "연결 해제 뒤 늦은 콜백은 무시한다.");
        }

        #endregion

        private CombatAbandonController CreateAbandon(CombatExecutionMode mode)
        {
            return new CombatAbandonController(mode, _faults, () => _facts, () => _canAct, () => _cancelCalls++);
        }

        private CombatDevLayerController CreateDevLayer(CombatExecutionMode mode)
        {
            return new CombatDevLayerController(mode, _faults, () => _facts, () => _canAct, () => _cancelCalls++);
        }

        private static CombatInputFacts Facts(CombatExecutionMode mode, bool hasPendingCommand = false)
        {
            return new CombatInputFacts(
                isBattleDisplayed: true,
                hasBattle: true,
                phase: CombatFlowPhase.PlayerInput,
                stagePhase: StagePhase.Deployment,
                hasCompletion: false,
                executionMode: mode,
                hasPendingCommand: hasPendingCommand);
        }

        private sealed class AbandonDisplay : ICombatAbandonDisplay
        {
            internal Action Open;
            internal Action Confirm;
            internal Action Cancel;
            internal bool Visible;
            internal bool Interactable;
            internal bool ConfirmShown;

            public void BindAbandon(Action open, Action confirm, Action cancel)
            {
                Open = open;
                Confirm = confirm;
                Cancel = cancel;
            }

            public void SetAbandonAvailable(bool visible, bool interactable)
            {
                Visible = visible;
                Interactable = interactable;
            }

            public void ShowAbandonConfirm() => ConfirmShown = true;

            public void HideAbandonConfirm() => ConfirmShown = false;
        }

        private sealed class AbandonRequest : ICombatAbandonRequest
        {
            internal CombatExitStatus Status = CombatExitStatus.Idle;
            internal bool Throw;
            internal int Requests;

            public CombatExitStatus ExitStatus => Status;

            public bool RequestAbandon()
            {
                Requests++;
                if (Throw)
                    throw new InvalidOperationException("포기 요청 실패(테스트)");
                return true;
            }
        }

        private sealed class DevDisplay : ICombatDevLayerDisplay
        {
            internal Action Deployment;
            internal Action Combat;
            internal int ShowCount;
            internal int HideCount;

            public void ShowDevLayerPanel(Action onDeployment, Action onCombat)
            {
                ShowCount++;
                Deployment = onDeployment;
                Combat = onCombat;
            }

            public void HideDevLayerPanel()
            {
                HideCount++;
                Deployment = null;
                Combat = null;
            }
        }

        private sealed class Layers : IInputLayerService
        {
            internal int ChangeCount;

            public InputLayer CurrentLayer { get; set; }

            public void ChangeLayer(InputLayer nextLayer)
            {
                CurrentLayer = nextLayer;
                ChangeCount++;
            }

            public void ClearLayer() => CurrentLayer = default;
        }
    }
}
