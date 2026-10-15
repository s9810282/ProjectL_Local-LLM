using System;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item
{
    internal enum StageConnectionAlgorithm
    {
        Nearest,
        Random,
        WeightedRandom
    }

    /// <summary>
    /// 스테이지 맵 생성 규칙입니다. Core가 받는 순수 C# 데이터이며 StageMapDataSO가 생성합니다.
    /// </summary>
    internal sealed class StageMapData : IData
    {
        public int MapKey { get; }
        public int DepthCount { get; }
        public int WidthMax { get; }
        public float AdditionalConnectionChance { get; }
        public StageConnectionAlgorithm ConnectionAlgorithm { get; }
        public float NearestConnectionWeight { get; }
        public float OtherConnectionWeight { get; }
        public int Seed { get; }
        public bool RandomizeSeedEachRun { get; }

        public StageMapData(
            int mapKey,
            int depthCount,
            int widthMax,
            float additionalConnectionChance,
            StageConnectionAlgorithm connectionAlgorithm,
            float nearestConnectionWeight,
            float otherConnectionWeight,
            int seed,
            bool randomizeSeedEachRun)
        {
            MapKey = mapKey;
            DepthCount = depthCount;
            WidthMax = widthMax;
            AdditionalConnectionChance = additionalConnectionChance;
            ConnectionAlgorithm = connectionAlgorithm;
            NearestConnectionWeight = nearestConnectionWeight;
            OtherConnectionWeight = otherConnectionWeight;
            Seed = seed;
            RandomizeSeedEachRun = randomizeSeedEachRun;
        }

        internal void Validate()
        {
            if (MapKey < 1 || DepthCount < 2 || WidthMax < 1)
                throw new InvalidOperationException("Stage map keys and dimensions are invalid.");
            if (float.IsNaN(AdditionalConnectionChance) || AdditionalConnectionChance < 0 || AdditionalConnectionChance > 1)
                throw new InvalidOperationException("Additional connection chance must be between 0 and 1.");
            if (!System.Enum.IsDefined(typeof(StageConnectionAlgorithm), ConnectionAlgorithm) ||
                NearestConnectionWeight <= 0 || OtherConnectionWeight <= 0)
                throw new InvalidOperationException("Stage connection settings are invalid.");
        }
    }
}
