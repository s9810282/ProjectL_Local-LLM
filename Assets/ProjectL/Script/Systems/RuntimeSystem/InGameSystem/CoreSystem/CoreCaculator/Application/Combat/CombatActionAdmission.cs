using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
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
    /// 행동 가능 여부를 물을 수 있는 유닛의 자격(진영·필드·생존)은 <see cref="EligibilityFailure"/>가 원본이다(새 SSOT 설계 SN2).
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
            var eligibility = EligibilityFailure(actor, automatic ? Owner.Enemy : Owner.Player);
            if (eligibility != MovementFailureReason.None) return eligibility;
            if (actor.ActionUse == UnitActionUse.Deferred) return MovementFailureReason.ActionDeferred;
            if (!(action == CombatCommandKind.Move
                    ? UnitActionUseRules.CanStartMove(actor.ActionUse)
                    : UnitActionUseRules.CanStartAttack(actor.ActionUse)))
                return MovementFailureReason.ActionAlreadyUsed;
            if ((action == CombatCommandKind.Attack || action == CombatCommandKind.Move) && ReconstructionTraitRule.IsReconstructing(actor))
                return MovementFailureReason.Reconstructing;
            if (IsStunned(actor)) return MovementFailureReason.Stunned;
            if (action == CombatCommandKind.Attack && IsAttackRestricted(actor)) return MovementFailureReason.Disarmed;
            if (action == CombatCommandKind.Move && IsMovementRestricted(actor)) return MovementFailureReason.AnkleBroken;
            return MovementFailureReason.None;
        }

        /// <summary>
        /// 행동 가능 여부를 물을 수 있는 유닛의 자격 원본이다(새 SSOT 설계 SN2). 진영 → 필드 → 생존 순으로 보고 처음 어긋난 이유를 돌려준다.
        /// 행위자 검사(<see cref="Actor"/>)와 묶음 조회(<see cref="GetAvailabilities"/>)가 함께 부른다.
        /// </summary>
        internal static MovementFailureReason EligibilityFailure(CharacterCombatFacts actor, Owner owner)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (actor.Owner != owner) return MovementFailureReason.WrongController;
            if (actor.Area != CharacterRuntimeArea.Field) return MovementFailureReason.NotInCombat;
            if (actor.Health.IsDead) return MovementFailureReason.Dead;
            return MovementFailureReason.None;
        }

        internal static bool IsStunned(CharacterCombatFacts actor) =>
            AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Stun);

        /// <summary>무장 해제·기절·재구성에 따른 공격 제한을 판정한다.</summary>
        internal static bool IsAttackRestricted(CharacterCombatFacts actor) =>
            IsStunned(actor) || AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Disarm) ||
            ReconstructionTraitRule.IsReconstructing(actor);

        /// <summary>속박·기절·재구성에 따른 이동 제한을 판정한다.</summary>
        internal static bool IsMovementRestricted(CharacterCombatFacts actor) =>
            IsStunned(actor) || AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Bind) ||
            ReconstructionTraitRule.IsReconstructing(actor);


        internal static int? MovementAllowance(CombatCommandInput input, CharacterCombatFacts actor) =>
            VoluntaryMovementRules.Allowance(actor, actor.Anchor.HasValue ? input.State.Environment.VisibleTo(actor.Owner) : null);


        internal static UnitActionUseSettlementSnapshot Settlement(CharacterCombatFacts actor, UnitActionUse finalActionUse) =>
            new UnitActionUseSettlementSnapshot(actor.EntityId, actor.ActionUse, finalActionUse);

        internal static CombatActionAvailability GetAvailability(CombatCommandInput input)
        {
            var moveFailure = CombatMovementCalculator.Actor(input, out var actor);
            var attackFailure = CombatAttackQueries.Actor(input, out _);
            return new CombatActionAvailability(input.BattleInstanceId,input.State.Turn.ControllerTurnSequence,                input.ActorId,actor?.ActionUse ?? UnitActionUse.Deferred,                actor == null ? null : MovementAllowance(input, actor),moveFailure,attackFailure,MovementUndoCalculator.Evaluate(input));
        }

        /// <summary>
        /// 자격 아군(<see cref="EligibilityFailure"/>가 아군으로 이유 없음) 모두의 행동 가능 여부를 입력 유닛 순으로 모은다(새 SSOT 설계 SN2).
        /// 입력은 한 번 조립하고 행위자만 바꿔(<see cref="CombatCommandInput.WithActor"/>) 단일 조회와 같은 답을 낸다.
        /// 행동을 이미 쓴 유닛·보류 유닛도 든다(답이 "못 함"이다).
        /// </summary>
        internal static IReadOnlyList<CombatActionAvailability> GetAvailabilities(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var availabilities = new List<CombatActionAvailability>();
            foreach (CharacterCombatFacts character in input.Characters)
            {
                if (EligibilityFailure(character, Owner.Player) == MovementFailureReason.None)
                    availabilities.Add(GetAvailability(input.WithActor(character.EntityId)));
            }
            return availabilities;
        }
    }
}
