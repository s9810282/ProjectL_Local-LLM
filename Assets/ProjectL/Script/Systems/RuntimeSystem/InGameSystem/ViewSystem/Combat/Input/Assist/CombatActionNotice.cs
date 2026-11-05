using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>입력 기능은 실패 사실만 전달하고 표시 문구·시간을 알지 않습니다.</summary>
    internal interface ICombatActionRejectionSink
    {
        void AttackRejected(AttackFailureReason reason);
        void MovementRejected(MovementFailureReason reason);
    }

    /// <summary>공개된 Core 실패 사실을 사용자 안내로 변환합니다. 규칙 판정은 하지 않습니다.</summary>
    internal sealed class CombatActionNotice : ICombatActionRejectionSink
    {
        private const float Duration = 0.5f;
        // 이유가 아니라 규칙을 설명하는 긴 문장용입니다. 0.5초로는 끝까지 읽을 수 없습니다(하붑 묶음 끝 검토 L1, 임시 값).
        private const float RuleDuration = 2f;
        private readonly IUITextPresentation _text;

        internal CombatActionNotice(IUITextPresentation text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public void AttackRejected(AttackFailureReason reason)
        {
            if (!ShowActionFailure(reason.ToString()) && IsTargetFailure(reason))
                _text.ShowNotice("공격 대상이 없거나 공격할 수 없는 위치입니다", Duration);
        }

        public void MovementRejected(MovementFailureReason reason)
        {
            ShowActionFailure(reason.ToString());
        }

        internal void MainCharacterNotPlaced() =>
            _text.ShowNotice("메인캐릭터가 배치되지 않았습니다!", Duration);

        internal void OnResult(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Accepted || result.Kind != CombatProcessingKind.Command)
                return;

            // 최종 결과 계약은 문자열 사유 코드입니다. 행동 거절 안내를 우선합니다.
            if (ShowActionFailure(result.Reason))
                return;
            else if (result.CommandKind == CombatCommandKind.EnterCombat && result.Reason == "InvalidMainCharacter")
                MainCharacterNotPlaced();
            else if (result.CommandKind == CombatCommandKind.Attack &&
                System.Enum.TryParse(result.Reason, out AttackFailureReason failure) && IsTargetFailure(failure))
                _text.ShowNotice("공격 대상이 없거나 공격할 수 없는 위치입니다", Duration);
        }

        private bool ShowActionFailure(string reason)
        {
            string message;
            float duration = Duration;
            switch (reason)
            {
                case nameof(AttackFailureReason.ActionAlreadyUsed):
                    message = "이번 턴에는 이미 행동한 유닛입니다.";
                    break;
                case nameof(AttackFailureReason.ActionDeferred):
                    message = "현재 행동이 보류된 유닛입니다.";
                    break;
                case nameof(AttackFailureReason.WrongController):
                    message = "지금은 해당 진영이 행동할 차례가 아닙니다.";
                    break;
                case nameof(AttackFailureReason.StaleTurn):
                case nameof(MovementFailureReason.StaleContext):
                    message = "상황이 바뀌었습니다. 행동을 다시 지정해주세요.";
                    break;
                // 특성·기믹이 건 행동 제한입니다(특성 시스템 View 계획 TP-9, 문구 초안).
                case nameof(AttackFailureReason.Disarmed):
                    message = "무장 해제 상태라 공격할 수 없습니다.";
                    break;
                // 투사체가 막힌다는 뜻이 아니라 지정 규칙입니다(하붑 View 설계 §3.5).
                case nameof(AttackFailureReason.HaboobDesignationBlocked):
                    message = "공격자와 지정한 칸이 모두 먼지구름 안이거나 모두 밖이어야 합니다.";
                    duration = RuleDuration;
                    break;
                // 발목 꺾기와 낙석 속박이 같은 사유라 원인을 적지 않습니다(낙석 계획 RP-11).
                case nameof(MovementFailureReason.AnkleBroken):
                    message = "이동 불가 상태라 이동할 수 없습니다.";
                    break;
                default:
                    return false;
            }
            _text.ShowNotice(message, duration);
            return true;
        }

        private static bool IsTargetFailure(AttackFailureReason reason)
        {
            switch (reason)
            {
                case AttackFailureReason.InvalidPrimaryTarget:
                case AttackFailureReason.NonHostile:
                case AttackFailureReason.OutOfBounds:
                case AttackFailureReason.InvalidAttackDirection:
                case AttackFailureReason.AmbiguousAttackDirection:
                case AttackFailureReason.InvalidAttackAim:
                case AttackFailureReason.AppointCellRejected:
                case AttackFailureReason.NoEligibleTargets:
                case AttackFailureReason.BlockedByWall:
                    return true;
                default:
                    return false;
            }
        }
    }
}
