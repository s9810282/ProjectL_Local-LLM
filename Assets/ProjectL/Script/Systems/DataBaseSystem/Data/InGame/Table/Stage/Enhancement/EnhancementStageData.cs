using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement
{
    [CreateAssetMenu(fileName = "EnhancementStage", menuName = "ProjectL/Database/InGame/Stage/Enhancement")]
    public sealed class EnhancementStageData : StageBaseData
    {
        [SerializeField] private int price1To2;
        [SerializeField] private int price2To3;
        public override StageType StageType => StageType.Shop;

        protected override IStageData CreateStageData()
        {
            var prices = price1To2 == 0 && price2To3 == 0 ? null : new TrainingPrices(price1To2, price2To3);
            return new EnhancementStageDetail(prices);
        }
    }
}
