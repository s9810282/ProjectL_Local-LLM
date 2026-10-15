using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop
{
    /// <summary>
    /// ShopStageDetail의 입력 SO입니다. 후보 유닛은 UnitDataSO를 끌어다 놓고, CreateStageData에서 EntityKey로 바꿉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopStageData", menuName = "ProjectL/Database/InGame/Stage/Shop")]
    public sealed class ShopStageData : StageBaseData
    {
        [Serializable]
        private struct CandidateInput
        {
            [SerializeField] private UnitDataSO unitDefinition;
            [SerializeField, Min(0)] private int cost;

            public ShopCandidate ToData()
            {
                EntityKey key = unitDefinition != null ? unitDefinition.Key : EntityKey.None;
                return new ShopCandidate(key, cost);
            }
        }

        [SerializeField] private CandidateInput[] candidates = new CandidateInput[0];

        public override StageType StageType => StageType.Shop;

        protected override IStageData CreateStageData()
        {
            var data = new ShopCandidate[candidates?.Length ?? 0];
            for (int i = 0; i < data.Length; i++)
                data[i] = candidates[i].ToData();
            return new ShopStageDetail(data);
        }
    }
}
