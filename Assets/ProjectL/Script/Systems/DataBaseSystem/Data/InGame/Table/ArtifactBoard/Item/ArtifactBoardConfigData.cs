using System;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item
{
    /// <summary>
    /// 아티팩트 보드의 처음 크기입니다. 실행 계층이 첫 보드 초기화에 쓰는 순수 C# 데이터이며 ArtifactBoardConfigDataSO가 생성합니다.
    /// </summary>
    /// <remarks>현재 크기는 보상 등으로 늘어날 수 있으므로 항상 ArtifactBuffer에서 읽습니다. 이 값은 처음 크기에만 씁니다.</remarks>
    public sealed class ArtifactBoardConfigData : IData
    {
        public int InitialWidth { get; }
        public int InitialHeight { get; }

        public ArtifactBoardConfigData(int initialWidth, int initialHeight)
        {
            InitialWidth = initialWidth;
            InitialHeight = initialHeight;
        }

        public void Validate()
        {
            if (InitialWidth < 1 || InitialHeight < 1)
                throw new InvalidOperationException("[ArtifactBoardConfigData] Initial board width and height must be 1 or more.");
        }
    }
}
