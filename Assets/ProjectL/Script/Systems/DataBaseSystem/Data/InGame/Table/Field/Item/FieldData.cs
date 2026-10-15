using System;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item
{
    /// <summary>
    /// 전투 필드 한 종류의 크기와 배치 규칙입니다. Core가 받는 순수 C# 데이터이며 FieldDataSO가 생성합니다.
    /// </summary>
    public sealed class FieldData : IData
    {
        public string FieldId { get; }
        public int ColumnCount { get; }
        public int RowCount { get; }
        public int ReserveCapacity { get; }
        public int PlayerDeploymentDepth { get; }
        public int EnemyDeploymentDepth { get; }

        public FieldData(
            string fieldId,
            int columnCount,
            int rowCount,
            int reserveCapacity,
            int playerDeploymentDepth,
            int enemyDeploymentDepth)
        {
            FieldId = fieldId;
            ColumnCount = columnCount;
            RowCount = rowCount;
            ReserveCapacity = reserveCapacity;
            PlayerDeploymentDepth = playerDeploymentDepth;
            EnemyDeploymentDepth = enemyDeploymentDepth;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FieldId))
                throw new InvalidOperationException("[FieldData] FieldId is required.");
            if (ColumnCount < 1 || RowCount < 1)
                throw new InvalidOperationException($"[FieldData] Field size must be positive. FieldId: {FieldId}");
            if (ReserveCapacity < 1)
                throw new InvalidOperationException($"[FieldData] ReserveCapacity must be positive. FieldId: {FieldId}");
            if (PlayerDeploymentDepth < 1 || EnemyDeploymentDepth < 1)
                throw new InvalidOperationException($"[FieldData] Deployment depth must be positive. FieldId: {FieldId}");
        }
    }
}
