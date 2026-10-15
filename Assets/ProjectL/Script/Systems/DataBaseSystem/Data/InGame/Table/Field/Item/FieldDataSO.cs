using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item
{
    /// <summary>FieldData의 입력 SO입니다. 기획 값은 이 에셋에서 수정합니다.</summary>
    [CreateAssetMenu(fileName = "NewFieldData", menuName = "ProjectL/Database/InGame/FieldData")]
    internal sealed class FieldDataSO : DatabaseItem
    {
        [SerializeField] private string fieldId;
        [SerializeField, Min(1)] private int columnCount = 5;
        [SerializeField, Min(1)] private int rowCount = 5;
        [SerializeField, Min(1)] private int reserveCapacity = 4;
        [SerializeField, Min(1)] private int playerDeploymentDepth = 1;
        [SerializeField, Min(1)] private int enemyDeploymentDepth = 1;

        /// <summary>다른 SO가 이 필드를 참조할 때 쓰는 키입니다.</summary>
        internal string FieldId => fieldId;

        public override IData GetData()
        {
            return new FieldData(
                fieldId,
                columnCount,
                rowCount,
                reserveCapacity,
                playerDeploymentDepth,
                enemyDeploymentDepth);
        }
    }
}
