using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    #region 서비스

    public interface IStageServiceManager : IServiceModule
    {
        void Enter(int stageKey);
    }

    /// <summary>StageKey로 Root를 조회하고, 실제 StageData를 타입별 Listener에 전달합니다.</summary>
    #endregion

    #region 시스템 모듈

    internal sealed class StageServiceManager : IStageServiceManager,
        ISystemInitData<IDataBaseQueryContext>, IExportSystemServices
    {
        private readonly IReadOnlyDictionary<StageType, Action<StageRootData>> _services =
            StageServiceCollection.CreateBindings();
        private IInGameQuery _inGameQuery;

        public void InitData(IDataBaseQueryContext data)
        {
            _inGameQuery = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
        }

        public void Enter(int stageKey)
        {
            StageRootData stage = GetStage(stageKey);
            if (!_services.TryGetValue(stage.StageType, out Action<StageRootData> service))
                throw new KeyNotFoundException($"No stage service for {stage.StageType}.");
            service(stage);
        }

        internal bool IsBossStage(int stageKey)
        {
            return GetStage(stageKey).MapRole == StageMapRole.Boss;
        }

        // 실행 핸들러를 호출하지 않고 진입에 필요한 데이터만 확인한다.
        internal void ValidateEntry(int stageKey)
        {
            StageRootData stage = GetStage(stageKey);
            if (!_services.ContainsKey(stage.StageType) || stage.StageData.StageType != stage.StageType)
                throw new InvalidOperationException("Stage entry data has an unsupported or inconsistent type.");
            stage.StageData.Validate();
        }

        internal ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item.StageMapData GetStageMap(int mapKey)
        {
            if (_inGameQuery == null) throw new InvalidOperationException("StageServiceManager must be initialized before use.");
            return _inGameQuery.StageMapTableQuery.GetStageMap(mapKey);
        }

        internal StageRootData GetStage(int stageKey)
        {
            if (_inGameQuery == null)
                throw new InvalidOperationException("StageServiceManager must be initialized before use.");
            return _inGameQuery.StageTableQuery.GetStage(stageKey);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageServiceManager>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
