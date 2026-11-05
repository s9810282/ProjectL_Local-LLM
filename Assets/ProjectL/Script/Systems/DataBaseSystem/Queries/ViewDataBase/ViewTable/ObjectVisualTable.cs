using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Object.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IObjectVisualTableQuery : IServiceModule
    {
        ObjectVisualData GetObjectVisual(ObjectContentKind kind);
        bool TryGetObjectVisual(ObjectContentKind kind, out ObjectVisualData data);
    }

    /// <summary>Object 외형을 종류로 조회합니다. 운영 등록에는 프리팹이 필요합니다.</summary>
    internal sealed class ObjectVisualTable : IObjectVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<ObjectContentKind, ObjectVisualData> _dataByKind = new();

        internal ObjectVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not ObjectVisualDataSO dataSO || dataSO.GetData() is not ObjectVisualData data)
                    throw new InvalidOperationException("[ObjectVisualTable] ObjectVisualDataSO 항목이 필요합니다.");
                if (data.Prefab == null)
                    throw new InvalidOperationException($"[ObjectVisualTable] {data.Kind}의 프리팹이 없습니다.");
                if (!_dataByKind.TryAdd(data.Kind, data))
                    throw new InvalidOperationException($"[ObjectVisualTable] 종류가 중복됩니다: {data.Kind}.");
            }
        }

        public ObjectVisualData GetObjectVisual(ObjectContentKind kind)
        {
            if (_dataByKind.TryGetValue(kind, out ObjectVisualData data))
                return data;
            throw new KeyNotFoundException($"[ObjectVisualTable] 외형을 찾을 수 없습니다: {kind}.");
        }

        public bool TryGetObjectVisual(ObjectContentKind kind, out ObjectVisualData data) =>
            _dataByKind.TryGetValue(kind, out data);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IObjectVisualTableQuery>(this);
        }
    }
}
