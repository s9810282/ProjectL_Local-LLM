using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IArtifactDefinitionTableQuery : IServiceModule
    {
        ArtifactDefinitionData GetDefinition(int artifactId);

        bool TryGetDefinition(
            int artifactId,
            out ArtifactDefinitionData data);

        IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions();
    }

    /// <summary>
    /// 아티팩트 규칙 데이터를 ArtifactId로 조회합니다.
    /// </summary>
    internal sealed class ArtifactDefinitionTable :
        IArtifactDefinitionTableQuery,
        IExportSystemServices
    {
        private readonly Dictionary<int, ArtifactDefinitionData>
            _dataById = new();

        internal ArtifactDefinitionTable(
            IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ArtifactDefinitionDataSO dataSO ||
                    dataSO.GetData() is not ArtifactDefinitionData data)
                {
                    throw new InvalidOperationException(
                        "[ArtifactDefinitionTable] 유효한 ArtifactDefinitionDataSO가 필요합니다.");
                }

                // 규칙 데이터는 GetData()에서 생성될 때
                // 기존 ArtifactDefinition 생성자가 검증합니다.
                if (!_dataById.TryAdd(data.ArtifactId, data))
                {
                    throw new InvalidOperationException(
                        $"[ArtifactDefinitionTable] 중복 ID: {data.ArtifactId}");
                }
            }
        }

        public ArtifactDefinitionData GetDefinition(int artifactId)
        {
            if (_dataById.TryGetValue(
                artifactId,
                out ArtifactDefinitionData data))
            {
                return data;
            }

            throw new KeyNotFoundException(
                $"[ArtifactDefinitionTable] 아티팩트가 없습니다. ID: {artifactId}");
        }

        public bool TryGetDefinition(
            int artifactId,
            out ArtifactDefinitionData data)
        {
            return _dataById.TryGetValue(artifactId, out data);
        }

        public IReadOnlyCollection<ArtifactDefinitionData>
            GetAllDefinitions()
        {
            return _dataById.Values;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactDefinitionTableQuery>(this);
        }
    }
}