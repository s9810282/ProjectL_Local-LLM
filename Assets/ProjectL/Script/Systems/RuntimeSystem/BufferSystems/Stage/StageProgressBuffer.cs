using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Stage
{
    #region 서비스

    public interface IStageProgressBufferReadSink : IServiceModule
    {
        IReadOnlyList<StageProgressData> GetStages(int mapKey);
        bool IsCleared(int mapKey, int nodeId);
    }

    /// <summary>원본 비교 없이 바로 쓰는 경로다. 초기화·테스트 준비와 설계상 남겨 둔 경계(ResolveStage 등)에서만 쓴다.
    /// 새 게임 흐름은 기대값을 담은 준비→설치 제안(IBufferQuery의 Publish*·Prepare* 경로)을 쓴다.</summary>
    public interface IStageProgressBufferWriteSink : IServiceModule
    {
        void RegisterStages(int mapKey, IEnumerable<StageProgressData> stages);
        void SetClear(int mapKey, int nodeId, bool isClear);
        void Clear();
    }

    internal interface IStageProgressBufferStore : IServiceModule
    {
        PreparedStageRegistration PrepareRegistration(int mapKey, IReadOnlyList<StageProgressData> stages);
        void InstallRegistration(PreparedStageRegistration prepared);
        PreparedStageClear PrepareClear(int mapKey, int nodeId, bool isClear);
        void InstallClear(PreparedStageClear prepared);
    }

    internal sealed class PreparedStageRegistration
    {
        internal readonly int MapKey;
        internal readonly List<StageProgressData> Stages;
        internal PreparedStageRegistration(int mapKey, List<StageProgressData> stages)
        { MapKey = mapKey; Stages = stages; }
    }

    internal sealed class PreparedStageClear
    {
        internal readonly List<StageProgressData> Stages;
        internal readonly int Index;
        internal readonly StageProgressData Final;
        internal PreparedStageClear(List<StageProgressData> stages, int index, StageProgressData final)
        { Stages = stages; Index = index; Final = final; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class StageProgressBuffer :
        IStageProgressBufferReadSink, IStageProgressBufferWriteSink, IStageProgressBufferStore, IExportSystemServices
    {
        private readonly Dictionary<int, List<StageProgressData>> stagesByMap = new();

        public IReadOnlyList<StageProgressData> GetStages(int mapKey)
        {
            ValidateMapKey(mapKey);

            return stagesByMap.TryGetValue(mapKey, out var stages)
                ? stages.AsReadOnly()
                : Array.Empty<StageProgressData>();
        }

        public bool IsCleared(int mapKey, int nodeId)
        {
            ValidateMapKey(mapKey);

            if (nodeId < 1) throw new ArgumentOutOfRangeException(nameof(nodeId));

            if (stagesByMap.TryGetValue(mapKey, out var stages))
                foreach (var stage in stages)
                    if (stage.NodeId == nodeId) return stage.IsClear;

            return false;
        }

        public void SetClear(int mapKey, int nodeId, bool isClear)
        {
            InstallClear(PrepareClear(mapKey, nodeId, isClear));
        }

        public PreparedStageClear PrepareClear(int mapKey, int nodeId, bool isClear)
        {
            ValidateMapKey(mapKey);

            if (nodeId < 1) throw new ArgumentOutOfRangeException(nameof(nodeId));

            if (!stagesByMap.TryGetValue(mapKey, out var stages))
                throw new KeyNotFoundException($"Map {mapKey} has no registered stages.");

            int index = stages.FindIndex(stage => stage.NodeId == nodeId);

            if (index < 0) throw new KeyNotFoundException($"Node {nodeId} is not registered in map {mapKey}.");

            var data = stages[index];

            return new PreparedStageClear(stages, index, new StageProgressData(data.NodeId, data.StageKey, data.Depth,
                data.WidthIndex, data.NextNodeIds, isClear, data.StageType, data.MapRole));
        }

        public void InstallClear(PreparedStageClear prepared) => prepared.Stages[prepared.Index] = prepared.Final;

        /// <summary>Init에서 맵별 노드 목록을 한 번 등록합니다. 전달받은 목록은 복사해서 보관합니다.</summary>
        public void RegisterStages(int mapKey, IEnumerable<StageProgressData> stages)
        {
            ValidateMapKey(mapKey);
            if (stages == null) throw new ArgumentNullException(nameof(stages));
            InstallRegistration(PrepareRegistration(mapKey, new List<StageProgressData>(stages)));
        }

        public PreparedStageRegistration PrepareRegistration(int mapKey, IReadOnlyList<StageProgressData> stages)
        {
            ValidateMapKey(mapKey);

            if (stages == null) throw new ArgumentNullException(nameof(stages));

            if (stagesByMap.ContainsKey(mapKey))
                throw new InvalidOperationException($"Stages for map {mapKey} are already registered.");

            var records = new List<StageProgressData>();
            var nodeIds = new HashSet<int>();

            foreach (var stage in stages)
            {
                // 키·깊이 형식은 StageProgressData 생성자가 보장한다. 생성자를 거치지 않은 기본값(NodeId 0)과 중복만 막는다.
                if (stage.NodeId < 1 || !nodeIds.Add(stage.NodeId))
                    throw new ArgumentException("Stage records require valid keys and unique node IDs.", nameof(stages));
                records.Add(stage);
            }

            if (records.Count == 0) throw new ArgumentException("Stage records must not be empty.", nameof(stages));

            foreach (var stage in records)
                foreach (int nextId in stage.NextNodeIds)
                    if (!nodeIds.Contains(nextId))
                        throw new ArgumentException($"Connection target {nextId} does not exist in this map.", nameof(stages));

            return new PreparedStageRegistration(mapKey, records);
        }

        public void InstallRegistration(PreparedStageRegistration prepared) => stagesByMap.Add(prepared.MapKey, prepared.Stages);

        public void Clear() => stagesByMap.Clear();

        private static void ValidateMapKey(int mapKey)
        {
            if (mapKey < 1) throw new ArgumentOutOfRangeException(nameof(mapKey));
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageProgressBufferReadSink>(this);

            sink.AddService<IStageProgressBufferWriteSink>(this);
            sink.AddService<IStageProgressBufferStore>(this);
        }
    }

    #endregion
}
