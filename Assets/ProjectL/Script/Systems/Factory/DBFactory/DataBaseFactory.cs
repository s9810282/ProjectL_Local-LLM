using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.Render;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.DBFactory
{
    #region 시스템 모듈

    public static class DataBaseFactory
    {
        public static IServiceModule Create(DatabaseCatalogSO Data)
        {
            DataBaseRegistry<IDatabaseGroup> DataBaseGrop = new(Data);
            SystemBuilderHelper systemBuilderHelper = new();

            var RenderDataBase = RenderDataBaseFactory.Create(DataBaseGrop.GetDataBase<RenderGroup>());
            RenderDataBase.ExportServices(systemBuilderHelper);

            var inGameDataBase = InGameDataBaseFactory.Create(
                DataBaseGrop.GetDataBase<InGameGroup>());
            inGameDataBase.ExportServices(systemBuilderHelper);

            var viewDataBase = ViewDataBaseFactory.Create(
                DataBaseGrop.GetDataBase<ViewGroup>());
            viewDataBase.ExportServices(systemBuilderHelper);

            return new DataBaseGroupSystem(systemBuilderHelper.BuildSystemReader());
        }
    }

    #endregion
}
