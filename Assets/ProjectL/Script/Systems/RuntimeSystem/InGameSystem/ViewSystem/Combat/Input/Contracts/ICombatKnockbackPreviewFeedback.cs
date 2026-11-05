using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 공격 조준이 호버 응답으로 넉백 미리보기 표시 의도만 전하는 좁은 계약입니다(넉백 미리보기 설계 KB1, 결정 Q-KB4 A).
    /// 공격 조준만 받고, 배치·이동·아이템 입력은 이 계약을 모릅니다. 구현: 씬 표시(<see cref="CombatSceneDisplay"/>), 시험 가짜.
    /// </summary>
    /// <remarks>
    /// 부르는 쪽은 Core 응답을 그대로 넘기고 방향·칸·모양을 계산하지 않습니다(<c>CombatKnockbackPreview</c>가 정함, 원본은 Core 넉백 기록).
    /// 구현은 멱등이고, 해제 뒤 늦게 온 호출은 무시합니다.
    /// </remarks>
    internal interface ICombatKnockbackPreviewFeedback
    {
        /// <summary>공격 가능 응답의 넉백 기록을 그립니다. 기록이 없으면 지운 것과 같습니다.</summary>
        void ShowKnockbackPreview(in AttackPreview preview);

        /// <summary>넉백 미리보기를 지웁니다. 여러 번 불려도 안전합니다.</summary>
        void ClearKnockbackPreview();
    }
}
