using System;
using System.Collections.Generic;
using System.Linq;
using ChapterGraphConnection = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterGraphConnection;
using ChapterGraphSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterGraphSpec;
using ChapterStageDistributionRule = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterStageDistributionRule;
using ChapterStageKind = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterStageKind;
using ChapterStagePool = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterStagePool;

namespace ProjectL.Tool.Editor
{
    // 표에 든 Root 하나의 맵 판정 재료입니다(병합 대응 설계 MG5). Kind가 null이면 일반 스테이지인데 종류를 고를 수 없습니다(MG-P5).
    internal sealed class CombatStageSoChapterStage
    {
        public readonly string RootGuid, Name, DetailType;
        public readonly int StageKey, MapKey;
        public readonly bool IsBoss;
        public readonly ChapterStageKind? Kind;

        public CombatStageSoChapterStage(string rootGuid, string name, int stageKey, int mapKey, bool isBoss, ChapterStageKind? kind, string detailType)
        {
            RootGuid = rootGuid; Name = name; StageKey = stageKey; MapKey = mapKey; IsBoss = isBoss; Kind = kind; DetailType = detailType;
        }

        // 저장 전 작업본 값으로 바꾼 사본입니다. 상세 SO는 그대로입니다.
        public CombatStageSoChapterStage With(int stageKey, int mapKey, bool isBoss) =>
            new CombatStageSoChapterStage(RootGuid, Name, stageKey, mapKey, isBoss, Kind, DetailType);

        public string Label => $"{Name}({StageKey})";
    }

    // 맵 SO 출현 칸 하나입니다.
    internal sealed class CombatStageSoDistribution
    {
        public readonly ChapterStageKind Kind;
        public readonly int Weight, Minimum, MinDepth;

        public CombatStageSoDistribution(ChapterStageKind kind, int weight, int minimum, int minDepth)
        {
            Kind = kind; Weight = weight; Minimum = minimum; MinDepth = minDepth;
        }
    }

    // 맵 SO 하나의 생성 규칙 값입니다(MG5). 읽지 못했으면 Error만 있고, 맵 키도 읽지 못했으면 MapKey가 0입니다.
    internal sealed class CombatStageSoMapRules
    {
        public readonly int MapKey, DepthCount, WidthMax, WidthMin, FirstStageKey, Seed;
        public readonly bool SingleFirstLayer;
        public readonly string Name, Connection, Error;
        public readonly float NearestWeight, OtherWeight, AdditionalChance;
        public readonly IReadOnlyList<CombatStageSoDistribution> Distributions;

        public CombatStageSoMapRules(int mapKey, string name, int depthCount, int widthMax, int widthMin, bool singleFirstLayer, int firstStageKey, int seed,
            string connection, float nearestWeight, float otherWeight, float additionalChance, IReadOnlyList<CombatStageSoDistribution> distributions)
        {
            MapKey = mapKey; Name = name; DepthCount = depthCount; WidthMax = widthMax; WidthMin = widthMin; SingleFirstLayer = singleFirstLayer;
            FirstStageKey = firstStageKey; Seed = seed; Connection = connection;
            NearestWeight = nearestWeight; OtherWeight = otherWeight; AdditionalChance = additionalChance; Distributions = distributions;
        }

        private CombatStageSoMapRules(int mapKey, string name, string error)
        {
            MapKey = mapKey; Name = name; Error = error; Distributions = Array.Empty<CombatStageSoDistribution>();
        }

        public static CombatStageSoMapRules Unreadable(int mapKey, string name, string error) => new CombatStageSoMapRules(mapKey, name, error);
    }

    // 맵 하나를 판정한 결과입니다. Problem이 null이면 그 맵의 Run 생성이 통과합니다. 맵 SO가 없으면 판정하지 않습니다(MG-P6).
    internal sealed class CombatStageSoChapterResult
    {
        public readonly int MapKey;
        public readonly CombatStageSoMapRules Map;
        public readonly IReadOnlyList<CombatStageSoChapterStage> Bosses;
        public readonly IReadOnlyDictionary<ChapterStageKind, int> Candidates;
        public readonly string Problem;

        public CombatStageSoChapterResult(int mapKey, CombatStageSoMapRules map, IReadOnlyList<CombatStageSoChapterStage> bosses,
            IReadOnlyDictionary<ChapterStageKind, int> candidates, string problem)
        {
            MapKey = mapKey; Map = map; Bosses = bosses; Candidates = candidates; Problem = problem;
        }

        public bool HasMap => Map != null;
        public int CandidateCount(ChapterStageKind kind) => Candidates.TryGetValue(kind, out int count) ? count : 0;
    }

    internal enum CombatStageSoChapterVerdict
    {
        Pass,
        NoMap,
        AlreadyBroken,
        Breaks
    }

    // 한 맵의 바뀌기 전·뒤입니다. 이번 변경이 깨뜨리면 Breaks, 전부터 깨져 있으면 AlreadyBroken입니다(Q-MG3).
    internal sealed class CombatStageSoChapterMapJudgement
    {
        public readonly CombatStageSoChapterResult Before, After;

        public CombatStageSoChapterMapJudgement(CombatStageSoChapterResult before, CombatStageSoChapterResult after)
        {
            Before = before; After = after;
        }

        public int MapKey => After.MapKey;

        public CombatStageSoChapterVerdict Verdict =>
            !After.HasMap ? CombatStageSoChapterVerdict.NoMap
            : After.Problem == null ? CombatStageSoChapterVerdict.Pass
            : Before.Problem == null ? CombatStageSoChapterVerdict.Breaks
            : CombatStageSoChapterVerdict.AlreadyBroken;
    }

    // 변경 하나가 닿는 맵들의 판정입니다(MG7). SwapBoss는 이 변경으로 보스가 둘이 될 때 함께 빼면 되는 기존 보스입니다(Q-MG3 A).
    internal sealed class CombatStageSoChapterJudgement
    {
        public readonly IReadOnlyList<CombatStageSoChapterMapJudgement> Maps;
        public readonly CombatStageSoChapterStage SwapBoss;

        public CombatStageSoChapterJudgement(IReadOnlyList<CombatStageSoChapterMapJudgement> maps, CombatStageSoChapterStage swapBoss)
        {
            Maps = maps; SwapBoss = swapBoss;
        }

        public bool Breaks => Maps.Any(map => map.Verdict == CombatStageSoChapterVerdict.Breaks);
        public bool AlreadyBroken => Maps.Any(map => map.Verdict == CombatStageSoChapterVerdict.AlreadyBroken);

        // 확인 창·알림에 쓰는 줄들입니다. 맵마다 바뀌는 수, 깨지는 까닭, 맵 SO가 없다는 안내를 냅니다.
        public string Describe()
        {
            var lines = new List<string>();
            foreach (var map in Maps)
            {
                lines.AddRange(CombatStageSoChapter.Changes(map));
                switch (map.Verdict)
                {
                    case CombatStageSoChapterVerdict.NoMap:
                        lines.Add($"맵 키 {map.MapKey}의 맵 SO가 없어 이 맵 키로는 Run이 만들어지지 않습니다.");
                        break;
                    case CombatStageSoChapterVerdict.Breaks:
                        lines.Add($"맵 {map.MapKey}의 Run 생성이 멈춥니다: {map.After.Problem}");
                        break;
                    case CombatStageSoChapterVerdict.AlreadyBroken:
                        lines.Add($"맵 {map.MapKey}은 지금도 Run 생성이 멈추고, 바뀐 뒤에도 멈춥니다: {map.After.Problem}");
                        break;
                }
            }
            return string.Join("\n", lines);
        }
    }

    // 표 등록 상태가 맵 생성 규칙을 지키는지 봅니다(병합 대응 설계 MG6·MG7). 에셋을 모르고 값만 받습니다.
    // 후보 묶음·맵 모양 규칙은 Core 공개 계약(ChapterStageDistributionRule·ChapterStagePool·ChapterGraphSpec)을 실제로 만들어 Core가 판정합니다.
    // 도구 사본은 셋입니다: 보스 수(StageGeneratorSystem.PrepareGraph), 종류 고르기(KindOf, CombatStageSoAssets), 연결 방식(ToConnection, 아래).
    internal static class CombatStageSoChapter
    {
        internal static CombatStageSoMapRules MapFor(IReadOnlyList<CombatStageSoMapRules> maps, int mapKey) =>
            // 맵 키를 읽지 못한 맵 SO가 있으면 그것이 이 맵일 수 있어 "없음" 대신 그 오류를 보입니다.
            maps.FirstOrDefault(map => map.MapKey == mapKey) ?? maps.FirstOrDefault(map => map.MapKey == 0 && map.Error != null);

        internal static CombatStageSoChapterResult Check(int mapKey, CombatStageSoMapRules map, IReadOnlyList<CombatStageSoChapterStage> allStages)
        {
            var stages = allStages.Where(stage => stage.MapKey == mapKey).ToList();
            var bosses = stages.Where(stage => stage.IsBoss).ToList();
            var candidates = stages.Where(stage => !stage.IsBoss && stage.Kind.HasValue)
                .GroupBy(stage => stage.Kind.Value).ToDictionary(group => group.Key, group => group.Count());
            string problem = map == null ? null
                : map.Error != null ? $"맵 SO({map.Name})를 읽지 못했습니다: {map.Error}"
                : ProblemOf(mapKey, map, stages, bosses);
            return new CombatStageSoChapterResult(mapKey, map, bosses, candidates, problem);
        }

        private static string ProblemOf(int mapKey, CombatStageSoMapRules map, List<CombatStageSoChapterStage> stages,
            List<CombatStageSoChapterStage> bosses)
        {
            // StageTable.GetStages: 그 맵 키의 Root가 표에 하나도 없으면 예외입니다.
            if (stages.Count == 0) return $"맵 {mapKey}에 표에 든 스테이지가 없습니다.";
            var unknown = stages.FirstOrDefault(stage => !stage.IsBoss && !stage.Kind.HasValue);
            if (unknown != null) return $"{unknown.Label}: 일반 스테이지인데 상세 SO({unknown.DetailType})로 종류를 정할 수 없습니다.";
            // 사본(StageGeneratorSystem.PrepareGraph): 역할이 Regular가 아니면 보스이고, 보스는 맵마다 정확히 하나입니다.
            if (bosses.Count == 0) return "보스 스테이지가 없습니다(맵마다 하나여야 합니다).";
            if (bosses.Count > 1) return $"보스 스테이지가 {bosses.Count}개입니다(맵마다 하나): {string.Join(", ", bosses.Select(boss => boss.Label))}";
            // 사본(StageGeneratorSystem.ToConnection): 맵 SO 연결 방식과 Core 연결 방식은 이름이 같습니다(계획 P-MG3).
            if (!Enum.TryParse(map.Connection, out ChapterGraphConnection connection) || !Enum.IsDefined(typeof(ChapterGraphConnection), connection))
                return $"맵 SO 연결 방식 '{map.Connection}'을 Core 연결 방식으로 바꿀 수 없습니다.";
            try
            {
                var pools = map.Distributions.Select(distribution => new ChapterStagePool(
                    new ChapterStageDistributionRule(distribution.Kind, distribution.Weight, distribution.Minimum, distribution.MinDepth),
                    stages.Where(stage => !stage.IsBoss && stage.Kind == distribution.Kind).Select(stage => stage.StageKey).ToList())).ToList();
                _ = new ChapterGraphSpec(map.DepthCount, map.WidthMax, map.WidthMin, map.SingleFirstLayer, map.Seed, connection, map.NearestWeight, map.OtherWeight,
                    map.AdditionalChance, pools, bosses[0].StageKey, firstStageKey: map.FirstStageKey);
                return null;
            }
            catch (ArgumentException exception) { return exception.Message; }
        }

        // 변경 하나를 지금 표 상태에 덮어 판정합니다(MG7). next가 null이면 rootGuid를 빼고, 아니면 next로 넣거나 바꿉니다.
        // dropGuid는 보스 교체 때 함께 빼는 다른 Root입니다.
        internal static CombatStageSoChapterJudgement Judge(IReadOnlyList<CombatStageSoChapterStage> stages, IReadOnlyList<CombatStageSoMapRules> maps,
            string rootGuid, CombatStageSoChapterStage next, string dropGuid = null)
        {
            bool Changed(CombatStageSoChapterStage stage) => stage.RootGuid == rootGuid || (dropGuid != null && stage.RootGuid == dropGuid);
            var after = stages.Where(stage => !Changed(stage)).ToList();
            if (next != null) after.Add(next);
            var keys = new SortedSet<int>(stages.Where(Changed).Select(stage => stage.MapKey));
            if (next != null) keys.Add(next.MapKey);
            var judged = keys.Select(key => new CombatStageSoChapterMapJudgement(
                Check(key, MapFor(maps, key), stages), Check(key, MapFor(maps, key), after))).ToList();
            CombatStageSoChapterStage swap = null;
            if (dropGuid == null && next != null && next.IsBoss)
            {
                var others = stages.Where(stage => stage.MapKey == next.MapKey && stage.IsBoss && stage.RootGuid != rootGuid).ToList();
                if (others.Count == 1) swap = others[0];
            }
            return new CombatStageSoChapterJudgement(judged, swap);
        }

        // '스테이지 값' 맵 생성 줄입니다(MG9). 예: "보스 Stage_Normal_A(1101) · 폭 2~3(첫 층 1) · 전투 2(80·1) · 상점 1(9·1, 깊이 2~)".
        // 괄호는 가중치·최소 개수이고, 출현 시작 깊이는 1이 아닐 때만 덧붙입니다.
        internal static string Summary(CombatStageSoChapterResult result)
        {
            string boss = result.Bosses.Count == 1 ? $"보스 {result.Bosses[0].Label}"
                : result.Bosses.Count == 0 ? "보스 없음" : $"보스 {result.Bosses.Count}개";
            var parts = new List<string> { boss };
            if (result.Map != null && result.Map.Error == null)
            {
                parts.Add($"폭 {result.Map.WidthMin}~{result.Map.WidthMax}{(result.Map.SingleFirstLayer ? "(첫 층 1)" : "")}");
                if (result.Map.FirstStageKey > 0) parts.Add($"첫 층 고정 {result.Map.FirstStageKey}");
                parts.AddRange(result.Map.Distributions.Select(distribution =>
                    $"{KindName(distribution.Kind)} {result.CandidateCount(distribution.Kind)}({distribution.Weight}·{distribution.Minimum}"
                    + (distribution.MinDepth > 1 ? $", 깊이 {distribution.MinDepth}~)" : ")")));
            }
            else
                parts.AddRange(result.Candidates.OrderBy(pair => pair.Key).Select(pair => $"{KindName(pair.Key)} {pair.Value}"));
            return string.Join(" · ", parts);
        }

        // 한 맵에서 바뀌는 보스·종류별 후보 수입니다. 예: "맵 1 전투 후보 2 → 3".
        internal static IEnumerable<string> Changes(CombatStageSoChapterMapJudgement map)
        {
            string before = BossLabel(map.Before), after = BossLabel(map.After);
            if (before != after) yield return $"맵 {map.MapKey} 보스 {before} → {after}";
            foreach (ChapterStageKind kind in Enum.GetValues(typeof(ChapterStageKind)))
            {
                int from = map.Before.CandidateCount(kind), to = map.After.CandidateCount(kind);
                if (from != to) yield return $"맵 {map.MapKey} {KindName(kind)} 후보 {from} → {to}";
            }
        }

        // 이 Root가 그 맵에서 맡은 자리입니다.
        internal static string RoleOf(IReadOnlyList<CombatStageSoChapterStage> stages, string rootGuid)
        {
            var stage = stages.FirstOrDefault(found => found.RootGuid == rootGuid);
            if (stage == null) return "미등록";
            if (stage.IsBoss) return $"맵 {stage.MapKey} 보스";
            return stage.Kind.HasValue ? $"맵 {stage.MapKey} {KindName(stage.Kind.Value)} 후보" : $"맵 {stage.MapKey} 종류 모름";
        }

        private static string BossLabel(CombatStageSoChapterResult result) =>
            result.Bosses.Count == 1 ? result.Bosses[0].Label : result.Bosses.Count == 0 ? "없음" : $"{result.Bosses.Count}개";

        private static string KindName(ChapterStageKind kind) => kind switch
        {
            ChapterStageKind.Combat => "전투",
            ChapterStageKind.Shop => "상점",
            ChapterStageKind.Recovery => "회복",
            ChapterStageKind.Enhancement => "강화",
            _ => kind.ToString()
        };
    }
}
