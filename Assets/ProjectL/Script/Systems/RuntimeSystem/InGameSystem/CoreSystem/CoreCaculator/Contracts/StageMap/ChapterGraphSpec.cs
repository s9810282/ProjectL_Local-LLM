using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 챕터 그래프 생성에 필요한 입력 묶음이다. 어떤 DB에서 읽었는지와 무관하며, 시드는 호출자가 이미 정한 값이다.
    /// 그래프는 시작 노드(깊이 0) → 일반 스테이지 층들 → 보스 노드(마지막 깊이) 모양이다.
    /// </summary>
    public sealed class ChapterGraphSpec
    {
        /// <summary>시작 노드와 보스 노드를 포함한 층 수. 2 이상.</summary>
        public int DepthCount { get; }
        /// <summary>중간 층 하나의 최대 노드 수. 층마다 1~이 값 사이로 정한다.</summary>
        public int WidthMax { get; }
        public int Seed { get; }
        public ChapterGraphConnection Connection { get; }
        /// <summary>WeightedRandom에서 가장 가까운 노드의 가중치.</summary>
        public float NearestWeight { get; }
        /// <summary>WeightedRandom에서 나머지 노드 하나의 가중치.</summary>
        public float OtherWeight { get; }
        /// <summary>노드마다 다음 층으로 연결을 하나 더 만들 확률(0~1).</summary>
        public float AdditionalConnectionChance { get; }
        /// <summary>중간 층 노드에 무작위로 배정할 스테이지 키. 중복을 허용하며 순서가 결과에 영향을 준다.</summary>
        public IReadOnlyList<int> RegularStageKeys { get; }
        public int BossStageKey { get; }

        public ChapterGraphSpec(int depthCount, int widthMax, int seed, ChapterGraphConnection connection,
            float nearestWeight, float otherWeight, float additionalConnectionChance,
            IReadOnlyList<int> regularStageKeys, int bossStageKey)
        {
            if (depthCount < 2) throw new ArgumentOutOfRangeException(nameof(depthCount));
            if (widthMax < 1) throw new ArgumentOutOfRangeException(nameof(widthMax));
            if (!System.Enum.IsDefined(typeof(ChapterGraphConnection), connection))
                throw new ArgumentOutOfRangeException(nameof(connection));
            if (float.IsNaN(nearestWeight) || nearestWeight <= 0) throw new ArgumentOutOfRangeException(nameof(nearestWeight));
            if (float.IsNaN(otherWeight) || otherWeight <= 0) throw new ArgumentOutOfRangeException(nameof(otherWeight));
            if (float.IsNaN(additionalConnectionChance) || additionalConnectionChance < 0 || additionalConnectionChance > 1)
                throw new ArgumentOutOfRangeException(nameof(additionalConnectionChance));
            if (regularStageKeys == null) throw new ArgumentNullException(nameof(regularStageKeys));
            if (regularStageKeys.Count == 0)
                throw new ArgumentException("일반 스테이지 후보가 하나 이상 필요합니다.", nameof(regularStageKeys));
            var keys = new int[regularStageKeys.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                if (regularStageKeys[i] < 1)
                    throw new ArgumentException("스테이지 키는 1 이상이어야 합니다.", nameof(regularStageKeys));
                keys[i] = regularStageKeys[i];
            }
            if (bossStageKey < 1) throw new ArgumentOutOfRangeException(nameof(bossStageKey));

            DepthCount = depthCount;
            WidthMax = widthMax;
            Seed = seed;
            Connection = connection;
            NearestWeight = nearestWeight;
            OtherWeight = otherWeight;
            AdditionalConnectionChance = additionalConnectionChance;
            RegularStageKeys = Array.AsReadOnly(keys);
            BossStageKey = bossStageKey;
        }
    }
}
