using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 진행 중인 Run에 재고 지급을 게시하는 거래다. 지급 가능 판정과 수량 계산은 Core가 하고,
    /// 여기서는 Run이 진행 중인지와 계산 때 읽은 재고가 그대로인지만 확인한 뒤 설치한다.
    /// 같은 재고 저장소를 쓰지만 새 Run 초기화는 ProgressTransactions, 장착·회수 게시는 BattleTransactions가 맡는다.
    /// </summary>
    internal sealed class ItemInventoryTransactions : IItemInventoryBufferWriteSink
    {
        private readonly BufferGuards _guards;
        private readonly IItemInventoryStore _items;

        internal ItemInventoryTransactions(BufferGuards guards, IItemInventoryStore items)
        {
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _items = items ?? throw new System.ArgumentNullException(nameof(items));
        }

        public void PublishGrant(ItemInventoryChange change)
        {
            if (change == null) throw new System.ArgumentNullException(nameof(change));
            _guards.EnsureRunInProgress();
            // 기대값이 현재 재고와 다르면 저장소 준비 단계가 거부하므로 부분 설치가 없다.
            _items.Install(_items.Prepare(change));
        }
    }
}
