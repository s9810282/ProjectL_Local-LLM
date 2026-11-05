using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    public interface IItemInventoryReadSink : IServiceModule
    {
        ItemInventorySnapshot Current { get; }
    }
    internal interface IItemInventoryStore : IServiceModule
    {
        PreparedItemInventoryChange Prepare(ItemInventoryChange change);
        void Install(PreparedItemInventoryChange prepared);
        void Reset();
    }
    internal sealed class PreparedItemInventoryChange
    {
        internal ItemInventorySnapshot Final { get; }
        internal PreparedItemInventoryChange(ItemInventorySnapshot final) { Final = final; }
    }
    /// <summary>Run 재고 원본이다. 쓰기는 거래 클래스가 저장소 계약으로만 하며, 저장소 메서드는 명시적 구현으로 숨긴다.</summary>
    internal sealed class ItemInventoryBuffer : IItemInventoryReadSink, IItemInventoryStore, IExportSystemServices
    {
        public ItemInventorySnapshot Current { get; private set; } = ItemInventorySnapshot.Empty;
        PreparedItemInventoryChange IItemInventoryStore.Prepare(ItemInventoryChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (!Current.SameValues(change.Expected))
                throw new InvalidOperationException("아이템 재고의 기대값이 현재 재고와 다릅니다.");
            return new PreparedItemInventoryChange(change.Final);
        }
        void IItemInventoryStore.Install(PreparedItemInventoryChange prepared) => Current = prepared.Final;
        void IItemInventoryStore.Reset() => Current = ItemInventorySnapshot.Empty;
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IItemInventoryReadSink>(this);
            sink.AddService<IItemInventoryStore>(this);
        }
    }
}
