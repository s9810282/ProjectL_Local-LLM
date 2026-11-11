using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

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
        /// <summary>모든 중간 층의 최소 노드 수입니다(1~WidthMax).</summary>
        public int WidthMin { get; }
        /// <summary>깊이 1(시작 바로 다음 층)을 노드 하나로 만드는지입니다.</summary>
        public bool SingleFirstLayer { get; }
        /// <summary>
        /// 깊이 1 노드에 늘 놓을 일반 스테이지 키입니다(챕터 1-1 고정 맵 등). 0이면 무작위입니다.
        /// SingleFirstLayer가 켜져 있어야 하며, 이 스테이지는 다른 노드의 추첨에 나오지 않습니다.
        /// 그 맵의 일반 스테이지인지는 그래프를 만들 때(ChapterGraphSpec) 검사합니다.
        /// </summary>
        public int FirstStageKey { get; }
        /// <summary>한 갈래인 노드가 바로 옆 다음 층 노드에 한 갈래 더 이어질 확률(0~1)입니다. 규칙은 ChapterGraphSpec 문서를 따릅니다.</summary>
        public float AdditionalConnectionChance { get; }
        /// <summary>맵 전체에 둘 갈래 노드의 최소 수입니다. 0이면 보장하지 않습니다.</summary>
        public int MinimumBranchCount { get; }
        public StageConnectionAlgorithm ConnectionAlgorithm { get; }
        public float NearestConnectionWeight { get; }
        public float OtherConnectionWeight { get; }
        public int Seed { get; }
        public bool RandomizeSeedEachRun { get; }

        /// <summary>이 챕터의 보스를 깬 뒤 이어갈 챕터 키입니다. 0이면 마지막 챕터입니다(챕터 연결의 유일한 원본).</summary>
        public int NextChapterKey { get; }

        /// <summary>일반 노드의 종류별 출현 규칙입니다(종류 오름차순). 운영 맵은 네 종류가 하나씩 있어야 합니다.</summary>
        public IReadOnlyList<ChapterStageDistributionRule> StageDistribution { get; }

        public StageMapData(
            int mapKey,
            int depthCount,
            int widthMax,
            int widthMin,
            bool singleFirstLayer,
            int firstStageKey,
            float additionalConnectionChance,
            int minimumBranchCount,
            StageConnectionAlgorithm connectionAlgorithm,
            float nearestConnectionWeight,
            float otherConnectionWeight,
            int seed,
            bool randomizeSeedEachRun,
            int nextChapterKey,
            IReadOnlyList<ChapterStageDistributionRule> stageDistribution)
        {
            if (stageDistribution == null) throw new ArgumentNullException(nameof(stageDistribution));
            var rules = new ChapterStageDistributionRule[stageDistribution.Count];
            for (int i = 0; i < rules.Length; i++)
                rules[i] = stageDistribution[i] ?? throw new ArgumentException("출현 규칙 목록에 빈 항목이 있습니다.", nameof(stageDistribution));
            Array.Sort(rules, (a, b) => a.Kind.CompareTo(b.Kind));

            MapKey = mapKey;
            DepthCount = depthCount;
            WidthMax = widthMax;
            WidthMin = widthMin;
            SingleFirstLayer = singleFirstLayer;
            FirstStageKey = firstStageKey;
            AdditionalConnectionChance = additionalConnectionChance;
            MinimumBranchCount = minimumBranchCount;
            ConnectionAlgorithm = connectionAlgorithm;
            NearestConnectionWeight = nearestConnectionWeight;
            OtherConnectionWeight = otherConnectionWeight;
            Seed = seed;
            RandomizeSeedEachRun = randomizeSeedEachRun;
            NextChapterKey = nextChapterKey;
            StageDistribution = Array.AsReadOnly(rules);
        }

        /// <summary>
        /// 출현 설정 이전의 형태입니다. 컴파일 호환만을 위해 남겼으며 출현 규칙이 비어 있어 Validate가 거절합니다.
        /// </summary>
        [Obsolete("출현 규칙(stageDistribution)을 받는 생성자를 쓰세요. 이 생성자로 만든 맵은 Validate를 통과하지 못합니다.")]
        public StageMapData(
            int mapKey,
            int depthCount,
            int widthMax,
            float additionalConnectionChance,
            StageConnectionAlgorithm connectionAlgorithm,
            float nearestConnectionWeight,
            float otherConnectionWeight,
            int seed,
            bool randomizeSeedEachRun,
            int nextChapterKey)
            : this(mapKey, depthCount, widthMax, 1, false, 0, additionalConnectionChance, 0, connectionAlgorithm, nearestConnectionWeight,
                otherConnectionWeight, seed, randomizeSeedEachRun, nextChapterKey, Array.Empty<ChapterStageDistributionRule>())
        {
        }

        internal void Validate()
        {
            if (MapKey < 1 || DepthCount < 2 || WidthMax < 1)
                throw new InvalidOperationException("Stage map keys and dimensions are invalid.");
            if (WidthMin < 1 || WidthMin > WidthMax)
                throw new InvalidOperationException($"MapKey {MapKey}의 최소 폭 {WidthMin}은 1 이상, 최대 폭 {WidthMax} 이하여야 합니다.");
            if (FirstStageKey < 0)
                throw new InvalidOperationException($"MapKey {MapKey}의 첫 층 스테이지 키가 잘못되었습니다: {FirstStageKey}.");
            if (FirstStageKey > 0 && !SingleFirstLayer)
                throw new InvalidOperationException(
                    $"MapKey {MapKey}의 첫 층 스테이지 {FirstStageKey}를 쓰려면 첫 층 노드 1개(singleFirstLayer)를 켜야 합니다.");
            if (FirstStageKey > 0 && DepthCount < 3)
                throw new InvalidOperationException($"MapKey {MapKey}는 중간 층이 없어 첫 층 스테이지 {FirstStageKey}를 놓을 수 없습니다.");
            if (float.IsNaN(AdditionalConnectionChance) || AdditionalConnectionChance < 0 || AdditionalConnectionChance > 1)
                throw new InvalidOperationException("Additional connection chance must be between 0 and 1.");
            if (MinimumBranchCount < 0)
                throw new InvalidOperationException($"MapKey {MapKey}의 맵당 최소 갈래 수는 0 이상이어야 합니다: {MinimumBranchCount}.");
            if (!System.Enum.IsDefined(typeof(StageConnectionAlgorithm), ConnectionAlgorithm) ||
                NearestConnectionWeight <= 0 || OtherConnectionWeight <= 0)
                throw new InvalidOperationException("Stage connection settings are invalid.");
            if (NextChapterKey < 0 || NextChapterKey == MapKey)
                throw new InvalidOperationException($"MapKey {MapKey}의 다음 챕터 키가 잘못되었습니다: {NextChapterKey}.");
            ValidateStageDistribution();
        }

        // 운영 맵 정책: 네 종류가 하나씩, 일반 상점·회복은 최소 1개, 남은 자리를 채울 가중치, 깊이별 최소 배치 자리.
        private void ValidateStageDistribution()
        {
            var kinds = (ChapterStageKind[])System.Enum.GetValues(typeof(ChapterStageKind));
            bool fourKinds = StageDistribution.Count == kinds.Length;
            for (int i = 0; fourKinds && i < kinds.Length; i++) fourKinds = StageDistribution[i].Kind == kinds[i];
            if (!fourKinds)
                throw new InvalidOperationException(
                    $"MapKey {MapKey}의 스테이지 출현 설정은 전투·상점·회복·강화가 하나씩 있어야 합니다(현재 {StageDistribution.Count}개).");

            long totalWeight = 0;
            foreach (ChapterStageDistributionRule rule in StageDistribution)
            {
                totalWeight += rule.Weight;
                if ((rule.Kind == ChapterStageKind.Shop || rule.Kind == ChapterStageKind.Recovery) && rule.MinimumCount < 1)
                    throw new InvalidOperationException($"MapKey {MapKey}의 {rule.Kind} 최소 개수는 1 이상이어야 합니다: {rule.MinimumCount}.");
            }
            if (totalWeight == 0)
                throw new InvalidOperationException($"MapKey {MapKey}의 스테이지 출현 가중치가 모두 0입니다.");

            string placementError = ChapterStageDistributionRule.FindPlacementError(StageDistribution, DepthCount - 2, WidthMin, SingleFirstLayer);
            if (placementError != null)
                throw new InvalidOperationException(
                    $"MapKey {MapKey}의 출현 설정을 배치할 수 없습니다: {placementError} 깊이·최소 폭을 늘리거나 최소 개수·출현 시작 깊이를 줄이세요.");
        }
    }
}
