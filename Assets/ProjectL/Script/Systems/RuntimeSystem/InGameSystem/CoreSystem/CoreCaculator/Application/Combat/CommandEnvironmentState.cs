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

        /// <summary>플랫폼의 정의·식별자를 유지하며 현재 위치와 진행 부호만 바꾼다.</summary>
        internal void ChangePlatform(MovingPlatformFact expected, MovingPlatformFact final)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (final == null) throw new ArgumentNullException(nameof(final));
            if (!_sources.TryGetValue(expected.Id, out var current) || !current.SameValues(expected) ||
                !Bindings.TerrainSourceIds.Contains(expected.Id) || !expected.Spec.SameValues(final.Spec) ||
                !expected.KnownTo.SequenceEqual(final.KnownTo))
                throw new InvalidOperationException("플랫폼 변경은 현재 원본과 고정 정의를 보존해야 합니다.");
            _sources[expected.Id] = final;
        }

        internal void AddTraitTrail(TraitTrailFact trail)
        {
            if (trail == null) throw new ArgumentNullException(nameof(trail));
            if (trail.Id != Progress.NextSourceId || _sources.ContainsKey(trail.Id))
                throw new InvalidOperationException("흔적은 기존 환경의 다음 원본 ID를 사용해야 합니다.");
            _sources.Add(trail.Id, trail);
            Progress = new EnvironmentProgressFacts(Progress.NextObjectEntryOrdinal, checked(Progress.NextSourceId + 1), Progress.RandomState);
        }

        /// <summary>현재 원본과 일치하는 약한 지반의 한 번 진입 또는 붕괴만 반영한다.</summary>
        internal void ChangeTerrain(StaticTerrainFact expected, StaticTerrainFact final)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (final == null) throw new ArgumentNullException(nameof(final));
            if (!_sources.TryGetValue(expected.Id, out var current) || !current.SameValues(expected) ||
                !Bindings.TerrainSourceIds.Contains(expected.Id) || !expected.Active || expected.Kind != StaticTerrainKind.WeakGround)
                throw new InvalidOperationException("지형 변경 기대값은 현재 분류된 활성 약한 지반이어야 합니다.");

            bool entered = final.Kind == StaticTerrainKind.WeakGround && expected.EntryCount < long.MaxValue &&
                expected.RegisterEntry().SameValues(final);
            bool collapsed = final.Kind == StaticTerrainKind.Cliff && expected.PendingCollapse &&
                expected.Collapse().SameValues(final);
            if (!entered && !collapsed)
                throw new InvalidOperationException("지형 변경은 기존 구성과 식별자를 유지하는 한 번 진입 또는 예정된 붕괴여야 합니다.");
            _sources[expected.Id] = final;
        }

        /// <summary>기둥 영향의 기대값과 연속 할당을 모두 확인한 뒤 환경·분류·진행값을 함께 바꾼다.</summary>
        internal void ApplyPillarTerrainChanges(IReadOnlyList<CombatPillarTerrainEvent> changes)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            long nextId = Progress.NextSourceId;
            var ids = new HashSet<long>();
            var added = new List<long>();
            foreach (var change in changes)
            {
                if (change == null || !ids.Add(change.Final.Id))
                    throw new ArgumentException("기둥 지형 변경에는 중복이나 null을 넣을 수 없습니다.", nameof(changes));
                if (change.Expected == null)
                {
                    if (_sources.ContainsKey(change.Final.Id) || change.Final.Id != nextId)
                        throw new InvalidOperationException("잔해 벽은 기존 환경의 다음 원본 ID를 사용해야 합니다.");
                    added.Add(nextId);
                    nextId = checked(nextId + 1);
                }
                else if (!_sources.TryGetValue(change.Expected.Id, out var current) || !current.SameValues(change.Expected) ||
                    !Bindings.TerrainSourceIds.Contains(change.Expected.Id))
                    throw new InvalidOperationException("기둥 지형 변경의 기대 원본이 현재 지형과 다릅니다.");
            }
            foreach (var change in changes) _sources[change.Final.Id] = change.Final;
            if (added.Count != 0)
                Bindings = new EnvironmentSemanticBindings(Bindings.TerrainSourceIds.Concat(added).ToArray(),
                    Bindings.ObjectSourceIds, Bindings.ObstacleEntityIds);
            Progress = new EnvironmentProgressFacts(Progress.NextObjectEntryOrdinal, nextId, Progress.RandomState);
        }

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

        internal void SetRandom(uint expected, uint final)
        {
            if (Progress.RandomState != expected) throw new InvalidOperationException("환경 난수 기대값이 오래되었습니다.");
            Progress = new EnvironmentProgressFacts(Progress.NextObjectEntryOrdinal, Progress.NextSourceId, final);
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
            if (expected is ObjectSourceFact objectSource && !objectSource.ChangesOnlyActive(final))
                throw new InvalidOperationException("Object 소모는 진입 순서나 원본 내용까지 바꿀 수 없습니다.");
            if (expected.SameValues(final)) return false;
            _sources[expected.Id] = final;
            return true;
        }

        /// <summary>사망 전 위치로 깃발 드롭을 제안하고 원본·분류·할당 진행값을 함께 갱신한다.</summary>
        internal void ResolveFlagDrops(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> characters,
            IReadOnlyList<StructureCombatFacts> structures, IReadOnlyDictionary<EntityId, CellCoord> deathAnchors,
            List<CombatEvent> events, IReadOnlyCollection<EntityId> fallenEntityIds = null)
        {
            var environment = Facts;
            var bindings = Bindings;
            var progress = Progress;
            FlagDropCalculator.ResolveDeaths(input, characters, structures, deathAnchors,
                ref environment, ref bindings, ref progress, events, fallenEntityIds);
            Bindings = bindings;
            Progress = progress;
            foreach (var source in environment.Tiles) _sources[source.Id] = source;
        }
    }
}
