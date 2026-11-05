namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걸음 하나의 이동 종류입니다(걸음 모양 설계 ST1). 종류의 성질은 <see cref="CombatMovementKinds"/>가, 걸음 모양은 <see cref="CombatStepStyles"/>가 정합니다.
    /// 판정 원본은 Core 이동 원인을 읽는 <see cref="CombatMovementKinds.Of"/> 하나이고, 이동 표시(<c>CombatMovementPresentation</c>)와 적 행동 간격이 부릅니다.
    /// 장애물 건너뛰기 같은 나중 종류는 Core가 사건 사실로 알려 줄 때 값 하나, 성질 한 줄, 모양 표 한 줄을 더합니다(설계 ST6).
    /// </summary>
    internal enum CombatMovementKind
    {
        /// <summary>자발 이동(아군·적의 경로 걸음)입니다.</summary>
        Voluntary = 0,

        /// <summary>밀림(넉백·충격 볼트·공격 강제 이동, 끌어오기 레인)입니다.</summary>
        Forced = 1
    }
}
