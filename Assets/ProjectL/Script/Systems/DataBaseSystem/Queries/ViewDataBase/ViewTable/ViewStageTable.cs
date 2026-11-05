using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ViewStage.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IViewStageTableQuery : IServiceModule
    {
        ViewStageData GetViewStage(int stageKey);
        bool TryGetViewStage(int stageKey, out ViewStageData data);
        IReadOnlyCollection<ViewStageData> GetAllViewStages();
    }

    /// <summary>StageKey 요청 한 번으로 프랍과 환경 외형을 함께 조립해 돌려주는 View 표입니다.</summary>
    internal sealed class ViewStageTable : IViewStageTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, ViewStageData> _dataByStage = new();

        internal ViewStageTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ViewStageDataSO dataSO || dataSO.GetData() is not ViewStageData data)
                    throw new InvalidOperationException("[ViewStageTable] ViewStageDataSO 항목이 필요합니다.");
                if (!_dataByStage.TryAdd(data.StageKey, data))
                    throw new InvalidOperationException($"[ViewStageTable] StageKey가 중복됩니다: {data.StageKey}.");
            }
        }

        public ViewStageData GetViewStage(int stageKey)
        {
            if (TryGetViewStage(stageKey, out ViewStageData data))
                return data;
            throw new KeyNotFoundException($"[ViewStageTable] 스테이지 표시 정보를 찾을 수 없습니다: {stageKey}.");
        }

        public bool TryGetViewStage(int stageKey, out ViewStageData data) =>
            _dataByStage.TryGetValue(stageKey, out data);

        public IReadOnlyCollection<ViewStageData> GetAllViewStages() => _dataByStage.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IViewStageTableQuery>(this);
        }
    }
}
