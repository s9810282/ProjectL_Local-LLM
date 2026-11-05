using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    /// <summary>
    /// 현재 Run 보유 유닛의 배치 전 능력치를 읽는 계약이다. UI에는 BufferSystem의 게시된 스냅샷만 전달한다.
    /// Core 계산용 구현은 Core 내부에서만 사용한다. 게시 전·원본 변경 직후에는 Pending이다.
    /// 전투 중에는 <see cref="CharacterStatsQueryStatus.BattleActive"/>를 돌려주며, 전투 중 능력치는 <see cref="ICharacterBufferReadSink.Characters"/>를 쓴다.
    /// </summary>
    public interface ICharacterStatsReadSink : IServiceModule
    {
        CharacterStatsQueryResult ReadOwned();
    }
}
