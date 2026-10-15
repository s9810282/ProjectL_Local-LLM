using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Framework.DataBase
{
    public abstract class DataTableBase<TData> : IExportSystemServices
        where TData : IData
    {
        public DataTableBase(IDataProvider<IData> datas) { }

        #region 시스템 모듈

        public abstract void ExportServices(ISystemWriteSink sink);

        #endregion
    }
}
