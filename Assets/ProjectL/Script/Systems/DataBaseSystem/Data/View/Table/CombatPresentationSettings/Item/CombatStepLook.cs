namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>
    /// 자발 이동(경로 걸음)의 걸음 모양입니다(걸음 모양 설계 ST4). 어느 이동이 이 값을 쓰는지는 View 걸음 모양 표(<c>CombatStepStyles</c>)가 정합니다.
    /// </summary>
    internal enum CombatStepLook
    {
        /// <summary>칸 사이를 걸어 옮깁니다. 높이가 다른 칸 사이에서만 살짝 뜁니다(설정 <c>stepClimbHop</c>).</summary>
        Walk = 0,

        /// <summary>칸마다 한 번씩 뛰어 옮깁니다. 높이·시간은 설정 <c>hopHeight</c>·<c>hopSeconds</c>입니다.</summary>
        Hop = 1
    }
}
