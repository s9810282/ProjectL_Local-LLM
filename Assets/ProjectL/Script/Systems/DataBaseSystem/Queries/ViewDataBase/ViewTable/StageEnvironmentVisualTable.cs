using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IStageEnvironmentVisualTableQuery : IServiceModule
    {
        StageEnvironmentVisualData GetStageEnvironmentVisual(int stageKey);
        bool TryGetStageEnvironmentVisual(int stageKey, out StageEnvironmentVisualData data);
    }

    /// <summary>스테이지 환경 외형 회전을 StageKey로 조회합니다. 위치와 상태를 보관하지 않습니다.</summary>
    internal sealed class StageEnvironmentVisualTable : IStageEnvironmentVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageEnvironmentVisualData> _dataByStage = new();
        private readonly IViewStageTableQuery _viewStages;

        /// <summary>ViewStage 표를 원본으로 쓰는 호환 조회입니다.</summary>
        internal StageEnvironmentVisualTable(IViewStageTableQuery viewStages)
        {
            _viewStages = viewStages ?? throw new ArgumentNullException(nameof(viewStages));
        }

        internal StageEnvironmentVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageEnvironmentVisualDataSO dataSO || dataSO.GetData() is not StageEnvironmentVisualData data)
                    throw new InvalidOperationException("[StageEnvironmentVisualTable] StageEnvironmentVisualDataSO 항목이 필요합니다.");
                if (!_dataByStage.TryAdd(data.StageKey, data))
                    throw new InvalidOperationException($"[StageEnvironmentVisualTable] StageKey가 중복됩니다: {data.StageKey}.");
            }
        }

        public StageEnvironmentVisualData GetStageEnvironmentVisual(int stageKey)
        {
            if (_viewStages != null)
                return _viewStages.GetViewStage(stageKey).Environment;
            if (_dataByStage.TryGetValue(stageKey, out StageEnvironmentVisualData data))
                return data;
            throw new KeyNotFoundException($"[StageEnvironmentVisualTable] 외형 회전을 찾을 수 없습니다: {stageKey}.");
        }

        public bool TryGetStageEnvironmentVisual(int stageKey, out StageEnvironmentVisualData data)
        {
            if (_viewStages != null)
            {
                if (_viewStages.TryGetViewStage(stageKey, out var stage))
                {
                    data = stage.Environment;
                    return true;
                }
                data = null;
                return false;
            }
            return _dataByStage.TryGetValue(stageKey, out data);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageEnvironmentVisualTableQuery>(this);
        }
    }
}
