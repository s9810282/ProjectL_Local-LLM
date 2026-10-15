using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    #region 서비스

    internal interface IStageTableQuery : IServiceModule
    {
        StageRootData GetStage(int stageKey);
        IReadOnlyList<StageRootData> GetStages(int mapKey);
    }

    /// <summary>
    /// 모든 StageRootData를 한 번 읽어 StageKey와 MapKey 기준으로 제공합니다.
    /// 전투·상점·이벤트별 별도 테이블은 만들지 않습니다.
    /// </summary>
    #endregion

    #region 시스템 모듈

    internal sealed class StageTable : IStageTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageRootData> _stagesByKey = new();
        private readonly Dictionary<int, IReadOnlyList<StageRootData>> _stagesByMapKey = new();

        internal StageTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var mutableStagesByMapKey = new Dictionary<int, List<StageRootData>>();
            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageRootDataSO dataSO || dataSO.GetData() is not StageRootData stage)
                    throw new InvalidOperationException("StageTable requires non-null StageRootDataSO items.");

                stage.Validate();

                if (!_stagesByKey.TryAdd(stage.StageKey, stage))
                    throw new InvalidOperationException($"Duplicate StageKey: {stage.StageKey}.");

                if (!mutableStagesByMapKey.TryGetValue(stage.MapKey, out List<StageRootData> stages))
                {
                    stages = new List<StageRootData>();
                    mutableStagesByMapKey.Add(stage.MapKey, stages);
                }

                stages.Add(stage);
            }

            foreach ((int mapKey, List<StageRootData> stages) in mutableStagesByMapKey)
                _stagesByMapKey.Add(mapKey, new ReadOnlyCollection<StageRootData>(stages));
        }

        public StageRootData GetStage(int stageKey)
        {
            if (_stagesByKey.TryGetValue(stageKey, out StageRootData stage))
                return stage;

            throw new KeyNotFoundException($"Stage data not found. StageKey: {stageKey}.");
        }

        public IReadOnlyList<StageRootData> GetStages(int mapKey)
        {
            if (_stagesByMapKey.TryGetValue(mapKey, out IReadOnlyList<StageRootData> stages))
                return stages;

            throw new KeyNotFoundException($"No stages are registered. MapKey: {mapKey}.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageTableQuery>(this);
        }
    }

    #endregion
}
