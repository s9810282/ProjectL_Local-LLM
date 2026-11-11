using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 이동 종류(<see cref="CombatMovementKind"/>)의 판정과 성질 원본입니다(걸음 모양 계획 §6 M1, 특성 시스템 View 계획 Tc.1). 걸음의 모양은 모양 표(<see cref="CombatStepStyles"/>)가,
    /// 걸음 사건의 종류(<see cref="Of"/>)와 그 밖의 성질은 여기 한 곳이 정합니다. 새 종류는 여기에 한 줄을 더합니다(빠뜨리면 그 종류의 첫 걸음에서 예외).
    /// </summary>
    internal static class CombatMovementKinds
    {
        /// <summary>
        /// 걸음 사건 하나의 이동 종류입니다(특성 시스템 View 설계 TS5, 계획 R4·TP-7). 판정 원본은 이 함수 하나이고, 이동 표시(<c>CombatMovementPresentation</c>)와
        /// 적 행동 간격(<c>CombatEnemyPresentationPacing</c>)이 함께 부릅니다. Core 이동 원인(<see cref="CombatCharacterEvent.MovementCause"/>)이 밀림(Forced)이면 밀림,
        /// 스스로(Voluntary)이거나 비어 있으면 스스로 이동입니다. Core는 보통 스스로 이동(뛰어넘기 포함)의 원인을 비우고, 밀린 걸음(넉백·충격 볼트·공격 강제 이동·
        /// 타일 규칙 강제 이동)에는 늘 Forced를 채웁니다(인계 §5, 계획 §3.1). 걸음 경로 사건(<c>Moved</c>)이 아니면 밀림으로 봅니다(복귀·재배치는 걸음 레인 경로가 아님).
        /// 밀림 가운데 원인 특성이 작렬(<see cref="TraitKey.CrashImpact"/>)이면 날려 감입니다(작렬 View 설계 CV5, 결정 Q-CV2 A).
        /// </summary>
        internal static CombatMovementKind Of(CombatCharacterEvent step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (step.Kind != CombatEventKind.Moved)
                return CombatMovementKind.Forced;
            CombatMovementCause cause = step.MovementCause;
            if (cause?.Agency != CombatMovementAgency.Forced)
                return CombatMovementKind.Voluntary;
            return cause.TraitKey == TraitKey.CrashImpact ? CombatMovementKind.Flung : CombatMovementKind.Forced;
        }

        /// <summary>
        /// 유닛이 스스로 하는 이동인지입니다. 참이면 걸음 전에 걸음 방향으로 돌고, 걸음 접촉 Gate가 다른 유닛의 이동을 걸음 끝까지 막고(이동 접촉 설계 M4 규칙 1),
        /// 새 행동이라 앞 행동의 재생 강조를 지우고, 경로 끝에서 원래 방향으로 돌아갑니다. 거짓(밀림)이면 넷 다 하지 않습니다.
        /// 걸음 사건(<c>CombatCharacterEvent</c>)의 성질입니다. 끌어오기 경로는 걸음 사건이 아니라 레인이 따로 정합니다(끌려가는 방향으로 돌았다가 도착 뒤 되돌림).
        /// </summary>
        internal static bool IsSelfDriven(CombatMovementKind kind) => kind switch
        {
            CombatMovementKind.Voluntary => true,
            CombatMovementKind.Forced => false,
            CombatMovementKind.Flung => false,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "[CombatMovementKinds] 성질을 정하지 않은 이동 종류입니다."),
        };
    }
}
