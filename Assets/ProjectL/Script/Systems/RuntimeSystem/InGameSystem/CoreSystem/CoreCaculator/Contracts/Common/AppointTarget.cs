namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum AppointTarget
    {
        None = 0,
        NearestSingle = 1, // 가장 가까운 대상 하나
        FarthestSingle = 2, // 가장 먼 대상 하나
        NearestAll = 3, // 가장 가까운 거리에 있는 대상 모두
        FarthestAll = 4, // 가장 먼 거리에 있는 대상 모두
        AllInRange = 5, // 지정 범위 안의 대상 모두
    }
}