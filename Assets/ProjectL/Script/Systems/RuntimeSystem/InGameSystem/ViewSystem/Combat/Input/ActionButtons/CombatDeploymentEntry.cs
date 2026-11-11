using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 배치 국면의 전투 진입입니다(View 3라운드 W11-2). 배치 확인창(<see cref="CombatDeploymentConfirmation"/>)과 그 콜백 넷
    /// (진입 가능 판단·진입 제출·입력 정리·메인 미배치 안내)을 한 곳에 둡니다.
    /// </summary>
    /// <remarks>
    /// 수명은 씬 연결 수명입니다. 연결 입력 묶음(<see cref="CombatSceneInput"/>)이 함께 갖습니다(설계 P23).
    /// 확인창이 열렸는지는 연결이 없으면 언제나 거짓이라, 서비스 수명이던 때와 답이 같습니다.
    /// 닫기는 조정자가 지금과 같은 자리(분리·입력 정지·입력 닫힘의 맨 앞)에서 부릅니다.
    /// </remarks>
    internal sealed class CombatDeploymentEntry
    {
        private readonly CombatDeploymentConfirmation _confirmation;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<bool> _isInputReady;
        private readonly Func<bool, CombatInputFacts> _readInputFacts;
        private readonly CombatInputRouter _router;
        private readonly ICharacterBufferReadSink _characters;
        private readonly CombatActionNotice _notices;
        private readonly Action _afterSubmit;

        /// <param name="confirmation">배치 확인창 표시입니다. 필수입니다(입력 재료 묶음 <c>CombatSceneInputSources</c>가 null을 거절하고, 조정자가 서비스 바인딩 때 확인창 계약을 검사).</param>
        /// <param name="isInputReady">입력 판단에 쓸 수 있는지입니다(씬 연결 활성 ∧ 입력 슬롯 연결 ∧ 진입 연출 완료, 조정자의 판정식).</param>
        /// <param name="readInputFacts">진입 가능 판단에 쓸 사실 공급자입니다(인자 = 선행 미처리 명령까지 읽을지).</param>
        /// <param name="router">진입 제출·입력 정리를 넘길 Router입니다(좌표 없는 명령, 환경 호버는 건드리지 않음).</param>
        /// <param name="afterSubmit">진입을 제출한 뒤 행동 버튼을 바로 맞추는 입력 분배의 입구(<c>CombatSceneInputDriver.SyncActionButtons</c>)입니다(계획 TP-21).</param>
        internal CombatDeploymentEntry(
            ICombatStateBufferReadSink state,
            ICharacterBufferReadSink characters,
            IUIConfirmationPresentation confirmation,
            RuntimeFaultBoundary faults,
            Func<bool> isInputReady,
            Func<bool, CombatInputFacts> readInputFacts,
            CombatInputRouter router,
            CombatActionNotice notices,
            Action afterSubmit)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _isInputReady = isInputReady ?? throw new ArgumentNullException(nameof(isInputReady));
            _readInputFacts = readInputFacts ?? throw new ArgumentNullException(nameof(readInputFacts));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _notices = notices ?? throw new ArgumentNullException(nameof(notices));
            _afterSubmit = afterSubmit ?? throw new ArgumentNullException(nameof(afterSubmit));
            _confirmation = new CombatDeploymentConfirmation(characters, () => state.Current?.Membership, confirmation,
                CanEnter, Submit, CancelInputs, faults, ShowMainNotPlaced);
        }

        /// <summary>배치 확인창이 열려 있는지입니다. 수신 창구와 입력 사실이 묻습니다.</summary>
        internal bool IsOpen => _confirmation.IsOpen;

        /// <summary>진입 요청입니다(키·버튼의 진입 입력). 미배치 유닛이 있으면 확인창을 엽니다.</summary>
        internal void Request() => _confirmation.Request();

        /// <summary>열린 확인창이 지금 사실로 더 열려 있으면 안 되면 닫습니다. 행동 버튼을 맞출 때 부릅니다.</summary>
        internal void Sync() => _confirmation.Sync();

        /// <summary>확인창을 닫습니다. 열려 있지 않으면 아무것도 하지 않습니다.</summary>
        internal void Close() => _confirmation.Close();

        /// <summary>
        /// 확인창이 "열린 채 둘지"를 묻는 판단이기도 해서(<see cref="Sync"/>) 모달 열림은 여기서 보지 않습니다.
        /// 입력 닫힘(제출 정지·포기 요청)은 <see cref="CombatInputPolicy.CanSubmit"/>의 공통 판정이 봅니다.
        /// </summary>
        private bool CanEnter()
        {
            if (_faults.IsFaulted || !_isInputReady()) return false;
            CombatInputFacts facts = _readInputFacts(true);
            return CombatInputPolicy.CanSubmit(facts, facts.HasPendingCommand, CombatViewCommandKind.EnterCombat);
        }

        private void Submit()
        {
            _router.Route(CombatInputRouter.Request.EnterCombat, 0f, 0f, _characters.Characters);
            _afterSubmit();
        }

        // 좌표 없는 명령이라 Router로만 넘긴다. 환경 호버 지우기를 더하지 않는다(2라운드 계획 검증 M4).
        private void CancelInputs() =>
            _router.Route(CombatInputRouter.Request.FocusLost, 0f, 0f, _characters.Characters);

        private void ShowMainNotPlaced() => _notices.MainCharacterNotPlaced();
    }
}
