using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// 덱 편집 가능 시점을 Core가 묻는 계약이다. 판단은 Stage 흐름 소유자가 구현하고, 조립부가 생성 때 덱 서비스에 넘긴다.
    /// 전투 유지·Run 종료는 Core가 Buffer 사실로 먼저 막으므로 구현은 그 밖의 Stage 상호작용 창만 판단한다.
    /// null을 반환하지 않으며 <see cref="DeckUnavailableReason.AuthorityUnbound"/>는 반환하지 않는다.
    /// </summary>
    public interface IDeckEditAvailabilityRead : IServiceModule
    {
        DeckEditAvailability ReadDeckEditAvailability();
    }
}
