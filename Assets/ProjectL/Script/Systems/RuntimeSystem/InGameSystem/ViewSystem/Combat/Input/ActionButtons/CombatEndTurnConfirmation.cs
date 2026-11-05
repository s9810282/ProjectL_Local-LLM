using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 아직 행동하지 않은 아군이 있으면 턴 종료 확인을 거치며, 확정 때 현재 입력 상태를 다시 검사합니다.
    /// 배치 확인창(<see cref="CombatDeploymentConfirmation"/>)과 같은 공용 확인창을 씁니다.
    /// </summary>
    /// <remarks>
    /// "행동하지 않음"은 Core가 내 준 행동 사용 상태가 미사용(<see cref="UnitActionUse.Available"/>)인 것입니다. 이동만 한 유닛은 세지 않습니다.
    /// 상태 이상 등으로 실제로 할 수 있는 행동이 있는지는 Core 판단이라 보지 않습니다. 확인창은 명령 전 안내일 뿐이고 Core 규칙은 그대로입니다.
    /// </remarks>
    internal sealed class CombatEndTurnConfirmation : IUIConfirmationReceiver
    {
        private readonly ICharacterBufferReadSink _characters;
        private readonly IUIConfirmationPresentation _presentation;
        private readonly Func<bool> _canEndTurn;
        private readonly Func<bool> _asksConfirmation;
        private readonly Action _endTurn;
        private readonly Action _cancelInputs;
        private readonly RuntimeFaultBoundary _faults;

        /// <param name="canEndTurn">지금 턴 종료를 제출할 수 있는지입니다. 요청·확정·열린 채 두기 판단이 함께 씁니다.</param>
        /// <param name="asksConfirmation">이 요청에 확인을 물어도 되는지입니다(튜토리얼 입력 제한 중에는 묻지 않음).</param>
        /// <param name="endTurn">턴 종료 입력을 기존 경로(Router)로 넘깁니다.</param>
        internal CombatEndTurnConfirmation(ICharacterBufferReadSink characters, IUIConfirmationPresentation presentation,
            Func<bool> canEndTurn, Func<bool> asksConfirmation, Action endTurn, Action cancelInputs, RuntimeFaultBoundary faults)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            // 표시를 쓰지 않는 시험 조립은 생략할 수 있지만 확인이 필요한 요청에서는 누락을 오류로 보고합니다(배치 확인창과 같음).
            _presentation = presentation;
            _canEndTurn = canEndTurn ?? throw new ArgumentNullException(nameof(canEndTurn));
            _asksConfirmation = asksConfirmation ?? throw new ArgumentNullException(nameof(asksConfirmation));
            _endTurn = endTurn ?? throw new ArgumentNullException(nameof(endTurn));
            _cancelInputs = cancelInputs ?? throw new ArgumentNullException(nameof(cancelInputs));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        internal bool IsOpen => _presentation?.IsConfirmationOpen(this) ?? false;

        /// <summary>필드에 살아 있는 아군 중 이번 턴에 아직 아무 행동도 하지 않은 유닛의 수입니다.</summary>
        internal int CountIdleUnits()
        {
            int count = 0;
            var characters = _characters.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.Owner == Owner.Player && character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead
                    && character.ActionUse == UnitActionUse.Available)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 턴 종료 요청입니다(키·버튼). 제출할 수 없는 요청은 기존 경로로 그대로 넘겨 거절 안내·튜토리얼 막힘 보고를 지금처럼 받습니다.
        /// 제출할 수 있고 행동하지 않은 아군이 있으면 확인창을 엽니다.
        /// </summary>
        internal void Request()
        {
            if (IsOpen) return;
            int idle = _canEndTurn() && _asksConfirmation() ? CountIdleUnits() : 0;
            if (idle == 0)
            {
                _endTurn();
                return;
            }
            if (_presentation == null)
                throw new InvalidOperationException("[CombatEndTurnConfirmation] 턴 종료 확인에 IUIConfirmationPresentation이 필요합니다.");
            _cancelInputs();
            if (_canEndTurn())
                _presentation.ShowConfirmation($"아직 행동하지 않은 유닛이 {idle}명 있습니다. 턴을 종료할까요?", this);
        }

        public void Respond(bool accepted)
        {
            try
            {
                if (accepted && _canEndTurn()) _endTurn();
            }
            catch (Exception error) { _faults.Report(error); }
        }

        internal void Sync()
        {
            if (IsOpen && !_canEndTurn()) Close();
        }

        internal void Close() => _presentation?.HideConfirmation(this);
    }
}
