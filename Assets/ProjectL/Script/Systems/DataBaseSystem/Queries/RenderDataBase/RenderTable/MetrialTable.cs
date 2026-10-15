using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Material.View.Table.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
 // IDataProvider�� ���� �߰�

namespace ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable
{
    #region 서비스

    internal interface IMetrialTableQuery : IServiceModule
    {
        // 1. ��ü ������ ����Ʈ ��ȯ
        IReadOnlyList<MetrialData> GetData();

        // 2. Key������ Ư�� ��Ƽ���� ��ȯ (�߰���)
        MetrialData GetMaterial(string key);

        // 3. Key������ �����ϰ� ��Ƽ���� ��ȯ (�߰���)
        bool TryGetMaterial(string key, out MetrialData materialData);
    }

    #endregion

    #region 시스템 모듈

    internal class MetrialTable : IMetrialTableQuery, IExportSystemServices
    {
        // ��ü �����͸� ���� ����Ʈ��, ���� �˻��� ���� ��ųʸ�
        private readonly List<MetrialData> _materialList = new();
        private readonly Dictionary<string, MetrialData> _materialDict = new();

        // ������: �����ͺ��̽� �ʱ�ȭ ������ IDataProvider�� ���� �����͸� ���Թ���
        internal MetrialTable(IDataProvider<IDatabaseItem> datas)
        {
            if (datas == null) throw new ArgumentNullException(nameof(datas));

            // MetrialDataSO Ÿ�Ը� ���͸��ؼ� ������
            foreach (var dataSO in datas.GetAllData().OfType<MaterialDataSO>())
            {
                // �ռ� EffectTable���� �Ͻ� ��ó��, ���� �����͸� ����
                if (dataSO.GetData() is MetrialData materialData)
                {
                    AddMaterialData(materialData);
                }
            }
        }

        private void AddMaterialData(MetrialData data)
        {
            ValidateData(data); // ������ ���Ἲ ����

            _materialList.Add(data);
            _materialDict.Add(data.Key, data); // ���� Ž���� ���� ��ųʸ��� �߰�
        }

        private void ValidateData(MetrialData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[MetrialTable] MaterialData instance is null.");
            }

            if (string.IsNullOrEmpty(data.Key))
            {
                throw new ArgumentException("[MetrialTable] Material Key is null or empty.");
            }

            if (data.Material == null)
            {
                throw new ArgumentNullException(nameof(data.Material), $"[MetrialTable] Material object is null. (Key: {data.Key})");
            }

            if (_materialDict.ContainsKey(data.Key))
            {
                throw new InvalidOperationException($"[MetrialTable] Material key already exists. Duplicate key: {data.Key}");
            }
        }

        public IReadOnlyList<MetrialData> GetData()
        {
            return _materialList;
        }

        public MetrialData GetMaterial(string key)
        {
            if (_materialDict.TryGetValue(key, out var data))
            {
                return data;
            }

            throw new KeyNotFoundException($"[MetrialTable] Material key '{key}' was not found.");
        }

        public bool TryGetMaterial(string key, out MetrialData materialData)
        {
            return _materialDict.TryGetValue(key, out materialData);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IMetrialTableQuery>(this);
        }
    }

    #endregion
}
