using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground.Item
{
    /// <summary>챕터 키와 배경 원본을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다.</summary>
    [CreateAssetMenu(fileName = "NewStageMapBackgroundData", menuName = "ProjectL/Database/View/StageMapBackgroundData")]
    internal sealed class StageMapBackgroundDataSO : DatabaseItem
    {
        [Tooltip("현재 진행 Buffer의 맵 키와 대응하는 챕터 키입니다.")]
        [SerializeField, Min(1)] private int chapterKey = 1;

        [Tooltip("소품 배치를 포함한 배경 프리팹입니다. 활성 루트의 위치 0·회전 identity·배율 1이 필요합니다.")]
        [SerializeField] private GameObject backgroundPrefab;

        public override IData GetData() => new StageMapBackgroundData(chapterKey, backgroundPrefab);
    }
}
