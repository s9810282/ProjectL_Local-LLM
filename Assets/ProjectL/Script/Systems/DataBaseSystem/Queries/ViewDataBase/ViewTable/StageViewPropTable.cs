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
            if (_dataByStage.TryGetValue(stageKey, out StageViewPropData data))
                return data;

            throw new KeyNotFoundException(
                $"[StageViewPropTable] Stage view prop was not found. StageKey: {stageKey}");
        }

        public bool TryGetStageViewProp(int stageKey, out StageViewPropData data)
        {
            return _dataByStage.TryGetValue(stageKey, out data);
        }

        public IReadOnlyCollection<StageViewPropData> GetAllStageViewProps()
        {
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
            if (data.GameObject == null)
                throw new ArgumentNullException(nameof(data.GameObject));
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
