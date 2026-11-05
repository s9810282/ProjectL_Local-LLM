using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item
{
    /// <summary>StageEconomyData의 입력 SO입니다. 전투 스테이지 SO가 이 에셋을 참조합니다.</summary>
    [CreateAssetMenu(fileName = "NewStageEconomyData", menuName = "ProjectL/Database/InGame/StageEconomyData")]
    internal sealed class StageEconomyDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int economyKey = 1;
        [SerializeField, Min(0)] private int initialGold;
        [SerializeField, Min(0)] private int manualDrawGoldCost;

        [SerializeField] private bool clearRewardsConfigured;
        [SerializeField, Min(0)] private int clearGold;
        [SerializeField, Min(0)] private int clearFame;
        [SerializeField, Min(0)] private int clearExperiencePerParticipant;

        /// <summary>다른 SO가 이 경제 규칙을 참조할 때 쓰는 키입니다.</summary>
        internal int EconomyKey => economyKey;

        public override IData GetData() => new StageEconomyData(economyKey, initialGold, manualDrawGoldCost,
            clearRewardsConfigured ? new StageClearRewardSpec(clearGold, clearFame, clearExperiencePerParticipant) : null);
    }
}
