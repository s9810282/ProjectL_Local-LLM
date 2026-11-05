namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 특성 발동의 유닛 행동을 누가 하는지입니다(유닛 행동 설계 AA1). <see cref="None"/>이면 그 발동 줄에는 행동이 없습니다.
    /// </summary>
    internal enum CombatUnitActionActor
    {
        /// <summary>행동이 없습니다(기본값). 큐·추가 공격만 냅니다.</summary>
        None = 0,

        /// <summary>발동 유닛(발동 사실의 <c>SourceUnitId</c>)이 합니다. 가로채기(대신 맞기)에서는 수호자입니다.</summary>
        Source = 1,

        /// <summary>발동 대상(발동 사실의 <c>TargetIds</c>)이 각자 합니다.</summary>
        Targets = 2
    }
}
