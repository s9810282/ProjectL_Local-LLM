using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StageMapGenerationConfig
    {
        /// <summary>
        /// 시작 지점과 보스 방을 포함한 전체 레이어 수
        /// </summary>
        public int DepthCount { get; }
        /// <summary>
        /// 일반 Depth 하나가 가질 수 있는 최대 노드
        /// </summary>
        public int WidthMax { get; }
        /// <summary>
        /// 가까운 두 번째 노드와 추가 Edge를 만들 확률 ( 0.0 ~ 1.0 사이 )
        /// </summary>
        public double AdditionalConnectionChance { get; }
        public int Seed { get; }

        public StageMapGenerationConfig(
            int depthCount,
            int widthMax,
            double additionalConnectionChance,
            int seed)
        {
            if (depthCount < 2)
                throw new ArgumentOutOfRangeException(nameof(depthCount));
            if (widthMax < 1)
                throw new ArgumentOutOfRangeException(nameof(widthMax));
            if (double.IsNaN(additionalConnectionChance) ||
                double.IsInfinity(additionalConnectionChance) ||
                additionalConnectionChance < 0.0 ||
                additionalConnectionChance > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalConnectionChance));
            }

            DepthCount = depthCount;
            WidthMax = widthMax;
            AdditionalConnectionChance = additionalConnectionChance;
            Seed = seed;
        }
    }
}