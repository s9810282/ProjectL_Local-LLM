using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ITutorialDefinitionTableQuery : IServiceModule
    {
        TutorialDefinitionData Get(int tutorialKey);
        bool TryGet(int tutorialKey, out TutorialDefinitionData data);

        /// <summary>그 맵에서 실행할 정의를 찾습니다. 한 맵에는 정의가 하나뿐입니다(설계 §15.12).</summary>
        bool TryGetByTargetMap(int mapKey, out TutorialDefinitionData data);

        IReadOnlyCollection<TutorialDefinitionData> GetAll();
    }

    /// <summary>
    /// 튜토리얼 정의를 튜토리얼 키 기준으로 제공합니다(튜토리얼 설계 §15.11). 정의는 불러올 때 모두 검사합니다.
    /// 한 맵에는 정의 하나만 둡니다. 실행 대상은 맵으로 판정하기 때문입니다(설계 §15.12).
    /// </summary>
    internal sealed class TutorialDefinitionTable : ITutorialDefinitionTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, TutorialDefinitionData> _dataByKey = new();
        private readonly Dictionary<int, TutorialDefinitionData> _dataByMap = new();

        internal TutorialDefinitionTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not TutorialDefinitionDataSO dataSO || dataSO.GetData() is not TutorialDefinitionData data)
                    throw new InvalidOperationException("[TutorialDefinitionTable] Requires non-null TutorialDefinitionDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.TutorialKey, data))
                    throw new InvalidOperationException($"[TutorialDefinitionTable] Duplicate key: {data.TutorialKey}.");
                if (!_dataByMap.TryAdd(data.TargetMapKey, data))
                    throw new InvalidOperationException($"[TutorialDefinitionTable] Duplicate target map key: {data.TargetMapKey}.");
            }
        }

        public TutorialDefinitionData Get(int tutorialKey)
        {
            if (_dataByKey.TryGetValue(tutorialKey, out TutorialDefinitionData data))
                return data;

            throw new KeyNotFoundException($"[TutorialDefinitionTable] Tutorial definition not found. Key: {tutorialKey}.");
        }

        public bool TryGet(int tutorialKey, out TutorialDefinitionData data) => _dataByKey.TryGetValue(tutorialKey, out data);

        public bool TryGetByTargetMap(int mapKey, out TutorialDefinitionData data) => _dataByMap.TryGetValue(mapKey, out data);

        public IReadOnlyCollection<TutorialDefinitionData> GetAll() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITutorialDefinitionTableQuery>(this);
        }
    }
}
