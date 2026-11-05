using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    #region 서비스

    internal interface IStageViewPropTableQuery : IServiceModule
    {
        StageViewPropData GetStageViewProp(int stageKey);
        bool TryGetStageViewProp(int stageKey, out StageViewPropData data);
        IReadOnlyCollection<StageViewPropData> GetAllStageViewProps();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class StageViewPropTable :
        IStageViewPropTableQuery,
        IExportSystemServices
    {
        private readonly Dictionary<int, StageViewPropData> _dataByStage = new();
        private readonly IViewStageTableQuery _viewStages;

        /// <summary>ViewStage 표를 원본으로 쓰는 호환 조회입니다.</summary>
        internal StageViewPropTable(IViewStageTableQuery viewStages)
        {
            _viewStages = viewStages ?? throw new ArgumentNullException(nameof(viewStages));
        }

        internal StageViewPropTable(IDataProvider<IDatabaseItem> datas)
        {
            if (datas == null)
                throw new ArgumentNullException(nameof(datas));

            foreach (StageViewPropDataSO dataSO in datas.GetAllData().OfType<StageViewPropDataSO>())
            {
                if (dataSO.GetData() is StageViewPropData data)
                    AddStageViewPropData(data);
            }
        }

        public StageViewPropData GetStageViewProp(int stageKey)
        {
            if (_viewStages != null)
                return _viewStages.GetViewStage(stageKey).Prop;
            if (_dataByStage.TryGetValue(stageKey, out StageViewPropData data))
                return data;

            throw new KeyNotFoundException(
                $"[StageViewPropTable] Stage view prop was not found. StageKey: {stageKey}");
        }

        public bool TryGetStageViewProp(int stageKey, out StageViewPropData data)
        {
            if (_viewStages != null)
            {
                if (_viewStages.TryGetViewStage(stageKey, out var stage))
                {
                    data = stage.Prop;
                    return true;
                }
                data = null;
                return false;
            }
            return _dataByStage.TryGetValue(stageKey, out data);
        }

        public IReadOnlyCollection<StageViewPropData> GetAllStageViewProps()
        {
            if (_viewStages != null)
                return _viewStages.GetAllViewStages().Select(stage => stage.Prop).ToArray();
            return _dataByStage.Values;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageViewPropTableQuery>(this);
        }

        private void AddStageViewPropData(StageViewPropData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Validate();
            if (data.StageKey < 1)
                throw new ArgumentOutOfRangeException(nameof(data.StageKey), "StageKey must be positive.");
            if (_dataByStage.ContainsKey(data.StageKey))
            {
                throw new InvalidOperationException(
                    $"[StageViewPropTable] Duplicate stage view prop. StageKey: {data.StageKey}");
            }

            _dataByStage.Add(data.StageKey, data);
        }
    }

    #endregion
}
