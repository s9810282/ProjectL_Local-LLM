using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IStageNodeVisualTableQuery : IServiceModule
    {
        StageNodeVisualData GetNodeVisual(StageType stageType, StageMapRole mapRole);
        bool TryGetNodeVisual(StageType stageType, StageMapRole mapRole, out StageNodeVisualData data);
        IReadOnlyCollection<StageNodeVisualData> GetAllNodeVisuals();
    }

    /// <summary>노드 외형을 (StageType, StageMapRole) 기준으로 제공합니다.</summary>
    internal sealed class StageNodeVisualTable : IStageNodeVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<(StageType, StageMapRole), StageNodeVisualData> _dataByKey = new();

        internal StageNodeVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageNodeVisualDataSO dataSO || dataSO.GetData() is not StageNodeVisualData data)
                    throw new InvalidOperationException("[StageNodeVisualTable] Requires non-null StageNodeVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd((data.StageType, data.MapRole), data))
                    throw new InvalidOperationException(
                        $"[StageNodeVisualTable] Duplicate key: ({data.StageType}, {data.MapRole}).");
            }
        }

        public StageNodeVisualData GetNodeVisual(StageType stageType, StageMapRole mapRole)
        {
            if (_dataByKey.TryGetValue((stageType, mapRole), out StageNodeVisualData data))
                return data;

            throw new KeyNotFoundException($"[StageNodeVisualTable] Node visual not found. Key: ({stageType}, {mapRole}).");
        }

        public bool TryGetNodeVisual(StageType stageType, StageMapRole mapRole, out StageNodeVisualData data)
        {
            return _dataByKey.TryGetValue((stageType, mapRole), out data);
        }

        public IReadOnlyCollection<StageNodeVisualData> GetAllNodeVisuals() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageNodeVisualTableQuery>(this);
        }
    }
}
