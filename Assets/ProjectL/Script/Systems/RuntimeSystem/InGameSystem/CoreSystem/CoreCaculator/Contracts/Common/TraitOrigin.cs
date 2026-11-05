namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 유닛이 그 특성을 왜 가졌는지다(특성 표시 출처 설계 TO1). 표시 순서도 이 값 순서다(결정 Q-TO1 A).
    /// 계산에는 쓰지 않는다.
    /// </summary>
    public enum TraitOrigin
    {
        None = 0,
        /// <summary>유닛 기본: DB 유닛 정의의 특성.</summary>
        Unit = 1,
        /// <summary>병과 고유: 현재 전투 병과의 기본 특성. 같은 키를 유닛이 원래 갖거나 성장으로 얻었어도 이 값이다(결정 Q-TO2·Q-TO3 A).</summary>
        Category = 2,
        /// <summary>레벨업 획득: 성장에서 획득(Acquire)한 특성.</summary>
        Growth = 3,
    }
}
