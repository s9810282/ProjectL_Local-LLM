using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IArtifactViewTableQuery : IServiceModule
    {
        ArtifactViewData GetArtifactView(int artifactId);

        bool TryGetArtifactView(
            int artifactId,
            out ArtifactViewData data);

        IReadOnlyCollection<ArtifactViewData> GetAllArtifactViews();
    }

    /// <summary>
    /// 아티팩트 표현 데이터를 ArtifactId로 조회합니다.
    /// </summary>
    internal sealed class ArtifactViewTable :
        IArtifactViewTableQuery,
        IExportSystemServices
    {
        private readonly Dictionary<int, ArtifactViewData>
            _dataById = new();

        internal ArtifactViewTable(
            IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ArtifactViewDataSO dataSO ||
                    dataSO.GetData() is not ArtifactViewData data)
                {
                    throw new InvalidOperationException(
                        "[ArtifactViewTable] 유효한 ArtifactViewDataSO가 필요합니다.");
                }

                data.Validate();

                if (!_dataById.TryAdd(data.ArtifactId, data))
                {
                    throw new InvalidOperationException(
                        $"[ArtifactViewTable] 중복 ID: {data.ArtifactId}");
                }
            }
        }

        public ArtifactViewData GetArtifactView(int artifactId)
        {
            if (_dataById.TryGetValue(
                artifactId,
                out ArtifactViewData data))
            {
                return data;
            }

            throw new KeyNotFoundException(
                $"[ArtifactViewTable] 표현 데이터가 없습니다. ID: {artifactId}");
        }

        public bool TryGetArtifactView(
            int artifactId,
            out ArtifactViewData data)
        {
            return _dataById.TryGetValue(artifactId, out data);
        }

        public IReadOnlyCollection<ArtifactViewData>
            GetAllArtifactViews()
        {
            return _dataById.Values;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactViewTableQuery>(this);
        }
    }
}