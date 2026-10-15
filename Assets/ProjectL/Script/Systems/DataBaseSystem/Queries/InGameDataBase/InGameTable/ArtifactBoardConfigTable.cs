using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IArtifactBoardConfigTableQuery : IServiceModule
    {
        ArtifactBoardConfigData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class ArtifactBoardConfigTable : IArtifactBoardConfigTableQuery, IExportSystemServices
    {
        public ArtifactBoardConfigData Config { get; }

        internal ArtifactBoardConfigTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ArtifactBoardConfigDataSO dataSO || dataSO.GetData() is not ArtifactBoardConfigData data)
                    throw new InvalidOperationException("[ArtifactBoardConfigTable] Requires non-null ArtifactBoardConfigDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[ArtifactBoardConfigTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[ArtifactBoardConfigTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactBoardConfigTableQuery>(this);
        }
    }
}
