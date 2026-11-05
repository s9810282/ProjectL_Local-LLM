using System;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ItemSystem
{
    public interface IItemInventoryService : IServiceModule
    {
        ItemGrantResult Grant(int itemKey, int quantity);
    }
    internal sealed class ItemInventoryService : IItemInventoryService, ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private IReadOnlyList<ItemDefinition> _definitions;
        private readonly bool _usesDatabase;
        internal ItemInventoryService()
        {
            _usesDatabase = true;
        }

        public void InitData(IDataBaseQueryContext data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (_usesDatabase)
            {
                var table = (data.InGameQuery as IHasConsumableItemTable)?.ConsumableItems;
                // 아이템 없는 개발 정의는 유지하되, 실제 지급은 정의가 없으면 변경 전에 거절한다.
                _definitions = table == null ? null : ItemDefinitionAdapter.Build(table.GetAll());
            }
        }
        // 바인딩 때 필요한 Sink만 받는다. 범용 IBufferQuery는 보관하지 않는다.
        private IItemInventoryReadSink _inventory;
        private IRunProgressBufferReadSink _run;
        private IItemInventoryBufferWriteSink _inventoryWrite;
        internal ItemInventoryService(IReadOnlyList<ItemDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            _definitions = Array.AsReadOnly(definitions.ToArray());
            if (_definitions.Any(d => d == null) || _definitions.Select(d => d.Key).Distinct().Count() != _definitions.Count)
                throw new InvalidOperationException("아이템 정의에는 null 또는 중복 키를 넣을 수 없습니다.");
        }
        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _inventory = buffers.ItemInventoryReadSink;
            _run = buffers.RunProgressReadSink;
            _inventoryWrite = buffers.ItemInventoryWriteSink;
        }

        /// <summary>지급 가능 판정과 수량 계산은 여기서 하고, 결과는 Buffer의 재고 지급 거래로 게시한다.</summary>
        public ItemGrantResult Grant(int itemKey, int quantity)
        {
            if (_definitions == null) throw new InvalidOperationException("선택된 아이템 정의 테이블이 필요합니다.");
            if (_inventoryWrite == null) throw new InvalidOperationException("아이템 서비스의 Buffer 바인딩이 필요합니다.");
            if (_run.Outcome != RunOutcome.InProgress)
                return new ItemGrantResult(false, "RunEnded", null);
            var result = ItemInventoryCalculator.Grant(_inventory.Current, _definitions, itemKey, quantity);
            if (result.Accepted) _inventoryWrite.PublishGrant(result.Change);
            return result;
        }
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IItemInventoryService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
