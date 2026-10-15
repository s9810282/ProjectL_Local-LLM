using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 안의 환경 원본·분류·할당 진행값이다. 원본 변경의 기대값 검증과 난수 진행만 담당하며
    /// 사건 기록·귀환 장벽 판정은 호출하는 투영이 소유한다.</summary>
    internal sealed class CommandEnvironmentState
    {
        private readonly Dictionary<long, MovementTileFact> _sources;
        internal EnvironmentSemanticBindings Bindings { get; private set; }
        internal EnvironmentProgressFacts Progress { get; private set; }
        internal MovementEnvironmentFacts Facts => new(_sources.Values.ToArray());

        internal CommandEnvironmentState(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            EnvironmentProgressFacts progress)
        {
            _sources = (environment ?? throw new ArgumentNullException(nameof(environment))).Tiles.ToDictionary(t => t.Id);
            Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        internal MovementTileFact Source(long id) => _sources[id];

        /// <summary>일반 진입 규칙에 넘길 셀의 원본을 기존 순서(Priority, ID)로 고른다.</summary>
        internal MovementTileFact[] ArrivalRuleSourcesAt(CellCoord cell) =>
            _sources.Values.Where(s => s.Cell == cell && s.TriggersArrivalRule).OrderBy(s => s.Priority).ThenBy(s => s.Id).ToArray();

        internal int ChooseRandom(int count)
        {
            uint random = Progress.RandomState;
            int index = EnvironmentRandom.Choose(ref random, count);
            if (random != Progress.RandomState)
                Progress = new EnvironmentProgressFacts(Progress.NextObjectEntryOrdinal, Progress.NextSourceId, random);
            return index;
        }

        internal void ChangeTornado(TornadoSourceFact expected, TornadoSourceFact final)
        {
            if (!Source(expected.Id).SameValues(expected) || !expected.AfterActivation(final.Cell).SameValues(final))
                throw new InvalidOperationException("Tornado의 위치 변경 또는 주기 기대값이 잘못되었습니다.");
            _sources[expected.Id] = final;
        }

        /// <summary>선택된 원본의 규칙 결과만 반영한다. 실제로 바뀌었으면 true다.</summary>
        internal bool ApplySelectedChange(MovementTileFact selected, MovementStepEffects effects)
        {
            if (effects.ExpectedSource == null) return false;
            var expected = effects.ExpectedSource;
            var final = effects.FinalSource;
            if (expected.Id != selected.Id || !Source(selected.Id).SameValues(expected) ||
                !expected.KnownTo.SequenceEqual(final.KnownTo) || expected.Priority != final.Priority || expected.IsMine != final.IsMine)
                throw new InvalidOperationException("진입 규칙의 원본 기대값 또는 변경 범위가 잘못되었습니다.");
            if (expected is ObjectSourceFact objectSource && !objectSource.WithActive(final.Active).SameValues(final))
                throw new InvalidOperationException("Object 소모는 진입 순서나 원본 내용까지 바꿀 수 없습니다.");
            if (expected.SameValues(final)) return false;
            _sources[expected.Id] = final;
            return true;
        }

        /// <summary>사망 전 위치로 깃발 드롭을 제안하고 원본·분류·할당 진행값을 함께 갱신한다.</summary>
        internal void ResolveFlagDrops(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            IReadOnlyList<StructureCombatFacts> structures, IReadOnlyDictionary<EntityId, CellCoord> deathAnchors,
            List<CombatEvent> events)
        {
            var environment = Facts;
            var bindings = Bindings;
            var progress = Progress;
            FlagDropCalculator.ResolveDeaths(input, characters, structures, deathAnchors,
                ref environment, ref bindings, ref progress, events);
            Bindings = bindings;
            Progress = progress;
            foreach (var source in environment.Tiles) _sources[source.Id] = source;
        }
    }
}
