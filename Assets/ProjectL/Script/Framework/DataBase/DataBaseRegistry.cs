using System;
using System.Collections.Generic;

namespace ProjectL.Framework.DataBase
{
    public interface IDataBaseReader
    {
        T GetDataBase<T>() where T : class;
    }

    public sealed class DataBaseRegistry<TLayer> : IDataBaseReader
    {
        private readonly HashSet<Type> _dataBaseTypes = new();
        private readonly Dictionary<Type, TLayer> _dataBaseMap = new();

        public DataBaseRegistry(IDataProvider<TLayer> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            foreach (var db in data.GetAllData())
            {
                if (db == null)
                    continue;

                AddData(db.GetType(), db);
            }
        }

        public T GetDataBase<T>() where T : class
        {
            if (_dataBaseMap.TryGetValue(typeof(T), out var db))
            {
                return db as T;
            }

            throw new InvalidOperationException($"Database of type {typeof(T).Name} not found.");
        }

        private void AddData(Type dataType, TLayer data)
        {
            if (_dataBaseTypes.Add(dataType))
            {
                _dataBaseMap[dataType] = data;
            }
            else
            {
                throw new InvalidOperationException($"Database of type {dataType.Name} already added.");
            }
        }
    }
}
