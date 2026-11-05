using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement
{
    public sealed class EnhancementStageDetail : IStageData
    {
        public StageType StageType => StageType.Shop;
        /// <summary>미설정이면 null이며, 새 방문 진입은 기존 Core 경계에서 거절한다.</summary>
        public TrainingPrices TrainingPrices { get; }

        public EnhancementStageDetail(TrainingPrices trainingPrices = null)
        {
            TrainingPrices = trainingPrices;
        }

        public void Validate()
        {
            // 설정된 가격은 TrainingPrices 생성자가 검증하며, 미설정도 DB 로딩은 허용한다.
        }
    }
}
