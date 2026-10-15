namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 타일의 회전 방향을 나타내는 열거형입니다. 4방향 회전을 지원하며, 각 방향은 90도 단위로 회전합니다. (시계 방향)
    /// </summary>
    public enum Rotation4 : byte
    {
        R0 = 0,
        R90 = 1,
        R180 = 2,
        R270 = 3,
    }
}
