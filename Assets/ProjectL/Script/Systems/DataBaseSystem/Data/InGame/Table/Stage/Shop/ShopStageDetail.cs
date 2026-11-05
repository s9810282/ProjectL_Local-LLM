using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop
{
    /// <summary>상점 후보 키와 모집 가격·추첨 가중치의 불변 정의다.</summary>
    public sealed class ShopStageDetail : IStageData
    {
        public StageType StageType => StageType.Shop;
        public IReadOnlyList<EntityKey> UnitKeys { get; }
        public IReadOnlyList<int> ItemKeys { get; }
        public int UnitRecruitCostGold { get; }
        public int ItemCategoryWeight { get; }
        public int UnitCategoryWeight { get; }

        public ShopStageDetail(IReadOnlyList<EntityKey> unitKeys, IReadOnlyList<int> itemKeys,
            int unitRecruitCostGold = 600, int itemCategoryWeight = 2, int unitCategoryWeight = 1)
        {
            UnitKeys = Array.AsReadOnly((unitKeys ?? throw new ArgumentNullException(nameof(unitKeys))).ToArray());
            ItemKeys = Array.AsReadOnly((itemKeys ?? throw new ArgumentNullException(nameof(itemKeys))).ToArray());
            UnitRecruitCostGold = unitRecruitCostGold;
            ItemCategoryWeight = itemCategoryWeight;
            UnitCategoryWeight = unitCategoryWeight;
        }

        public void Validate()
        {
            if (UnitRecruitCostGold < 0 || ItemCategoryWeight < 1 || UnitCategoryWeight < 1)
                throw new InvalidOperationException("상점 가격은 음수일 수 없고 가중치는 양수여야 합니다.");
            if (UnitKeys.Any(key => (int)key <= 0) || UnitKeys.Distinct().Count() != UnitKeys.Count)
                throw new InvalidOperationException("상점 유닛 키는 양수이며 중복되지 않아야 합니다.");
            if (ItemKeys.Any(key => key <= 0) || ItemKeys.Distinct().Count() != ItemKeys.Count)
                throw new InvalidOperationException("상점 아이템 키는 양수이며 중복되지 않아야 합니다.");
        }
    }
}
