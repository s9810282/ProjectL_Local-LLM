using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 한 스테이지 종류의 출현 규칙이다. 최소 개수만큼 먼저 배치하고, 남은 자리는 종류별 가중치로 추첨한다.
    /// 가중치는 남은 자리 안의 상대 비율이며 합계 100을 요구하지 않는다. 최소 개수는 챕터 맵 전체 기준이다.
    /// 이 종류는 출현 시작 깊이 이상의 중간 노드에만 놓인다(개발 진입 고정·첫 층 고정 노드는 예외).
    /// </summary>
    public sealed class ChapterStageDistributionRule
    {
        /// <summary>가중치 상한.</summary>
        public const int MaxWeight = 10000;

        public ChapterStageKind Kind { get; }
        /// <summary>남은 자리 추첨의 상대 가중치(0~MaxWeight). 0이면 남은 자리 추첨에서 빠지지만 최소 개수는 그대로 배치된다.</summary>
        public int Weight { get; }
        /// <summary>챕터 맵 전체에서 이 종류가 차지할 최소 노드 수(0 이상). 모든 경로의 방문을 보장하지 않는다.</summary>
        public int MinimumCount { get; }
        /// <summary>이 종류가 놓일 수 있는 가장 얕은 깊이(1 이상). 1은 시작 바로 다음 층이다.</summary>
        public int MinDepth { get; }
        /// <summary>가중치나 최소 개수가 있어 이 종류가 생성될 수 있는지. 활성 종류는 후보 스테이지가 하나 이상 필요하다.</summary>
        public bool IsActive => Weight > 0 || MinimumCount > 0;

        public ChapterStageDistributionRule(ChapterStageKind kind, int weight, int minimumCount, int minDepth)
        {
            if (!System.Enum.IsDefined(typeof(ChapterStageKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (weight < 0 || weight > MaxWeight) throw new ArgumentOutOfRangeException(nameof(weight));
            if (minimumCount < 0) throw new ArgumentOutOfRangeException(nameof(minimumCount));
            if (minDepth < 1) throw new ArgumentOutOfRangeException(nameof(minDepth));

            Kind = kind;
            Weight = weight;
            MinimumCount = minimumCount;
            MinDepth = minDepth;
        }

        /// <summary>
        /// 어떤 층 폭이 나와도 최소 배치와 남은 자리 추첨이 가능한지 검사한다. 문제가 없으면 null, 있으면 이유를 돌려준다.
        /// 중간 층 수를 L이라 할 때 다음을 본다.
        /// 1) 활성 종류의 시작 깊이는 L 이하다.
        /// 2) 남은 자리가 있다면, 깊이 1에 놓일 수 있는 가중치 양수 종류가 하나 이상 있다.
        /// 3) 깊이 d = 1..L마다, 깊이 d 이상에 놓여야 하는 노드 수가
        ///    층마다 최소 폭만 나온 경우의 자리 수 widthMin × (L − d + 1) 이하다.
        ///    singleFirstLayer면 깊이 1은 노드 하나라 d = 1의 자리에서 widthMin − 1을 뺀다.
        /// 놓여야 하는 노드 수 = 시작 깊이가 d 이상인 종류마다 (최소 개수 − 그 종류 고정 노드 수, 0 미만은 0)의 합
        ///  + 깊이 d 이상에 있을 수 있는 고정 노드 수다. 고정 노드는 시작 깊이와 무관하게 자기 종류 최소에 든다.
        /// 개발 진입 고정(pinnedKind)은 어느 깊이에 생길지 모르므로 모든 d에서 센다.
        /// 첫 층 고정(firstLayerKind)은 깊이 1 노드라 d = 1에서만 센다.
        /// 시작 깊이가 "d 이상"으로 포개지므로 3)을 통과하면 생성기가 시작 깊이가 깊은 종류부터 자리를 고를 때 늘 성공한다.
        /// widthMin이 1이고 모든 시작 깊이가 1이면 "1(고정) + 최소 개수 합 − 고정 종류 1 ≤ L"과 같다.
        /// 종류 중복은 검사하지 않는다(규칙 목록을 받는 쪽이 검사한다).
        /// </summary>
        /// <param name="pinnedKind">개발 진입이 고정한 노드의 종류. 없으면 null.</param>
        /// <param name="firstLayerKind">맵이 깊이 1 노드에 고정한 스테이지의 종류. 없으면 null. singleFirstLayer가 켜져 있어야 한다.</param>
        public static string FindPlacementError(IReadOnlyList<ChapterStageDistributionRule> rules, int middleDepths, int widthMin,
            bool singleFirstLayer, ChapterStageKind? pinnedKind = null, ChapterStageKind? firstLayerKind = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (middleDepths < 0) throw new ArgumentOutOfRangeException(nameof(middleDepths));
            if (widthMin < 1) throw new ArgumentOutOfRangeException(nameof(widthMin));
            if (pinnedKind.HasValue && !System.Enum.IsDefined(typeof(ChapterStageKind), pinnedKind.Value))
                throw new ArgumentOutOfRangeException(nameof(pinnedKind));
            if (firstLayerKind.HasValue && !System.Enum.IsDefined(typeof(ChapterStageKind), firstLayerKind.Value))
                throw new ArgumentOutOfRangeException(nameof(firstLayerKind));

            bool fillsDepthOne = false;
            bool hasMinimum = false;
            for (int i = 0; i < rules.Count; i++)
            {
                ChapterStageDistributionRule rule = rules[i]
                    ?? throw new ArgumentException("출현 규칙 목록에 빈 항목이 있습니다.", nameof(rules));
                if (rule.Weight > 0 && rule.MinDepth == 1) fillsDepthOne = true;
                if (rule.MinimumCount > 0) hasMinimum = true;
            }
            if (firstLayerKind.HasValue && !singleFirstLayer)
                return "첫 층 스테이지를 고정하려면 첫 층 노드 1개가 켜져 있어야 합니다.";
            // 중간 층이 없으면 배치할 노드가 없으므로 깊이 규칙은 볼 것이 없다.
            if (middleDepths == 0)
                return pinnedKind.HasValue || firstLayerKind.HasValue || hasMinimum
                    ? "중간 층이 없어 최소 배치나 고정 노드를 놓을 자리가 없습니다." : null;
            foreach (ChapterStageDistributionRule rule in rules)
                if (rule.IsActive && rule.MinDepth > middleDepths)
                    return $"{rule.Kind}의 출현 시작 깊이 {rule.MinDepth}가 중간 층 수 {middleDepths}보다 깊습니다.";
            if (!fillsDepthOne)
                return "깊이 1에 놓일 수 있는(출현 시작 깊이 1) 가중치 양수 종류가 없어 남은 자리를 채울 수 없습니다.";

            for (int depth = 1; depth <= middleDepths; depth++)
            {
                long need = 0;
                foreach (ChapterStageDistributionRule rule in rules)
                {
                    if (rule.MinDepth < depth) continue;
                    int covered = (pinnedKind == rule.Kind ? 1 : 0) + (firstLayerKind == rule.Kind ? 1 : 0);
                    need += Math.Max(0, rule.MinimumCount - covered);
                }
                if (pinnedKind.HasValue) need++;
                if (firstLayerKind.HasValue && depth == 1) need++;
                long slots = (long)widthMin * (middleDepths - depth + 1);
                if (singleFirstLayer && depth == 1) slots -= widthMin - 1;
                if (need > slots)
                    return depth == 1
                        ? $"최소 배치에 일반 노드 {need}개가 필요하지만 중간 층 {middleDepths}개(최소 폭 {widthMin}{(singleFirstLayer ? ", 첫 층 1개" : "")})는 {slots}자리입니다."
                        : $"깊이 {depth} 이상에만 놓일 최소 배치가 {need}개인데, 그 깊이들은 최소 폭 {widthMin} 기준 {slots}자리입니다.";
            }
            return null;
        }
    }
}
