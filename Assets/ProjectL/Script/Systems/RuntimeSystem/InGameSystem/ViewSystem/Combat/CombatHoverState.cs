namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 포인터가 올라간 칸의 표시 상태입니다(설계 §21.1). 가능 여부는 Core 응답으로만 정하고 View가 계산하지 않습니다.
    /// </summary>
    internal enum CombatHoverState
    {
        /// <summary>판단 전이거나 판단이 필요 없는 칸입니다.</summary>
        Neutral = 0,
        Valid = 1,
        Invalid = 2
    }
}
