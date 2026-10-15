using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Method.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Method.Core
{
    #region 서비스

    internal interface IMethodTableQuery : IServiceModule
    {
        MethodMasterData GetMethod(int key);
        bool TryGetMethod(int key, out MethodMasterData data);
        IReadOnlyList<MethodMasterData> GetAllMethods();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class MethodTable : IMethodTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<MethodMasterData> index;

        internal MethodTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<MethodMasterData>(source, data => data.Key);
        }

        public MethodMasterData GetMethod(int key) => index.Get(key);

        public bool TryGetMethod(int key, out MethodMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<MethodMasterData> GetAllMethods() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IMethodTableQuery>(this);
        }
    }

    #endregion
}
