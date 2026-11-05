using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>공개된 행동 사용 상태와 Core 거절 사유를 짧은 문구로 바꿉니다. 규칙을 판정하지 않습니다.</summary>
    internal static class CombatActionStateText
    {
        internal static string Format(UnitActionUse actionUse, string failure)
        {
            string state;
            switch (actionUse)
            {
                case UnitActionUse.Available: state = "미사용"; break;
                case UnitActionUse.Moved: state = "이동함"; break;
                case UnitActionUse.Attacked: state = "공격함"; break;
                case UnitActionUse.Deferred: state = "보류"; break;
                default: throw new ArgumentOutOfRangeException(nameof(actionUse), actionUse, "알 수 없는 행동 상태입니다.");
            }
            string reason = DescribeFailure(failure);
            return state +
                (reason.Length == 0 ? string.Empty : "\n" + reason);
        }

        private static string DescribeFailure(string failure)
        {
            switch (failure)
            {
                case null:
                case "":
                case nameof(AttackFailureReason.None): return string.Empty;
                case nameof(AttackFailureReason.ActionAlreadyUsed): return "행동사용";
                case nameof(AttackFailureReason.ActionDeferred): return "행동보류";
                case nameof(AttackFailureReason.WrongController): return "다른진영";
                case nameof(AttackFailureReason.StaleTurn):
                case nameof(MovementFailureReason.StaleContext): return "재확인";
                case nameof(AttackFailureReason.BasicAttackDisabled): return "공격없음";
                case nameof(MovementFailureReason.VoluntaryMoveDisabled):
                case nameof(MovementFailureReason.UnsupportedMoveMethod): return "이동불가";
                case nameof(AttackFailureReason.WrongPhase): return "단계대기";
                case nameof(AttackFailureReason.Dead): return "사망";
                case nameof(AttackFailureReason.NotInCombat): return "필드밖";
                case nameof(AttackFailureReason.StageEnded): return "전투종료";
                // 특성·기믹이 건 행동 제한입니다(특성 시스템 View 계획 TP-9). 이동 거절의 무장 해제도 같은 이름입니다.
                case nameof(AttackFailureReason.Disarmed): return "무장해제";
                // 이동 제한의 사유 하나입니다. 낙석 속박도 이 사유라 특성 이름을 쓰지 않습니다(낙석 계획 RP-11).
                case nameof(MovementFailureReason.AnkleBroken): return "이동불가";
                default: return "사용불가";
            }
        }
    }
}
