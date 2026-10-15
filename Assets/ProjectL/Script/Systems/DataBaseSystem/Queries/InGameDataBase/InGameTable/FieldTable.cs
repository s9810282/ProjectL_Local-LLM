using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IFieldTableQuery : IServiceModule
    {
        FieldData GetField(string fieldId);
        bool TryGetField(string fieldId, out FieldData data);
        IReadOnlyCollection<FieldData> GetAllFields();
    }

    /// <summary>모든 FieldData를 FieldId 기준으로 제공합니다.</summary>
    internal sealed class FieldTable : IFieldTableQuery, IExportSystemServices
    {
        private readonly Dictionary<string, FieldData> _dataById = new(StringComparer.Ordinal);

        internal FieldTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not FieldDataSO dataSO || dataSO.GetData() is not FieldData data)
                    throw new InvalidOperationException("[FieldTable] FieldTable requires non-null FieldDataSO items.");

                data.Validate();

                if (!_dataById.TryAdd(data.FieldId, data))
                    throw new InvalidOperationException($"[FieldTable] Duplicate FieldId: {data.FieldId}.");
            }
        }

        public FieldData GetField(string fieldId)
        {
            if (fieldId != null && _dataById.TryGetValue(fieldId, out FieldData data))
                return data;

            throw new KeyNotFoundException($"[FieldTable] Field data not found. FieldId: {fieldId}.");
        }

        public bool TryGetField(string fieldId, out FieldData data)
        {
            if (fieldId == null)
            {
                data = null;
                return false;
            }

            return _dataById.TryGetValue(fieldId, out data);
        }

        public IReadOnlyCollection<FieldData> GetAllFields() => _dataById.Values;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IFieldTableQuery>(this);
        }
    }
}
