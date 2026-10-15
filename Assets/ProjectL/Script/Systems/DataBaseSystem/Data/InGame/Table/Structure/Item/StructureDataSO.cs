using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item
{
    /// <summary>StructureData의 입력 SO입니다. 기획 값은 이 에셋에서 수정합니다.</summary>
    [CreateAssetMenu(fileName = "NewStructureData", menuName = "ProjectL/Database/InGame/StructureData")]
    internal sealed class StructureDataSO : DatabaseItem
    {
        [Header("Identity")]
        [SerializeField] private EntityKey key = EntityKey.Structure_Headquarters;

        [Header("Footprint")]
        [SerializeField, Min(1)] private int footprintWidth = 1;
        [SerializeField, Min(1)] private int footprintHeight = 1;

        [Header("Core Stats")]
        [SerializeField, Min(1)] private int maxHealth = 20;

        [Header("Attack")]
        [SerializeField] private bool isAttackable;
        [SerializeField, Min(0)] private int attackPower;

        /// <summary>다른 SO가 이 구조물을 참조할 때 쓰는 키입니다.</summary>
        internal EntityKey Key => key;

        public override IData GetData()
        {
            return new StructureData(key, footprintWidth, footprintHeight, maxHealth, isAttackable, attackPower);
        }
    }
}
