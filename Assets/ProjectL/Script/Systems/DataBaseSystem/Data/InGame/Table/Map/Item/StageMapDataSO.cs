using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item
{
    /// <summary>StageMapData의 입력 SO입니다. 기획 값은 이 에셋에서 수정합니다.</summary>
    [CreateAssetMenu(fileName = "NewStageMapData", menuName = "ProjectL/Database/InGame/StageMapData")]
    internal sealed class StageMapDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int mapKey = 1;
        [SerializeField, Min(2)] private int depthCount = 8;
        [SerializeField, Min(1)] private int widthMax = 3;
        [SerializeField, Min(1), Tooltip("모든 중간 층의 최소 노드 수입니다. 최대 폭 이하여야 합니다. 1이면 길이 한 칸으로 모이는 층이 나올 수 있습니다.")]
        private int widthMin = 1;
        [SerializeField, Tooltip("켜면 시작 바로 다음 층(깊이 1)은 노드 하나입니다. 모든 길이 그 노드를 지납니다. 다른 층은 최소~최대 폭 그대로입니다.")]
        private bool singleFirstLayer;
        [SerializeField, Min(0), Tooltip("첫 층(깊이 1) 노드에 늘 놓을 일반 스테이지 키입니다. 예: 챕터 1-1 고정 맵. 0이면 무작위입니다. " +
            "'첫 층 노드 1개'를 켜야 하고, 이 맵의 일반 스테이지여야 합니다. 이 스테이지는 다른 노드에는 나오지 않습니다.")]
        private int firstStageKey;
        [SerializeField, Range(0f, 1f)] private float additionalConnectionChance = 0.25f;
        [SerializeField] private StageConnectionAlgorithm connectionAlgorithm = StageConnectionAlgorithm.Nearest;
        [SerializeField, Min(0.01f)] private float nearestConnectionWeight = 10f;
        [SerializeField, Min(0.01f)] private float otherConnectionWeight = 1f;
        [SerializeField] private int seed;
        [SerializeField] private bool randomizeSeedEachRun;

        [SerializeField, Min(0), Tooltip("이 챕터의 보스를 깬 뒤 이어갈 챕터(맵) 키입니다. 0이면 마지막 챕터라 Run 승리로 끝납니다.")]
        private int nextChapterKey;

        [Header("스테이지 출현 설정 (시작·보스 노드 제외)")]
        [SerializeField, Tooltip("보스가 아닌 전투 스테이지입니다.")]
        private DistributionInput combatDistribution = new(80, 1, 1);
        [SerializeField, Tooltip("일반 상점입니다. 강화는 포함하지 않습니다. 최소 개수는 1 이상이어야 합니다.")]
        private DistributionInput shopDistribution = new(9, 1, 1);
        [SerializeField, Tooltip("회복 스테이지입니다. 최소 개수는 1 이상이어야 합니다.")]
        private DistributionInput recoveryDistribution = new(7, 1, 1);
        [SerializeField, Tooltip("강화(훈련소) 스테이지입니다.")]
        private DistributionInput enhancementDistribution = new(4, 1, 1);

        public override IData GetData()
        {
            return new StageMapData(
                mapKey,
                depthCount,
                widthMax,
                widthMin,
                singleFirstLayer,
                firstStageKey,
                additionalConnectionChance,
                connectionAlgorithm,
                nearestConnectionWeight,
                otherConnectionWeight,
                seed,
                randomizeSeedEachRun,
                nextChapterKey,
                new[]
                {
                    Rule(ChapterStageKind.Combat, combatDistribution),
                    Rule(ChapterStageKind.Shop, shopDistribution),
                    Rule(ChapterStageKind.Recovery, recoveryDistribution),
                    Rule(ChapterStageKind.Enhancement, enhancementDistribution)
                });
        }

        // 값 범위 검사는 규칙 생성자가 맡고, 여기서는 어느 에셋·종류인지만 덧붙입니다.
        private ChapterStageDistributionRule Rule(ChapterStageKind kind, DistributionInput input)
        {
            if (input == null) throw new InvalidOperationException($"MapKey {mapKey}의 {kind} 출현 설정이 비어 있습니다.");
            try
            {
                return new ChapterStageDistributionRule(kind, input.Weight, input.MinimumCount, input.MinDepth);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"MapKey {mapKey}의 {kind} 출현 설정이 잘못되었습니다: 가중치 {input.Weight}(0~{ChapterStageDistributionRule.MaxWeight}), 최소 개수 {input.MinimumCount}(0 이상), 출현 시작 깊이 {input.MinDepth}(1 이상).",
                    exception);
            }
        }

        /// <summary>한 종류의 출현 입력입니다. 종류는 이 값을 가진 칸이 정합니다.</summary>
        [Serializable]
        private sealed class DistributionInput
        {
            [SerializeField, Range(0, ChapterStageDistributionRule.MaxWeight),
             Tooltip("최소 개수를 놓고 남은 자리에서의 상대 가중치입니다. 합계 100일 필요는 없습니다. 0이면 남은 자리 추첨에서 빠지지만 최소 개수는 그대로 배치됩니다.")]
            private int weight;
            [SerializeField, Min(0),
             Tooltip("챕터 맵 전체에서 이 종류가 차지할 최소 노드 수입니다. 플레이어가 고르는 모든 경로에 나온다는 뜻은 아닙니다.")]
            private int minimumCount;
            [SerializeField, Min(1),
             Tooltip("이 깊이부터 나옵니다. 1은 시작 바로 다음 층입니다. 첫 층 스테이지와 개발 진입 고정 노드에는 적용하지 않습니다.")]
            private int minDepth = 1;

            internal int Weight => weight;
            internal int MinimumCount => minimumCount;
            internal int MinDepth => minDepth;

            public DistributionInput() { }

            public DistributionInput(int weight, int minimumCount, int minDepth)
            {
                this.weight = weight;
                this.minimumCount = minimumCount;
                this.minDepth = minDepth;
            }
        }
    }
}
