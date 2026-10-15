using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Item.Core
{
    #region 서비스

    internal interface IItemTableQuery : IServiceModule
    {
        ItemMasterData GetItem(int key);
        bool TryGetItem(int key, out ItemMasterData data);
        IReadOnlyList<ItemMasterData> GetAllItems();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ItemTable : IItemTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<ItemMasterData> index;

        internal ItemTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<ItemMasterData>(source, data => data.Key);
        }

        public ItemMasterData GetItem(int key) => index.Get(key);

        public bool TryGetItem(int key, out ItemMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<ItemMasterData> GetAllItems() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IItemTableQuery>(this);
        }
    }

    #endregion
}
