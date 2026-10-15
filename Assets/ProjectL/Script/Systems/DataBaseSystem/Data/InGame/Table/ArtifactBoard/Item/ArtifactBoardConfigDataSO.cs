using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item
{
    /// <summary>ArtifactBoardConfigData의 입력 SO입니다. 테이블에 하나만 둡니다(설정형).</summary>
    [CreateAssetMenu(fileName = "NewArtifactBoardConfigData", menuName = "ProjectL/Database/InGame/ArtifactBoardConfigData")]
    internal sealed class ArtifactBoardConfigDataSO : DatabaseItem
    {
        [Header("Initial Board Size")]
        [Tooltip("첫 보드 초기화에 쓰는 폭입니다. 보상으로 늘어난 현재 크기는 Buffer가 가집니다.")]
        [SerializeField, Min(1)] private int initialWidth = 4;
        [Tooltip("첫 보드 초기화에 쓰는 높이입니다. 보상으로 늘어난 현재 크기는 Buffer가 가집니다.")]
        [SerializeField, Min(1)] private int initialHeight = 4;

        public override IData GetData() => new ArtifactBoardConfigData(initialWidth, initialHeight);
    }
}
