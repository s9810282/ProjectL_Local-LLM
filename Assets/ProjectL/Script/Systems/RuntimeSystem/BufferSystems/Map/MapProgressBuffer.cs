using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Map
{
    #region 서비스

    public interface IMapProgressBufferReadSink : IServiceModule
    {
        IReadOnlyCollection<MapProgressData> Maps { get; }
        MapProgressData GetMap(int mapKey);
    }

    /// <summary>원본 비교 없이 바로 쓰는 경로다. 초기화·테스트 준비와 설계상 남겨 둔 경계(ResolveStage 등)에서만 쓴다.
    /// 새 게임 흐름은 기대값을 담은 준비→설치 제안(IBufferQuery의 Publish*·Prepare* 경로)을 쓴다.</summary>
    public interface IMapProgressBufferWriteSink : IServiceModule
    {
        void RegisterMap(int mapKey);
        void SetClear(int mapKey, bool isClear);
        void Clear();
    }

    internal interface IMapProgressBufferStore : IServiceModule
    {
        MapProgressData PrepareClear(int mapKey, bool isClear);
        void InstallClear(MapProgressData prepared);
    }

    #endregion

    #region 시스템 모듈

    internal sealed class MapProgressBuffer : IMapProgressBufferReadSink, IMapProgressBufferWriteSink,
        IMapProgressBufferStore, ISystemInitData<IDataBaseQueryContext>, IExportSystemServices
    {
        private readonly Dictionary<int, MapProgressData> maps = new();
        public IReadOnlyCollection<MapProgressData> Maps => maps.Values;

        public MapProgressData GetMap(int mapKey)
        {
            if (mapKey < 1) throw new ArgumentOutOfRangeException(nameof(mapKey));

            if (maps.TryGetValue(mapKey, out var data)) return data;

            throw new KeyNotFoundException($"Map buffer has no map with key {mapKey}.");
        }

        /// <summary>Init에서 맵을 한 번 등록합니다. 같은 키의 중복 등록은 거부합니다.</summary>
        public void RegisterMap(int mapKey)
        {
            var data = new MapProgressData(mapKey);

            if (maps.ContainsKey(mapKey))
                throw new InvalidOperationException($"Map {mapKey} is already registered.");

            maps.Add(mapKey, data);
        }

        public void SetClear(int mapKey, bool isClear)
        {
            InstallClear(PrepareClear(mapKey, isClear));
        }

        public MapProgressData PrepareClear(int mapKey, bool isClear)
        {
            var data = GetMap(mapKey);
            return new MapProgressData(data.MapKey, isClear);
        }

        public void InstallClear(MapProgressData prepared) => maps[prepared.MapKey] = prepared;

        public void Clear() => maps.Clear();

        public void InitData(IDataBaseQueryContext data)
        {
            foreach (var map in data.InGameQuery.StageMapTableQuery.GetAllStageMaps())
                RegisterMap(map.MapKey);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IMapProgressBufferReadSink>(this);
            sink.AddService<IMapProgressBufferWriteSink>(this);
            sink.AddService<IMapProgressBufferStore>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
