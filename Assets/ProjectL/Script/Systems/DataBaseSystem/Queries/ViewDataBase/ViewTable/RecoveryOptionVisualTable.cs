using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IRecoveryOptionVisualTableQuery : IServiceModule
    {
        RecoveryOptionVisualData GetRecoveryOptionVisual(int optionId);
        bool TryGetRecoveryOptionVisual(int optionId, out RecoveryOptionVisualData data);
        IReadOnlyCollection<RecoveryOptionVisualData> GetAllRecoveryOptionVisuals();
    }

    /// <summary>회복 선택지 표시를 OptionId 기준으로 제공합니다.</summary>
    internal sealed class RecoveryOptionVisualTable : IRecoveryOptionVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, RecoveryOptionVisualData> _dataByKey = new();

        internal RecoveryOptionVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not RecoveryOptionVisualDataSO dataSO || dataSO.GetData() is not RecoveryOptionVisualData data)
                    throw new InvalidOperationException("[RecoveryOptionVisualTable] Requires non-null RecoveryOptionVisualDataSO items.");

                data.Validate();

                if (!_dataByKey.TryAdd(data.OptionId, data))
                    throw new InvalidOperationException($"[RecoveryOptionVisualTable] Duplicate option id: {data.OptionId}.");
            }
        }

        public RecoveryOptionVisualData GetRecoveryOptionVisual(int optionId)
        {
            if (_dataByKey.TryGetValue(optionId, out RecoveryOptionVisualData data))
                return data;

            throw new KeyNotFoundException($"[RecoveryOptionVisualTable] Recovery option visual not found. OptionId: {optionId}.");
        }

        public bool TryGetRecoveryOptionVisual(int optionId, out RecoveryOptionVisualData data) =>
            _dataByKey.TryGetValue(optionId, out data);

        public IReadOnlyCollection<RecoveryOptionVisualData> GetAllRecoveryOptionVisuals() => _dataByKey.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRecoveryOptionVisualTableQuery>(this);
        }
    }
}
