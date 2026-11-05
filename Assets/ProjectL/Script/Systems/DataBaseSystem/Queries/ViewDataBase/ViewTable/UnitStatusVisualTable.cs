using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.UnitStatus.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IUnitStatusVisualTableQuery : IServiceModule
    {
        UnitStatusVisualData Get(StatusEffectKey kind);
        bool TryGet(StatusEffectKey kind, out UnitStatusVisualData data);
    }

    /// <summary>걸린 상태(버프·디버프)의 HUD 외형을 상태 종류 기준으로 제공합니다(특성 발동 연출 설계 TA9).</summary>
    internal sealed class UnitStatusVisualTable : IUnitStatusVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<StatusEffectKey, UnitStatusVisualData> _dataByKey = new();

        internal UnitStatusVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not UnitStatusVisualDataSO dataSO || dataSO.GetData() is not UnitStatusVisualData data)
                    throw new InvalidOperationException("[UnitStatusVisualTable] Requires non-null UnitStatusVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[UnitStatusVisualTable] Duplicate effect key: {data.Key}.");
            }
        }

        public UnitStatusVisualData Get(StatusEffectKey kind)
        {
            if (_dataByKey.TryGetValue(kind, out UnitStatusVisualData data))
                return data;

            throw new KeyNotFoundException($"[UnitStatusVisualTable] Status visual not found. Effect key: {kind}.");
        }

        public bool TryGet(StatusEffectKey kind, out UnitStatusVisualData data) => _dataByKey.TryGetValue(kind, out data);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IUnitStatusVisualTableQuery>(this);
        }
    }
}
