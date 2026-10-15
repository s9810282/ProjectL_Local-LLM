using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item
{
    /// <summary>StageMapData의 입력 SO입니다. 기획 값은 이 에셋에서 수정합니다.</summary>
    [CreateAssetMenu(fileName = "NewStageMapData", menuName = "ProjectL/Database/InGame/StageMapData")]
    internal sealed class StageMapDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int mapKey = 1;
        [SerializeField, Min(2)] private int depthCount = 8;
        [SerializeField, Min(1)] private int widthMax = 3;
        [SerializeField, Range(0f, 1f)] private float additionalConnectionChance = 0.25f;
        [SerializeField] private StageConnectionAlgorithm connectionAlgorithm = StageConnectionAlgorithm.Nearest;
        [SerializeField, Min(0.01f)] private float nearestConnectionWeight = 10f;
        [SerializeField, Min(0.01f)] private float otherConnectionWeight = 1f;
        [SerializeField] private int seed;
        [SerializeField] private bool randomizeSeedEachRun;

        public override IData GetData()
        {
            return new StageMapData(
                mapKey,
                depthCount,
                widthMax,
                additionalConnectionChance,
                connectionAlgorithm,
                nearestConnectionWeight,
                otherConnectionWeight,
                seed,
                randomizeSeedEachRun);
        }
    }
}
