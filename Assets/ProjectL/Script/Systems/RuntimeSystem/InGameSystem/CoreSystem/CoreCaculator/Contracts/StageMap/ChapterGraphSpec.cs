using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 챕터 그래프 생성에 필요한 입력 묶음이다. 어떤 DB에서 읽었는지와 무관하며, 시드는 호출자가 이미 정한 값이다.
    /// 그래프는 시작 노드(깊이 0) → 일반 스테이지 층들 → 보스 노드(마지막 깊이) 모양이다.
    /// 일반 노드는 종류별 후보 묶음(StagePools)의 최소 개수와 가중치로 채운다.
    /// </summary>
    public sealed class ChapterGraphSpec
    {
        /// <summary>SingleFirstLayer일 때 깊이 1의 유일한 노드 ID. 시작 노드가 1이고 그다음 번호다.</summary>
        public const int FirstLayerNodeId = 2;

        /// <summary>시작 노드와 보스 노드를 포함한 층 수. 2 이상.</summary>
        public int DepthCount { get; }
        /// <summary>중간 층 하나의 최대 노드 수. 층마다 WidthMin~이 값 사이로 정한다.</summary>
        public int WidthMax { get; }
        /// <summary>중간 층 하나의 최소 노드 수(1~WidthMax).</summary>
        public int WidthMin { get; }
        /// <summary>깊이 1(시작 바로 다음 층)을 노드 하나로 만드는지. 다른 층은 WidthMin~WidthMax 그대로다.</summary>
        public bool SingleFirstLayer { get; }
        public int Seed { get; }
        public ChapterGraphConnection Connection { get; }
        /// <summary>WeightedRandom에서 가장 가까운 노드의 가중치.</summary>
        public float NearestWeight { get; }
        /// <summary>WeightedRandom에서 나머지 노드 하나의 가중치.</summary>
        public float OtherWeight { get; }
        /// <summary>
        /// 한 갈래인 노드가 지금 대상의 바로 옆 노드에 한 갈래 더 이어질 확률(0~1). 노드는 최대 두 갈래이고,
        /// 같은 층 사이의 다른 선과 교차하면 잇지 않는다. 연결 방식과 무관하게 같은 규칙이다.
        /// </summary>
        public float AdditionalConnectionChance { get; }
        /// <summary>
        /// 맵 전체에 둘 갈래 노드의 최소 수. 0이면 보장하지 않는다. 갈래 노드는 노드가 둘 이상인 층에서 다음 노드 둘 이상과
        /// 이어진 노드다. 모자라면 줄 바꾸기와 같은 규칙으로 더 잇고, 맵 모양상 더 이을 곳이 없으면 있는 만큼만 둔다.
        /// </summary>
        public int MinimumBranchCount { get; }
        /// <summary>종류별 후보 묶음. 종류 오름차순이며 종류·스테이지 키가 묶음 사이에서 겹치지 않는다.</summary>
        public IReadOnlyList<ChapterStagePool> StagePools { get; }
        public int BossStageKey { get; }
        /// <summary>스테이지를 정해 둘 중간 노드. 없으면 null.</summary>
        public ChapterStagePin Pin { get; }
        /// <summary>
        /// 깊이 1 노드(FirstLayerNodeId)에 늘 놓을 일반 스테이지. 0이면 없음. SingleFirstLayer가 켜져 있어야 한다.
        /// 이 스테이지는 다른 노드의 무작위 추첨에서 빠진다. 개발 진입 고정이 같은 노드를 정하면 그쪽이 이긴다.
        /// </summary>
        public int FirstStageKey { get; }
        /// <summary>FirstStageKey를 실제로 깊이 1 노드에 놓는지. 개발 진입 고정이 그 노드를 정하면 거짓이다.</summary>
        public bool PlacesFirstStage => FirstStageKey != 0 && (Pin == null || Pin.NodeId.Value != FirstLayerNodeId);

        public ChapterGraphSpec(int depthCount, int widthMax, int widthMin, bool singleFirstLayer, int seed, ChapterGraphConnection connection,
            float nearestWeight, float otherWeight, float additionalConnectionChance,
            IReadOnlyList<ChapterStagePool> stagePools, int bossStageKey, ChapterStagePin pin = null, int firstStageKey = 0,
            int minimumBranchCount = 0)
        {
            if (firstStageKey < 0) throw new ArgumentOutOfRangeException(nameof(firstStageKey));
            if (minimumBranchCount < 0) throw new ArgumentOutOfRangeException(nameof(minimumBranchCount));
            if (depthCount < 2) throw new ArgumentOutOfRangeException(nameof(depthCount));
            if (widthMax < 1) throw new ArgumentOutOfRangeException(nameof(widthMax));
            if (widthMin < 1 || widthMin > widthMax) throw new ArgumentOutOfRangeException(nameof(widthMin));
            if (!System.Enum.IsDefined(typeof(ChapterGraphConnection), connection))
                throw new ArgumentOutOfRangeException(nameof(connection));
            if (float.IsNaN(nearestWeight) || nearestWeight <= 0) throw new ArgumentOutOfRangeException(nameof(nearestWeight));
            if (float.IsNaN(otherWeight) || otherWeight <= 0) throw new ArgumentOutOfRangeException(nameof(otherWeight));
            if (float.IsNaN(additionalConnectionChance) || additionalConnectionChance < 0 || additionalConnectionChance > 1)
                throw new ArgumentOutOfRangeException(nameof(additionalConnectionChance));
            if (stagePools == null) throw new ArgumentNullException(nameof(stagePools));
            if (bossStageKey < 1) throw new ArgumentOutOfRangeException(nameof(bossStageKey));

            var pools = new ChapterStagePool[stagePools.Count];
            for (int i = 0; i < pools.Length; i++)
                pools[i] = stagePools[i] ?? throw new ArgumentException("후보 묶음 목록에 빈 항목이 있습니다.", nameof(stagePools));
            Array.Sort(pools, (a, b) => a.Rule.Kind.CompareTo(b.Rule.Kind));

            var kinds = new HashSet<ChapterStageKind>();
            var keys = new HashSet<int>();
            var rules = new ChapterStageDistributionRule[pools.Length];
            long totalWeight = 0;
            ChapterStageKind? pinnedKind = null;
            ChapterStagePool firstPool = null;
            for (int i = 0; i < pools.Length; i++)
            {
                ChapterStagePool pool = pools[i];
                if (!kinds.Add(pool.Rule.Kind))
                    throw new ArgumentException($"{pool.Rule.Kind} 후보 묶음이 둘 이상입니다.", nameof(stagePools));
                foreach (int key in pool.StageKeys)
                {
                    if (key == bossStageKey)
                        throw new ArgumentException($"보스 스테이지 {key}가 {pool.Rule.Kind} 후보에 있습니다.", nameof(stagePools));
                    if (!keys.Add(key))
                        throw new ArgumentException($"스테이지 {key}가 둘 이상의 후보 묶음에 있습니다.", nameof(stagePools));
                    if (pin != null && pin.StageKey.Value == key) pinnedKind = pool.Rule.Kind;
                    if (firstStageKey == key) firstPool = pool;
                }
                rules[i] = pool.Rule;
                totalWeight += pool.Rule.Weight;
            }
            if (totalWeight == 0)
                throw new ArgumentException("남은 자리를 채울 종류가 없습니다. 가중치가 하나 이상 양수여야 합니다.", nameof(stagePools));
            if (pin != null && pinnedKind == null)
                throw new ArgumentException(
                    $"고정할 스테이지 {pin.StageKey.Value}는 이 맵의 일반 스테이지 후보가 아닙니다(보스 {bossStageKey}).", nameof(pin));

            bool placesFirst = firstStageKey != 0 && (pin == null || pin.NodeId.Value != FirstLayerNodeId);
            if (firstStageKey != 0)
            {
                if (!singleFirstLayer)
                    throw new ArgumentException("첫 층 스테이지를 고정하려면 첫 층 노드 1개가 켜져 있어야 합니다.", nameof(firstStageKey));
                if (firstPool == null)
                    throw new ArgumentException(
                        $"첫 층 스테이지 {firstStageKey}는 이 맵의 일반 스테이지 후보가 아닙니다(보스 {bossStageKey}).", nameof(firstStageKey));
                // 첫 층 스테이지는 다른 노드의 추첨에서 빠진다. 그 종류가 다른 노드에도 놓일 수 있으면 다른 후보가 있어야 한다.
                int covered = (placesFirst ? 1 : 0) + (pinnedKind == firstPool.Rule.Kind ? 1 : 0);
                bool drawsMore = firstPool.Rule.Weight > 0 || firstPool.Rule.MinimumCount > covered;
                if (drawsMore && firstPool.StageKeys.Count < 2)
                    throw new ArgumentException(
                        $"첫 층 스테이지 {firstStageKey}는 다른 노드에 나오지 않습니다. {firstPool.Rule.Kind} 노드가 다른 깊이에도 생길 수 있어 후보가 하나 더 필요합니다.",
                        nameof(firstStageKey));
            }

            string placementError = ChapterStageDistributionRule.FindPlacementError(rules, depthCount - 2, widthMin, singleFirstLayer, pinnedKind,
                placesFirst ? firstPool.Rule.Kind : (ChapterStageKind?)null);
            if (placementError != null) throw new ArgumentException(placementError, nameof(stagePools));

            DepthCount = depthCount;
            WidthMax = widthMax;
            WidthMin = widthMin;
            SingleFirstLayer = singleFirstLayer;
            Seed = seed;
            Connection = connection;
            NearestWeight = nearestWeight;
            OtherWeight = otherWeight;
            AdditionalConnectionChance = additionalConnectionChance;
            MinimumBranchCount = minimumBranchCount;
            StagePools = Array.AsReadOnly(pools);
            BossStageKey = bossStageKey;
            Pin = pin;
            FirstStageKey = firstStageKey;
        }

        /// <summary>
        /// 옛 균등 추첨 입력 형태다. 컴파일 호환만을 위해 남겼으며 어떤 동작도 하지 않는다.
        /// 종류별 후보 묶음을 받는 생성자를 쓴다.
        /// </summary>
        [Obsolete("종류별 후보 묶음(ChapterStagePool)을 받는 생성자를 쓰세요. 이 생성자는 항상 NotSupportedException을 던집니다.")]
        public ChapterGraphSpec(int depthCount, int widthMax, int seed, ChapterGraphConnection connection,
            float nearestWeight, float otherWeight, float additionalConnectionChance,
            IReadOnlyList<int> regularStageKeys, int bossStageKey)
        {
            throw new NotSupportedException(
                "일반 스테이지 키 목록으로 그래프를 만들지 않습니다. ChapterStagePool 목록을 받는 생성자를 쓰세요.");
        }
    }
}
