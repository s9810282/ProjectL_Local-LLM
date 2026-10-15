using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Render;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.View.Table;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.View.Table;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Material.View.Table;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.DBFactory.Group
{
    #region 시스템 모듈

    public static class RenderDataBaseFactory
    {
        internal static IExportSystemServices Create(RenderGroup Data)
        {
            DataBaseRegistry<IDatabaseTable> DataBaseGrop = new(Data);
            SystemBuilderHelper systemBuilderHelper = new();

            EffectTable effectTable = new EffectTable(DataBaseGrop.GetDataBase<EffectDataTableSO>());
            effectTable.ExportServices(systemBuilderHelper);

            LightTable lightTable = new LightTable(DataBaseGrop.GetDataBase<LightDataTableSO>());
            lightTable.ExportServices(systemBuilderHelper);

            MetrialTable metrialTable = new MetrialTable(DataBaseGrop.GetDataBase<MaterialDataTableSO>());
            metrialTable.ExportServices(systemBuilderHelper);

            return new RenderDataBase(systemBuilderHelper.BuildSystemReader());
        }
    }

    #endregion
}
