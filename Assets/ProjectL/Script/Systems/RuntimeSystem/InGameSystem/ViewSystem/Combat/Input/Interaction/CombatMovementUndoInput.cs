using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>현재 선택의 최신 Core 응답만 사용하는 이동 되돌리기 입력입니다.</summary>
    internal sealed class CombatMovementUndoInput
    {
        private readonly CombatActionAvailabilityTracker _availability;
        private readonly CombatCommandGate _gate;
        private readonly Func<bool, CombatInputFacts> _readFacts;
        private readonly Func<bool> _hasForeground;

        internal CombatMovementUndoInput(CombatActionAvailabilityTracker availability, CombatCommandGate gate,
            Func<bool, CombatInputFacts> readFacts, Func<bool> hasForeground)
        {
            _availability = availability ?? throw new ArgumentNullException(nameof(availability));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _hasForeground = hasForeground ?? throw new ArgumentNullException(nameof(hasForeground));
        }

        internal bool CanRequest => ReadAnswer(_readFacts(true)) != null;

        /// <summary>버튼과 키가 같은 최신 응답으로 한 번 제출합니다. 대기는 기존 Gate가 소유합니다.</summary>
        internal bool Request()
        {
            CombatInputFacts facts = _readFacts(true);
            CombatActionAvailability answer = ReadAnswer(facts);
            if (answer == null)
                return false;

            return _gate.SubmitPending(facts, CombatViewCommandData.UndoMove(answer.ActorId,
                answer.BattleInstanceId, answer.ControllerTurnSequence, answer.MovementUndo.MoveCommandNumber));
        }

        private CombatActionAvailability ReadAnswer(in CombatInputFacts facts)
        {
            if (!facts.IsInputAvailable || facts.HasUnhandledCommand || _hasForeground()
                || !CombatInputPolicy.CanSubmit(facts, _gate.HasPending, CombatViewCommandKind.UndoMove))
                return null;

            CombatActionAvailability answer = _availability.Read(facts);
            return answer?.MovementUndo.CanUndo == true && answer.MovementUndo.MoveCommandNumber > 0 ? answer : null;
        }
    }
}
