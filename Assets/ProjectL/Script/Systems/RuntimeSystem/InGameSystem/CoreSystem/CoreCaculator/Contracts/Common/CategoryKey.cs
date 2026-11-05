namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum CategoryKey
    {
        None = 0,
        Main = 10,
        Rider = 20,
        Heavy = 30,
        Cannon = 40,
        Boss = 50, // 전투 규칙이 없는 상성 분류. 보스 행동·능력은 유닛 정의가 따로 가진다.
    }
}