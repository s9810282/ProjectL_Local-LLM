using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Description.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Description.Core
{
    #region 서비스

    internal interface IDescriptionTableQuery : IServiceModule
    {
        DescriptionMasterData GetDescription(int key);
        bool TryGetDescription(int key, out DescriptionMasterData data);
        IReadOnlyList<DescriptionMasterData> GetAllDescriptions();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class DescriptionTable : IDescriptionTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<DescriptionMasterData> index;

        internal DescriptionTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<DescriptionMasterData>(source, data => data.Key);
        }

        public DescriptionMasterData GetDescription(int key) => index.Get(key);

        public bool TryGetDescription(int key, out DescriptionMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<DescriptionMasterData> GetAllDescriptions() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IDescriptionTableQuery>(this);
        }
    }

    #endregion
}
