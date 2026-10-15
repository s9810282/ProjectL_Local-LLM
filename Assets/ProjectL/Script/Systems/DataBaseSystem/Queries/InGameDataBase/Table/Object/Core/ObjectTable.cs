using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Object.Core
{
    #region 서비스

    internal interface IObjectTableQuery : IServiceModule
    {
        ObjectMasterData GetObject(int key);
        bool TryGetObject(int key, out ObjectMasterData data);
        IReadOnlyList<ObjectMasterData> GetAllObjects();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ObjectTable : IObjectTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<ObjectMasterData> index;

        internal ObjectTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<ObjectMasterData>(source, data => data.Key);
        }

        public ObjectMasterData GetObject(int key) => index.Get(key);

        public bool TryGetObject(int key, out ObjectMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<ObjectMasterData> GetAllObjects() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IObjectTableQuery>(this);
        }
    }

    #endregion
}
