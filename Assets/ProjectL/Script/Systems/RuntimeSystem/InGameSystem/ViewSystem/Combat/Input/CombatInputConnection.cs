using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 전투 입력의 연결입니다(View 3라운드 W11-1). 수신 창구(<see cref="CombatInputReceiver"/>)를 만들어 갖고, 입력 Layer를 목표에 맞추거나 풉니다.
    /// 행동 버튼(공격·턴 종료·복귀)도 수신 창구로 들어가므로 그 입구를 여기서 내줍니다.
    /// </summary>
    /// <remarks>
    /// 수명은 서비스 수명입니다. 조정자가 서비스를 받을 때(<c>Bind</c>) 한 번 만들고, 씬 연결마다 바꾸지 않습니다(계획 TP-22).
    /// 목표 Layer를 정하는 것은 입력 분배(<c>CombatSceneInputDriver.SyncInputLayer</c>, 정책 <c>CombatInputPolicy</c>)이고, 이 객체는 받은 목표로 슬롯 연결·Layer 변경·해제만 합니다.
    /// </remarks>
    internal sealed class CombatInputConnection
    {
        private readonly IInputLayerService _layers;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Action _cancelPresentation;

        /// <param name="canReceive">조정자의 이름 있는 수명 판정(씬 연결 활성)입니다. 수신 창구가 입구에서 묻습니다.</param>
        /// <param name="isConfirmOpen">전투 입력을 막는 확인창이 열렸는지입니다. 수신 창구가 입구에서 묻습니다.</param>
        /// <param name="route">입구 검사를 통과한 입력을 넘길 곳입니다(입력 분배 <see cref="CombatSceneInputDriver.Route"/>에 조정자가 그 순간의 연결을 붙여 넘김).</param>
        /// <param name="cancelPresentation">입력 중 표시(Drag·모드·선택·미리보기)를 정리하는 조정자의 입구입니다. Layer를 바꾸거나 풀 때 부릅니다.</param>
        internal CombatInputConnection(
            IStageInputReceiverSlot slot,
            IInputLayerService layers,
            RuntimeFaultBoundary faults,
            Func<bool> canReceive,
            Func<bool> isConfirmOpen,
            Action<CombatInputRouter.Request, float, float> route,
            Action cancelPresentation)
        {
            if (slot == null)
                throw new ArgumentNullException(nameof(slot));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _cancelPresentation = cancelPresentation ?? throw new ArgumentNullException(nameof(cancelPresentation));
            Receiver = new CombatInputReceiver(slot, faults, canReceive, isConfirmOpen, route);
            // 버튼 입구는 수신 창구 메서드를 한 번만 대리자로 만들어 둡니다. 수신 창구가 먼저 있으므로 null 검사가 필요 없습니다.
            // 이 객체는 서비스를 받을 때 한 번만 만들어서, 버튼이 가리키는 수신 창구가 바뀌지 않습니다.
            AttackPressed = Receiver.AttackToggleRequested;
            EndTurnPressed = Receiver.EndTurnRequested;
            ReturnPressed = Receiver.RequestReturn;
            SkillPressed = Receiver.SkillRequested;
        }

        /// <summary>입력 수신 창구입니다. 씬 루트의 BattleChanger 입력 연결이 이것을 받습니다(조정자가 루트 구독 때 넘김).</summary>
        internal CombatInputReceiver Receiver { get; }

        /// <summary>입력 슬롯에 연결돼 있는지입니다(수신 창구의 값).</summary>
        internal bool IsBound => Receiver.IsBound;

        /// <summary>지금 입력 Layer입니다. 입력 사실 조립이 읽습니다.</summary>
        internal InputLayer CurrentLayer => _layers.CurrentLayer;

        /// <summary>공격 버튼의 입구입니다(행동 버튼 설계 A2).</summary>
        internal Action AttackPressed { get; }

        /// <summary>턴 종료 버튼의 입구입니다.</summary>
        internal Action EndTurnPressed { get; }

        /// <summary>복귀 버튼의 입구입니다.</summary>
        internal Action ReturnPressed { get; }

        /// <summary>스킬 버튼의 입구입니다. R 키와 같은 입구입니다(전투 스킬 View 설계 SK4).</summary>
        internal Action SkillPressed { get; }

        /// <summary>
        /// 입력 연결을 목표 Layer로 맞춥니다. 목표가 없으면(준비 전·종료·입력 닫힘·전투 정리) 표현을 정리하고 입력을 해제합니다.
        /// Layer가 바뀌면 이전 단계의 Drag·모드·선택·미리보기를 정리합니다(대기는 보존, §5.5). 실제 Layer와 같으면 다시 바꾸지 않습니다.
        /// </summary>
        internal void Apply(InputLayer desired)
        {
            if (desired == default)
            {
                _cancelPresentation();
                ReleaseOrReport();
                return;
            }

            Receiver.Bind();

            if (_layers.CurrentLayer != desired)
            {
                _cancelPresentation();
                _layers.ChangeLayer(desired);
            }
        }

        /// <summary>입력을 해제하고, 실패는 오류 경계에 한 번 모아 보고합니다.</summary>
        internal void ReleaseOrReport()
        {
            List<Exception> errors = null;
            Release(ref errors);
            if (errors != null)
                _faults.Report(new AggregateException("[CombatViewSystem] 입력 해제에 실패했습니다.", errors));
        }

        /// <summary>
        /// 수신 창구 Unbind와 ClearLayer를 각각 시도합니다. 연결돼 있지 않거나 Layer가 없으면 건드리지 않습니다.
        /// Unbind 중 슬롯이 보내는 SelectCanceled는 연결된 상태로 받아 Router가 표현을 정리합니다(명령은 기록하지 않음, R2).
        /// </summary>
        internal void Release(ref List<Exception> errors)
        {
            CleanupErrors.Try(Receiver.Unbind, ref errors);
            CleanupErrors.Try(ClearLayerIfSet, ref errors);
        }

        /// <summary>창 포커스를 잃었습니다. 수신 창구로 넘깁니다.</summary>
        internal void FocusLost() => Receiver.FocusLost();

        /// <summary>Layer가 있을 때만 비웁니다. 현재 Layer 읽기도 정리 실패로 모으려고 메서드 안에서 읽습니다.</summary>
        private void ClearLayerIfSet()
        {
            if (_layers.CurrentLayer != default)
                _layers.ClearLayer();
        }
    }
}
