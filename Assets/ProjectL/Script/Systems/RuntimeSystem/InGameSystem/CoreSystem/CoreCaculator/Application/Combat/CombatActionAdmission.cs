using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 이동·공격·가용성 조회가 공유하는 일반 행동의 입장 규칙이다.
    /// 행동 사용 상태 조건은 <see cref="UnitActionUseRules"/>가 원본이다.
    /// </summary>
    internal static class CombatActionAdmission
    {
        /// <param name="action">시작하려는 일반 행동. <see cref="CombatCommandKind.Move"/> 또는 <see cref="CombatCommandKind.Attack"/>만 받는다.</param>
        internal static MovementFailureReason Actor(CombatCommandInput input, CombatCommandKind action, out CharacterCombatFacts actor)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (action != CombatCommandKind.Move && action != CombatCommandKind.Attack)
                throw new ArgumentOutOfRangeException(nameof(action));
            actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (input.State.Phase == CombatFlowPhase.Finished) return MovementFailureReason.StageEnded;
            if (input.State.Turn.StagePhase != StagePhase.Combat) return MovementFailureReason.WrongPhase;
            bool automatic = input.ProcessingKind == CombatProcessingKind.Advance;
            var controller = automatic ? CombatControllerPhase.Enemy : CombatControllerPhase.Player;
            if (input.State.Turn.ControllerPhase != controller) return MovementFailureReason.WrongController;
            if (!automatic && (input.ExpectedBattleInstanceId != input.BattleInstanceId ||
                input.ExpectedControllerTurnSequence != input.State.Turn.ControllerTurnSequence))
                return MovementFailureReason.StaleTurn;
            if (actor == null) return MovementFailureReason.EntityNotFound;
            if (actor.Owner != (automatic ? Owner.Enemy : Owner.Player)) return MovementFailureReason.WrongController;
            if (actor.Area != CharacterRuntimeArea.Field) return MovementFailureReason.NotInCombat;
            if (actor.Health.IsDead) return MovementFailureReason.Dead;
            if (actor.ActionUse == UnitActionUse.Deferred) return MovementFailureReason.ActionDeferred;
            if (!(action == CombatCommandKind.Move
                    ? UnitActionUseRules.CanStartMove(actor.ActionUse)
                    : UnitActionUseRules.CanStartAttack(actor.ActionUse)))
                return MovementFailureReason.ActionAlreadyUsed;
            if (action == CombatCommandKind.Attack && IsAttackRestricted(actor)) return MovementFailureReason.Disarmed;
            if (action == CombatCommandKind.Move && IsMovementRestricted(actor)) return MovementFailureReason.AnkleBroken;
            return MovementFailureReason.None;
        }

        internal static bool IsAttackRestricted(CharacterCombatFacts actor) => AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Disarm);
        internal static bool IsMovementRestricted(CharacterCombatFacts actor) => AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Bind);


        internal static int? MovementAllowance(CharacterCombatFacts actor) =>
            actor.Stats.MoveRange == 0 ? (int?)null : actor.Stats.MoveRange;

        internal static int? MovementAllowance(CombatCommandInput input, CharacterCombatFacts actor) =>
            actor.Stats.MoveRange == 0 ? (int?)null : !actor.Anchor.HasValue ? actor.Stats.MoveRange : checked(actor.Stats.MoveRange + GroundCompactionTraitRule.MovementBonusAt(
                actor.Anchor.Value, input.State.Environment.VisibleTo(actor.Owner)));


        internal static UnitActionUseSettlementSnapshot Settlement(CharacterCombatFacts actor, UnitActionUse finalActionUse) =>
            new UnitActionUseSettlementSnapshot(actor.EntityId, actor.ActionUse, finalActionUse);

        internal static CombatActionAvailability GetAvailability(CombatCommandInput input)
        {
            var moveFailure = CombatMovementCalculator.Actor(input, out var actor);
            var attackFailure = CombatAttackQueries.Actor(input, out _);
            return new CombatActionAvailability(input.BattleInstanceId,input.State.Turn.ControllerTurnSequence,                input.ActorId,actor?.ActionUse ?? UnitActionUse.Deferred,                actor == null ? null : MovementAllowance(input, actor),moveFailure,attackFailure,MovementUndoCalculator.Evaluate(input));
        }
    }
}
