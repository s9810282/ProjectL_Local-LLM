using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase
{
    /// <summary>Builds a Key index once when the database is created.</summary>
    internal sealed class ExcelTableIndex<TData> where TData : class, IExcelData
    {
        private readonly Dictionary<int, TData> dataByKey = new();
        private readonly ReadOnlyCollection<TData> allData;

        internal ExcelTableIndex(IDataProvider<IDatabaseItem> source, Func<TData, int> getKey)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (getKey == null)
                throw new ArgumentNullException(nameof(getKey));

            IEnumerable<IDatabaseItem> rows = source.GetAllData();
            if (rows == null)
                throw new InvalidOperationException($"{typeof(TData).Name} provider returned a null collection.");

            var entries = new List<TData>();
            foreach (IDatabaseItem item in rows)
            {
                if (item is not TData data)
                    throw new InvalidOperationException(
                        $"{typeof(TData).Name} table requires non-null {typeof(TData).Name} rows. Row: {entries.Count}.");

                int key = getKey(data);
                if (key <= 0)
                    throw new ArgumentException($"{typeof(TData).Name} Key must be positive. Key: {key}.", nameof(source));
                if (!dataByKey.TryAdd(key, data))
                    throw new InvalidOperationException($"Duplicate {typeof(TData).Name} Key: {key}.");

                entries.Add(data);
            }

            allData = entries.AsReadOnly();
        }

        internal TData Get(int key)
        {
            if (dataByKey.TryGetValue(key, out TData data))
                return data;

            throw new KeyNotFoundException($"{typeof(TData).Name} data not found. Key: {key}.");
        }

        internal bool TryGet(int key, out TData data) => dataByKey.TryGetValue(key, out data);

        internal IReadOnlyList<TData> GetAll() => allData;
    }
}
