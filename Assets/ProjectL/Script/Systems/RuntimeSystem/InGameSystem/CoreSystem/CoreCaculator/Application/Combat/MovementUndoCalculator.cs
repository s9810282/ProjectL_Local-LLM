using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class MovementUndoCalculator
    {
        public static MovementUndoAvailability Evaluate(CombatCommandInput input) => MovementEffectUndo.Evaluate(input, out _, out _);

        public static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Kind != CombatCommandKind.UndoMove) return CombatCommandCalculator.Reject(input, "UnsupportedCommand");
            var availability = MovementEffectUndo.Evaluate(input, out var checkpoint, out var candidate);
            if (!availability.CanUndo) return CombatCommandCalculator.Reject(input, availability.Failure.ToString());
            if (input.ExpectedMoveCommandNumber != checkpoint.MoveCommandNumber)
                return CombatCommandCalculator.Reject(input, MovementUndoFailureReason.StaleToken.ToString());
            return candidate;
        }
    }
}
