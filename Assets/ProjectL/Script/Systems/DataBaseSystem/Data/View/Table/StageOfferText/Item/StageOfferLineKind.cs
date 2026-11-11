namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item
{
    /// <summary>카드 안 글의 종류입니다. 값이 끼는 글은 꼴이고, 칸 차례는 <see cref="StageOfferTextData.ValueCountOf"/>가 정합니다.</summary>
    internal enum StageOfferLineKind
    {
        None = 0,
        UnitDetails = 1,
        ItemDetails = 2,
        // 3은 삭제된 옛 강화 상품 문구 번호로 비워 둡니다.
        RecoveryProjected = 4,
        TraitGrowthHeader = 5,
        TraitAcquireLabel = 6,
        TraitEnhanceLabel = 7,
        TrainingDetails = 8,
        TrainingRank = 9,
        TrainingEmpty = 10,
        TrainingUnitLevel = 11,
        TrainingUnitTraits = 12,
        TrainingUnitNoTrait = 13,
        TrainingTraitPrompt = 14,
        RecoveryMaxHealthProjected = 15,
        TrainingEffectEnabled = 16,
        TrainingEffectDisabled = 17
    }
}
