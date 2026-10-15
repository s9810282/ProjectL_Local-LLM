using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.View.Table.Item
{
    [System.Serializable]
    internal class EffectElementData : IData
    {
        [SerializeField] private EffectType effectType;
        [SerializeField] private int effectPoolInitializeCount = 0;
        [SerializeField] private int effectPoolMaxCount = 0;
        [SerializeField] private GameObject prefab;

        public EffectType EffectType => effectType;
        public int EffectPoolInitializeCount => effectPoolInitializeCount;
        public int EffectPoolMaxCount => effectPoolMaxCount;
        public GameObject Prefab => prefab;
    }

    // ����Ƽ ������ ��Ŭ�� �޴� �߰�
    [CreateAssetMenu(fileName = "NewEffectData", menuName = "ProjectL/Database/EffectData")]
    internal class EffectDataSO : DatabaseItem
    {
        [SerializeField] private EffectElementData effectElementData;

        public override IData GetData() => effectElementData;
    }
}