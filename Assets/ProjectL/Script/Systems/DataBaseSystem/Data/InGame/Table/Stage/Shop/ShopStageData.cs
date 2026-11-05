using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;

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

            public EntityKey Key => unitDefinition != null ? unitDefinition.Key : EntityKey.None;
        }

        [SerializeField] private CandidateInput[] candidates = new CandidateInput[0];
        [SerializeField] private ConsumableItemDataSO[] items = new ConsumableItemDataSO[0];
        [SerializeField, Min(0)] private int unitRecruitCostGold = 600;
        [SerializeField, Min(1)] private int itemCategoryWeight = 2;
        [SerializeField, Min(1)] private int unitCategoryWeight = 1;

        public override StageType StageType => StageType.Shop;

        protected override IStageData CreateStageData()
        {
            var unitKeys = new EntityKey[candidates?.Length ?? 0];
            for (int i = 0; i < unitKeys.Length; i++) unitKeys[i] = candidates[i].Key;
            var itemKeys = new int[items?.Length ?? 0];
            for (int i = 0; i < itemKeys.Length; i++) itemKeys[i] = items[i] != null ? items[i].Key : 0;
            return new ShopStageDetail(unitKeys, itemKeys, unitRecruitCostGold, itemCategoryWeight, unitCategoryWeight);
        }
    }
}
