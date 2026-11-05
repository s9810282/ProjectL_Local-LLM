using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>옛 입력의 원본 ID를 예약하고 단일 그룹 명세로 변환한다. 실제 중심 선정은 공통 기믹 준비가 맡는다.</summary>
    internal static class InitialTornadoPlacementCalculator
    {
        internal static (MovementEnvironmentFacts Environment, EnvironmentSemanticBindings Bindings, EnvironmentProgressFacts Progress,
            IReadOnlyList<StageGimmickSpec> Definitions) Place(BattleInitialization initialization,
            IReadOnlyList<StructureCombatFacts> structures, EnvironmentSemanticBindings bindings, int battleSeed)
        {
            if (initialization == null) throw new ArgumentNullException(nameof(initialization));
            var definitions = initialization.Stage.StageGimmicks.ToArray().ToList();
            var legacy = initialization.Environment.Tiles.OfType<TornadoSourceFact>().ToArray();
            var random = initialization.InitialRandomTornadoPlacements;
            var authored = definitions.OfType<TornadoGimmickSpec>().SingleOrDefault();
            if (authored == null && (legacy.Length > 0 || random.Count > 0))
            {
                authored = new TornadoGimmickSpec(legacy.Select(t => new TornadoMemberSpec(t.Id, t.KnownTo, t.Cell,
                    firstActivationAfterControllerTurns: t.NextDueCompletedTurn, repeatIntervalControllerTurns: t.RepeatIntervalUnitTurns))
                    .Concat(random.Select(t => new TornadoMemberSpec(t.SourceId, t.KnownTo,
                        firstActivationAfterControllerTurns: t.FirstActivationAfterUnitTurns,
                        repeatIntervalControllerTurns: t.RepeatIntervalUnitTurns))).ToArray());
                definitions.Add(authored);
            }
            else if (authored != null && (legacy.Length > 0 || random.Count > 0))
                throw new InvalidOperationException("토네이도 그룹과 옛 단일 칸 원본을 함께 설치할 수 없습니다.");
            if (authored == null) return (initialization.Environment, bindings, null, definitions);
            var ids = authored.Members.Select(m => m.SourceId).ToHashSet();
            var environment = new MovementEnvironmentFacts(initialization.Environment.Tiles.Where(t => t is not TornadoSourceFact).ToArray());
            if (environment.Tiles.Any(t => ids.Contains(t.Id)) || bindings.ObjectSourceIds.Any(ids.Contains))
                throw new InvalidOperationException("토네이도 원본 ID가 다른 환경 원본과 겹칩니다.");
            var resultBindings = new EnvironmentSemanticBindings(bindings.TerrainSourceIds.Where(id => !ids.Contains(id)).ToArray(),
                bindings.ObjectSourceIds, bindings.ObstacleEntityIds);
            uint seed = authored.Members.Any(m => !m.InitialCenter.HasValue) ? EnvironmentRandom.InitialState(battleSeed) : EnvironmentProgressFacts.InitialRandomState;
            var progress = EnvironmentProgressFacts.Initial(environment, seed);
            progress = new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                Math.Max(progress.NextSourceId, checked(ids.Max() + 1)), progress.RandomState);
            return (environment, resultBindings, progress, definitions);
        }
    }
}
