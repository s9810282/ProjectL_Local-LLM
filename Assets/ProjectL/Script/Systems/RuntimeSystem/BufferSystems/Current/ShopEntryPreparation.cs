using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>상점 진입의 선택 원본과 함께 설치할 대상 및 고정 상품이다.</summary>
    public sealed class ShopEntryPreparation
    {
        public int ExpectedChapterKey { get; }
        public bool ExpectedHasSelection { get; }
        public StageSelectionData ExpectedStage { get; }
        public StageNodeId TargetNodeId { get; }
        public StageKey TargetStageKey { get; }
        public string TargetStageName { get; }
        /// <summary>Core가 진입 가능하다고 판단한 대상 노드의 종류·역할이다. Buffer는 저장된 노드와 같은지만 비교한다.</summary>
        public StageType ExpectedTargetStageType { get; }
        public StageMapRole ExpectedTargetMapRole { get; }
        public IReadOnlyList<ShopPurchaseSnapshot> Offers { get; }

        public ShopEntryPreparation(int expectedChapterKey, bool expectedHasSelection,
            StageSelectionData expectedStage, StageNodeId targetNodeId, StageKey targetStageKey,
            string targetStageName, IReadOnlyList<ShopPurchaseSnapshot> offers,
            StageType expectedTargetStageType, StageMapRole expectedTargetMapRole)
        {
            StageSelectionData.RequireEntry(expectedChapterKey, expectedHasSelection, expectedStage,
                targetNodeId, targetStageKey, targetStageName);
            if (!System.Enum.IsDefined(typeof(StageType), expectedTargetStageType))
                throw new ArgumentOutOfRangeException(nameof(expectedTargetStageType));
            if (!System.Enum.IsDefined(typeof(StageMapRole), expectedTargetMapRole))
                throw new ArgumentOutOfRangeException(nameof(expectedTargetMapRole));

            // 기존 상점 계약으로 슬롯을 검증하고 호출자의 컬렉션과 분리한다.
            Offers = new CurrentShopData(offers).Offers;
            ExpectedChapterKey = expectedChapterKey;
            ExpectedHasSelection = expectedHasSelection;
            ExpectedStage = expectedStage;
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
            TargetStageName = targetStageName;
            ExpectedTargetStageType = expectedTargetStageType;
            ExpectedTargetMapRole = expectedTargetMapRole;
        }
    }
}
