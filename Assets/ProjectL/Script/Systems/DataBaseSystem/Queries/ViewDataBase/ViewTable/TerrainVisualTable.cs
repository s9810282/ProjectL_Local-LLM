using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ITerrainVisualTableQuery : IServiceModule
    {
        TerrainVisualData GetTerrainVisual(TerrainContentKind kind);
        bool TryGetTerrainVisual(TerrainContentKind kind, out TerrainVisualData data);
    }

    /// <summary>지형 외형을 종류로 조회합니다. 방식별 규칙(타일 위는 프리팹 필수 등)은 외형 데이터 생성자 한 곳이 검사합니다.</summary>
    internal sealed class TerrainVisualTable : ITerrainVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<TerrainContentKind, TerrainVisualData> _dataByKind = new();

        internal TerrainVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not TerrainVisualDataSO dataSO || dataSO.GetData() is not TerrainVisualData data)
                    throw new InvalidOperationException("[TerrainVisualTable] TerrainVisualDataSO 항목이 필요합니다.");
                if (!_dataByKind.TryAdd(data.Kind, data))
                    throw new InvalidOperationException($"[TerrainVisualTable] 종류가 중복됩니다: {data.Kind}.");
            }
        }

        public TerrainVisualData GetTerrainVisual(TerrainContentKind kind)
        {
            if (_dataByKind.TryGetValue(kind, out TerrainVisualData data))
                return data;
            throw new KeyNotFoundException($"[TerrainVisualTable] 외형을 찾을 수 없습니다: {kind}.");
        }

        public bool TryGetTerrainVisual(TerrainContentKind kind, out TerrainVisualData data) =>
            _dataByKind.TryGetValue(kind, out data);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITerrainVisualTableQuery>(this);
        }
    }
}
