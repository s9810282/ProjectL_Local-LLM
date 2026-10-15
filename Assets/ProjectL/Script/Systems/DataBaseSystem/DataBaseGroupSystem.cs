using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem
{
    #region 서비스

    internal interface IHasRenderQuery : IServiceModule
    {
        IRenderQuery RenderQuery { get; }
    }

    internal interface IHasInGameQuery : IServiceModule
    {
        IInGameQuery InGameQuery { get; }
    }

    internal interface IHasViewQuery : IServiceModule
    {
        IViewQuery ViewQuery { get; }
    }

    /// <summary>
    /// 아티팩트 보드 처음 크기 설정만 노출합니다. IInGameQuery에 넣지 않아 테스트용 InGame 조회 구현체에 영향을 주지 않습니다.
    /// 이 설정을 쓰는 쪽은 DB가 이 인터페이스를 구현하지 않을 수 있음을 처리해야 합니다(아티팩트 팝업 계획 D7).
    /// </summary>
    internal interface IHasArtifactBoardConfig : IServiceModule
    {
        IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; }
    }

    internal interface IDataBaseQueryContext : IHasRenderQuery, IHasInGameQuery, IHasViewQuery { }

    #endregion

    #region 시스템 모듈

    internal class DataBaseGroupSystem : SystemGroup, IDataBaseQueryContext, IHasArtifactBoardConfig
    {
        public IRenderQuery RenderQuery { get; }
        public IInGameQuery InGameQuery { get; }
        public IViewQuery ViewQuery { get; }
        public IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; }

        public DataBaseGroupSystem(ISystemReader systemReader) : base(systemReader)
        {
            RenderQuery = SystemReader.ServiceStoreReader.GetService(DataBaseGroupKey.Render);
            InGameQuery = SystemReader.ServiceStoreReader.GetService(DataBaseGroupKey.InGame);
            ViewQuery = SystemReader.ServiceStoreReader.GetService(DataBaseGroupKey.View);
            ArtifactBoardConfig = SystemReader.ServiceStoreReader.GetService(DataBaseGroupKey.ArtifactBoardConfig).ArtifactBoardConfig;
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IDataBaseQueryContext>(this);
        }
    }

    #endregion
}
