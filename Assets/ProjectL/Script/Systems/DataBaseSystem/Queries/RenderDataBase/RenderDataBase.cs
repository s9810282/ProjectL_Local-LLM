using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase
{
    #region 서비스

    internal interface IHasEffectQuery : IServiceModule
    {
        IEffectTableQuery EffectTableQuery { get; }
    }

    internal interface IHasMaterialQuery : IServiceModule
    {
        IMetrialTableQuery MetrialTableQuery { get; }
    }

    internal interface IHasLightQuery : IServiceModule
    {
        ILightTableQuery LightTableQuery { get; }
    }

    internal interface IRenderQuery : IHasEffectQuery, IHasMaterialQuery, IHasLightQuery { }

    #endregion

    #region 시스템 모듈

    internal class RenderDataBase : SystemGroup, IRenderQuery
    {
        public IEffectTableQuery EffectTableQuery { get; }

        public IMetrialTableQuery MetrialTableQuery { get; }

        public ILightTableQuery LightTableQuery { get; }

        public RenderDataBase(ISystemReader systemReader) : base(systemReader)
        {
            EffectTableQuery = systemReader.ServiceStoreReader.GetService(RenderTableKey.Effect);

            LightTableQuery = systemReader.ServiceStoreReader.GetService(RenderTableKey.Light);

            MetrialTableQuery = systemReader.ServiceStoreReader.GetService(RenderTableKey.Metrial);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRenderQuery>(this);
        }
    }

    #endregion
}
