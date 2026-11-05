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
        StageNodeVisualData GetNodeVisual(int stageKey, StageType stageType, StageMapRole mapRole);
        bool TryGetNodeVisual(int stageKey, StageType stageType, StageMapRole mapRole, out StageNodeVisualData data);
        IReadOnlyCollection<StageNodeVisualData> GetAllNodeVisuals();
    }

    /// <summary>StageKey 지정 외형을 우선하고, 미지정 키에는 종류·역할의 공통 외형을 제공합니다.</summary>
    internal sealed class StageNodeVisualTable : IStageNodeVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<(StageType, StageMapRole), StageNodeVisualData> _dataByKey = new();
        private readonly Dictionary<int, StageNodeVisualData> _dataByStageKey = new();
        private readonly IReadOnlyCollection<StageNodeVisualData> _allData;

        internal StageNodeVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var allData = new List<StageNodeVisualData>();
            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageNodeVisualDataSO dataSO || dataSO.GetData() is not StageNodeVisualData data)
                    throw new InvalidOperationException("[StageNodeVisualTable] Requires non-null StageNodeVisualDataSO items.");

                data.Validate();

                if (data.StageKey == 0)
                {
                    if (!_dataByKey.TryAdd((data.StageType, data.MapRole), data))
                        throw new InvalidOperationException(
                            $"[StageNodeVisualTable] 공통 외형 키가 중복됩니다: ({data.StageType}, {data.MapRole}).");
                }
                else if (!_dataByStageKey.TryAdd(data.StageKey, data))
                {
                    throw new InvalidOperationException($"[StageNodeVisualTable] StageKey가 중복됩니다: {data.StageKey}.");
                }
                allData.Add(data);
            }
            _allData = allData.AsReadOnly();
        }

        public StageNodeVisualData GetNodeVisual(StageType stageType, StageMapRole mapRole) =>
            GetNodeVisual(0, stageType, mapRole);

        public bool TryGetNodeVisual(StageType stageType, StageMapRole mapRole, out StageNodeVisualData data) =>
            TryGetNodeVisual(0, stageType, mapRole, out data);

        public StageNodeVisualData GetNodeVisual(int stageKey, StageType stageType, StageMapRole mapRole)
        {
            if (TryGetNodeVisual(stageKey, stageType, mapRole, out StageNodeVisualData data))
                return data;

            throw new KeyNotFoundException(
                $"[StageNodeVisualTable] 노드 외형이 없습니다. 키: ({stageKey}, {stageType}, {mapRole}).");
        }

        public bool TryGetNodeVisual(int stageKey, StageType stageType, StageMapRole mapRole, out StageNodeVisualData data)
        {
            if (stageKey < 0)
                throw new ArgumentOutOfRangeException(nameof(stageKey));

            if (stageKey > 0 && _dataByStageKey.TryGetValue(stageKey, out data))
            {
                if (data.StageType != stageType || data.MapRole != mapRole)
                    throw new InvalidOperationException(
                        $"[StageNodeVisualTable] StageKey {stageKey}의 등록 종류·역할 ({data.StageType}, {data.MapRole})과 " +
                        $"요청 ({stageType}, {mapRole})이 다릅니다.");
                return true;
            }
            return _dataByKey.TryGetValue((stageType, mapRole), out data);
        }

        public IReadOnlyCollection<StageNodeVisualData> GetAllNodeVisuals() => _allData;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageNodeVisualTableQuery>(this);
        }
    }
}
