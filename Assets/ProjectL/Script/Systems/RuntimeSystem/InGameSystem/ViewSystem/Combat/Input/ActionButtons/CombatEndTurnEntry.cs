using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 전투 국면의 턴 종료입니다. 턴 종료 확인창(<see cref="CombatEndTurnConfirmation"/>)과 그 콜백(종료 가능 판단·확인 여부·제출·입력 정리)을 한 곳에 둡니다.
    /// 배치 진입(<see cref="CombatDeploymentEntry"/>)과 같은 꼴입니다.
    /// </summary>
    /// <remarks>
    /// 수명은 씬 연결 수명이고 연결 입력 묶음(<see cref="CombatSceneInput"/>)이 함께 갖습니다. 닫기는 조정자가 배치 확인창과 같은 자리(분리·입력 정지·입력 닫힘)에서 부릅니다.
    /// 제출은 지금까지의 턴 종료 경로(Router의 EndTurn: 튜토리얼 검사 → 이동 모드 벗어남 → 대기 제출)를 그대로 지납니다.
    /// </remarks>
    internal sealed class CombatEndTurnEntry
    {
        private readonly CombatEndTurnConfirmation _confirmation;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<bool> _isInputReady;
        private readonly Func<bool, CombatInputFacts> _readInputFacts;
        private readonly CombatInputRouter _router;
        private readonly ICharacterBufferReadSink _characters;
        private readonly Action _afterSubmit;

        /// <param name="confirmation">공용 확인창입니다. 텍스트 표시가 확인창을 제공하지 않으면(시험 조립) null이고, 확인이 필요한 요청에서 오류로 보고합니다.</param>
        /// <param name="isInputReady">입력 판단에 쓸 수 있는지입니다(씬 연결 활성 ∧ 입력 슬롯 연결, 조정자의 판정식).</param>
        /// <param name="readInputFacts">종료 가능 판단에 쓸 사실 공급자입니다(인자 = 선행 미처리 명령까지 읽을지).</param>
        /// <param name="router">턴 종료·입력 정리를 넘길 Router입니다.</param>
        /// <param name="afterSubmit">확인창에서 턴 종료를 제출한 뒤 행동 버튼을 바로 맞추는 조정자의 입구입니다.</param>
        internal CombatEndTurnEntry(
            ICharacterBufferReadSink characters,
            IUIConfirmationPresentation confirmation,
            RuntimeFaultBoundary faults,
            Func<bool> isInputReady,
            Func<bool, CombatInputFacts> readInputFacts,
            CombatInputRouter router,
            Action afterSubmit)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _isInputReady = isInputReady ?? throw new ArgumentNullException(nameof(isInputReady));
            _readInputFacts = readInputFacts ?? throw new ArgumentNullException(nameof(readInputFacts));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _afterSubmit = afterSubmit ?? throw new ArgumentNullException(nameof(afterSubmit));
            _confirmation = new CombatEndTurnConfirmation(characters, confirmation, CanEndTurn, AsksConfirmation, Submit,
                CancelInputs, faults);
        }

        /// <summary>턴 종료 확인창이 열려 있는지입니다. 수신 창구와 입력 사실이 묻습니다.</summary>
        internal bool IsOpen => _confirmation.IsOpen;

        /// <summary>턴 종료 요청입니다(키·버튼). 행동하지 않은 아군이 있으면 확인창을 엽니다.</summary>
        internal void Request() => _confirmation.Request();

        /// <summary>열린 확인창이 지금 사실로 더 열려 있으면 안 되면 닫습니다. 행동 버튼을 맞출 때 부릅니다.</summary>
        internal void Sync() => _confirmation.Sync();

        /// <summary>확인창을 닫습니다. 열려 있지 않으면 아무것도 하지 않습니다.</summary>
        internal void Close() => _confirmation.Close();

        /// <summary>
        /// 턴 종료 버튼과 같은 판단입니다(선행 미처리 명령까지 읽음). 확인창이 "열린 채 둘지"를 묻는 판단이기도 해서 모달 열림은 여기서 보지 않습니다.
        /// </summary>
        private bool CanEndTurn()
        {
            if (_faults.IsFaulted || !_isInputReady()) return false;
            CombatInputFacts facts = _readInputFacts(true);
            return CombatInputPolicy.CanSubmit(facts, facts.HasPendingCommand, CombatViewCommandKind.EndPlayerTurn);
        }

        // 튜토리얼이 입력을 제한하는 동안은 묻지 않는다. 안내 흐름이 정한 턴 종료를 확인창이 가로막지 않게 한다.
        private bool AsksConfirmation() =>
            _readInputFacts(false).CombatInputRestriction == TutorialCombatInputRestriction.None;

        // 지금까지와 같은 턴 종료 경로다. 확인창 응답은 입력 경로 밖(UI 콜백)이라 제출 뒤 행동 버튼을 여기서 맞춘다(배치 진입과 같음).
        private void Submit()
        {
            _router.Route(CombatInputRouter.Request.EndTurn, 0f, 0f, _characters.Characters);
            _afterSubmit();
        }

        // 좌표 없는 명령이라 Router로만 넘긴다(배치 진입과 같음).
        private void CancelInputs() =>
            _router.Route(CombatInputRouter.Request.FocusLost, 0f, 0f, _characters.Characters);
    }
}
