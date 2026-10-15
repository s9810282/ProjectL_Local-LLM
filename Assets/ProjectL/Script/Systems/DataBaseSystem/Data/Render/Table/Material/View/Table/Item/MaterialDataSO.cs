using ProjectL.Framework.DataBase;
using UnityEngine;
// ���ӽ����̽� ��Ÿ ���� (Metrial -> Material)

namespace ProjectL.Systems.DataBaseSystem.Data.Render.Table.Material.View.Table.Item
{
    [System.Serializable]
    internal class MetrialData : IData
    {
        [SerializeField] private string key;
        [SerializeField] private UnityEngine.Material material; // ���ӽ����̽� �浹 ������ ���� UnityEngine ����

        public string Key => key;
        public UnityEngine.Material Material => material;
    }

    // ����Ƽ ������ ��Ŭ�� �޴� �߰�
    [CreateAssetMenu(fileName = "NewMaterialData", menuName = "ProjectL/Database/MaterialData")]
    internal class MaterialDataSO : DatabaseItem // Ŭ������ ��Ÿ ���� (Metrial -> Material)
    {
        [SerializeField] private MetrialData materialData; // ������ ��Ÿ ����

        public override IData GetData() => materialData;
    }
}