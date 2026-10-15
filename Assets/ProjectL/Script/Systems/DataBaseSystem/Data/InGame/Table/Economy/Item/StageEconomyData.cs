using System;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item
{
    /// <summary>
    /// 전투 스테이지의 재화 규칙입니다. Core가 받는 순수 C# 데이터이며 StageEconomyDataSO가 생성합니다.
    /// Core StageEconomySpec(initialGold, manualDrawGoldCost)에 대응합니다.
    /// </summary>
    public sealed class StageEconomyData : IData
    {
        public int EconomyKey { get; }
        public int InitialGold { get; }
        public int ManualDrawGoldCost { get; }

        public StageEconomyData(int economyKey, int initialGold, int manualDrawGoldCost)
        {
            EconomyKey = economyKey;
            InitialGold = initialGold;
            ManualDrawGoldCost = manualDrawGoldCost;
        }

        public void Validate()
        {
            if (EconomyKey < 1)
                throw new InvalidOperationException("[StageEconomyData] EconomyKey must be positive.");
            if (InitialGold < 0 || ManualDrawGoldCost < 0)
                throw new InvalidOperationException($"[StageEconomyData] Gold values must not be negative. EconomyKey: {EconomyKey}");
        }
    }
}
