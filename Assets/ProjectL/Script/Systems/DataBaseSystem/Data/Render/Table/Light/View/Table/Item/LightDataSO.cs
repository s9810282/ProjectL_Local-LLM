using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.Enum;
using ProjectL.Framework.DataBase;
using UnityEngine;
 // Enum ���ӽ����̽� �߰�

namespace ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.View.Table.Item
{
    [System.Serializable]
    internal class LightData : IData
    {
        [SerializeField] private LightPresetType presetType;           // String Ű�� Enum���� ����
        [SerializeField] private LightType unityLightType; // ���� �������� ���� ����Ƽ ����Ʈ Ÿ��
        [SerializeField] private UnityEngine.Light lightPrefab;
        [SerializeField] private int initialPoolSize = 10;

        public LightPresetType PresetType => presetType;
        public LightType UnityLightType => unityLightType;
        public UnityEngine.Light LightPrefab => lightPrefab;
        public int InitialPoolSize => initialPoolSize;
    }

    [CreateAssetMenu(fileName = "NewLightData", menuName = "ProjectL/Database/LightData")]
    internal class LightDataSO : DatabaseItem
    {
        [SerializeField] private LightData lightData;

        public override IData GetData() => lightData;
    }
}