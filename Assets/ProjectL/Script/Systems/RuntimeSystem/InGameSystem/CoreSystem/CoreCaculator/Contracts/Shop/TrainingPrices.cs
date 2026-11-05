using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    /// <summary>훈련 방문에 적용되는 두 단계의 독립적인 가격이다.</summary>
    public sealed class TrainingPrices
    {
        public int Price1To2 { get; }
        public int Price2To3 { get; }

        public TrainingPrices(int price1To2, int price2To3)
        {
            if (price1To2 <= 0) throw new ArgumentOutOfRangeException(nameof(price1To2));
            if (price2To3 <= price1To2) throw new ArgumentOutOfRangeException(nameof(price2To3));
            Price1To2 = price1To2;
            Price2To3 = price2To3;
        }

        public int GetPrice(int rank) => rank switch
        {
            1 => Price1To2,
            2 => Price2To3,
            _ => throw new ArgumentOutOfRangeException(nameof(rank))
        };
    }
}
