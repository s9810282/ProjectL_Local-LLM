using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal readonly struct HazardCellFact
    {
        public StageGimmickId Id { get; }
        public StageGimmickKey Key { get; }
        public long BoundaryCount { get; }
        public CellCoord? Cell { get; }
        public bool InstantDefeat { get; }
        public RockfallGimmickSpec Rockfall { get; }
        public CombatAssessmentGap Gaps { get; }
        public long? TornadoSourceId { get; }
        public IReadOnlyList<CellCoord> HaboobRegion { get; }
        public HaboobGimmickPhase? HaboobPhase { get; }
        public long? HaboobNextBoundaryCount { get; }
        internal HazardCellFact(StageGimmickId id, StageGimmickKey key, long boundaryCount, CellCoord? cell,
            bool instantDefeat = false, RockfallGimmickSpec rockfall = null, CombatAssessmentGap gaps = CombatAssessmentGap.None,
            long? tornadoSourceId = null, IReadOnlyList<CellCoord> haboobRegion = null, HaboobGimmickPhase? haboobPhase = null,
            long? haboobNextBoundaryCount = null)
        {
            Id = id; Key = key; BoundaryCount = boundaryCount; Cell = cell;
            InstantDefeat = instantDefeat; Rockfall = rockfall; Gaps = gaps;
            TornadoSourceId = tornadoSourceId;
            HaboobRegion = haboobRegion;
            HaboobPhase = haboobPhase;
            HaboobNextBoundaryCount = haboobNextBoundaryCount;
        }
    }

    internal readonly struct HazardExposure
    {
        public int KnownHpLoss { get; }
        public int PossibleHpLoss { get; }
        public int BindEndCount { get; }
        public int PossibleBindEndCount { get; }
        public bool AdditionalBindAtNextOpportunity { get; }
        public CombatAssessmentGap Gaps { get; }
        public IReadOnlyList<HazardCellFact> Evidence { get; }
        internal HazardExposure(int known, int possible, int bind, int possibleBind, CombatAssessmentGap gaps,
            IReadOnlyList<HazardCellFact> evidence, bool additionalBindAtNextOpportunity = false)
        {
            KnownHpLoss = known; PossibleHpLoss = possible; BindEndCount = bind; PossibleBindEndCount = possibleBind;
            Gaps = gaps; Evidence = evidence;
            AdditionalBindAtNextOpportunity = additionalBindAtNextOpportunity;
        }
    }

    internal sealed class StageHazardForecast
    {
        internal const CombatAssessmentGap TornadoThrowGaps = CombatAssessmentGap.UnknownPosition | CombatAssessmentGap.RandomOutcome;
        private readonly CombatCommandInput _input;
        private readonly Dictionary<CellCoord, List<int>> _cells;
        private readonly Dictionary<CellCoord, int> _entries;
        private readonly List<int> _unknown;
        private readonly List<int> _damageGroups;
        private readonly Dictionary<(StageGimmickId Id, long Boundary), int> _groupEnds;
        private readonly IReadOnlyList<CellCoord> _haboobRegion;
        private readonly long? _haboobUnknownAt;
        private readonly CharacterCombatFacts _main;
        private readonly CharacterCombatFacts _objectiveTarget;
        private readonly BattleObjectiveKind _objectiveKind;
        private readonly long _firstBoundary;
        private readonly int _width;
        private readonly int _height;
        internal IReadOnlyList<HazardCellFact> Facts { get; }
        internal long LastBoundaryCount { get; }

        private StageHazardForecast(CombatCommandInput input, long lastBoundary, List<HazardCellFact> facts,
            Dictionary<CellCoord, List<int>> cells, Dictionary<CellCoord, int> entries, List<int> unknown, List<int> damageGroups,
            Dictionary<(StageGimmickId Id, long Boundary), int> groupEnds,
            CharacterCombatFacts main, CharacterCombatFacts objectiveTarget,
            IReadOnlyList<CellCoord> haboobRegion, long? haboobUnknownAt)
        {
            _input = input;
            _firstBoundary = checked(input.State.Turn.CompletedControllerTurnCount + 1);
            _width = input.State.BoardWidth;
            _height = input.State.BoardHeight;
            LastBoundaryCount = lastBoundary;
            Facts = facts.AsReadOnly();
            _cells = cells;
            _entries = entries;
            _unknown = unknown;
            _damageGroups = damageGroups;
            _groupEnds = groupEnds;
            _haboobRegion = haboobRegion;
            _haboobUnknownAt = haboobUnknownAt;
            _main = main;
            _objectiveTarget = objectiveTarget;
            _objectiveKind = input.State.Objective.Kind;
        }

        /// <summary>현재 토네이도 영역에 실제 진입했을 때의 위험을 조회한다.</summary>
        internal CombatAssessmentGap EntryGaps(CharacterCombatFacts actor, CellCoord reachedCell) =>
            TryGetEntry(actor, reachedCell, out _) && !UnitBehaviorRules.IsForcedMovementImmune(_input, actor)
                ? TornadoThrowGaps : CombatAssessmentGap.None;

        internal bool? CanDesignateAt(CellCoord attacker, CellCoord primary, long boundaryCount)
        {
            if (boundaryCount < _firstBoundary - 1 || boundaryCount > LastBoundaryCount)
                throw new ArgumentOutOfRangeException(nameof(boundaryCount));
            if (_haboobUnknownAt.HasValue && boundaryCount >= _haboobUnknownAt.Value) return null;
            return _haboobRegion == null || HaboobTargetingRules.CanDesignate(_haboobRegion, attacker, primary);
        }

        private bool TryGetEntry(CharacterCombatFacts actor, CellCoord reachedCell, out int index)
        {
            index = default;
            if (_entries.Count == 0 || actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field) return false;
            if (!actor.Anchor.HasValue) throw new InvalidOperationException("필드 개체의 위치가 없습니다.");
            var offset = reachedCell - actor.Anchor.Value;
            foreach (var occupied in actor.OccupiedCells)
                if (_entries.TryGetValue(occupied + offset, out index)) return true;
            return false;
        }

        /// <summary>경로 진입과 종료 경계의 점유를 순서대로 평가한다. 경로 없는 후보는 누적 진입 위험을 넘길 수 있다.</summary>
        internal HazardExposure Evaluate(CharacterCombatFacts actor, IReadOnlyList<CellCoord> path, CellCoord endpoint,
            CombatAssessmentGap entryGaps = CombatAssessmentGap.None)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (endpoint.X < 0 || endpoint.Y < 0 || endpoint.X >= _width || endpoint.Y >= _height)
                throw new ArgumentOutOfRangeException(nameof(endpoint));
            int cutoff = int.MaxValue;
            int possibleCutoff = int.MaxValue;
            // 같은 기믹의 모든 직접 피해가 끝난 뒤 승패를 확인한다. 다른 칸의 메인·목표 사망도 같은 경계를 쓴다.
            ConsiderTerminal(_main, true);
            ConsiderTerminal(_objectiveTarget, false);
            int? entry = null;
            for (int i = path.Count > 0 && path[0] == actor.Anchor ? 1 : 0; i < path.Count; i++)
            {
                if (!TryGetEntry(actor, path[i], out int index)) continue;
                if (!UnitBehaviorRules.IsForcedMovementImmune(_input, actor))
                {
                    entryGaps |= TornadoThrowGaps;
                    entry = index;
                }
                break;
            }
            return EvaluateFixed(actor, endpoint, cutoff, possibleCutoff, entryGaps, entry, out _, out _);

            void ConsiderTerminal(CharacterCombatFacts unit, bool isMain)
            {
                if (unit == null || unit.EntityId == actor.EntityId ||
                    !BattleOutcomeCalculator.Resolve(isMain, !isMain, _objectiveKind).IsStageEnded) return;
                int end = -1, possibleEnd = -1;
                if (!unit.Health.IsDead)
                    EvaluateFixed(unit, unit.Anchor ?? CellCoord.Zero, int.MaxValue, int.MaxValue, CombatAssessmentGap.None,
                        null, out end, out possibleEnd);
                cutoff = Math.Min(cutoff, end);
                possibleCutoff = Math.Min(possibleCutoff, possibleEnd);
            }
        }

        private HazardExposure EvaluateFixed(CharacterCombatFacts actor, CellCoord endpoint, int cutoff, int possibleCutoff,
            CombatAssessmentGap entryGaps, int? entry, out int deathEnd, out int possibleDeathEnd)
        {
            deathEnd = int.MaxValue;
            possibleDeathEnd = int.MaxValue;
            var evidence = new List<HazardCellFact>();
            if (actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field)
                return new HazardExposure(0, 0, 0, 0, CombatAssessmentGap.None, evidence.AsReadOnly());
            if (!actor.Anchor.HasValue) throw new InvalidOperationException("필드 개체의 위치가 없습니다.");
            if (entry.HasValue && entry.Value <= cutoff) evidence.Add(Facts[entry.Value]);
            // 위치를 잃은 뒤에는 원래 칸의 색인에서 빠진 철도·낙석도 가능한 접촉이다.
            var selected = new SortedDictionary<int, bool>();
            foreach (int index in _damageGroups) selected.Add(index, false);
            foreach (int index in _unknown) selected[index] = true;
            var offset = endpoint - actor.Anchor.Value;
            foreach (var occupied in actor.OccupiedCells)
                if (_cells.TryGetValue(occupied + offset, out var indices))
                    foreach (int index in indices) selected[index] = true;
            int health = actor.Health.CurrentHealth;
            bool uncertainPosition = (entryGaps & CombatAssessmentGap.UnknownPosition) != 0;
            int known = 0, possible = uncertainPosition ? health : 0, bind = 0, possibleBind = 0;
            var gaps = entryGaps;
            bool? immune = null;
            bool laterOpportunity = cutoff == int.MaxValue && LastBoundaryCount > _firstBoundary;
            if (laterOpportunity && CombatActionAdmission.IsAttackRestricted(actor) && CombatActionAdmission.IsMovementRestricted(actor))
                gaps |= CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.FutureWarning;
            var impacts = new HashSet<(StageGimmickId Id, long Boundary)>();
            foreach (var (index, direct) in selected)
            {
                if (index > cutoff) break;
                if (!direct && !uncertainPosition) continue;
                var fact = Facts[index];
                if (fact.TornadoSourceId.HasValue)
                {
                    immune ??= UnitBehaviorRules.IsForcedMovementImmune(_input, actor);
                    if (immune.Value) continue;
                    uncertainPosition = true;
                    possible = health;
                    possibleDeathEnd = Math.Min(possibleDeathEnd, _groupEnds[(fact.Id, fact.BoundaryCount)]);
                }
                gaps |= fact.Gaps;
                evidence.Add(fact);
                if (!fact.InstantDefeat && fact.Rockfall == null) continue;
                if (!impacts.Add((fact.Id, fact.BoundaryCount))) continue;
                int loss = fact.InstantDefeat ? health - known : RockfallGimmickRule.HealthLoss(actor.Health.MaxHealth, fact.Rockfall);
                var endedSide = EndedSideAt(fact.BoundaryCount);
                int duration = fact.Rockfall == null ? 0 : RockfallGimmickRule.BindEndCount(actor.Owner, endedSide, fact.Rockfall);
                possible = (int)Math.Min(health, (long)possible + loss);
                if (possible == health)
                    possibleDeathEnd = Math.Min(possibleDeathEnd, _groupEnds[(fact.Id, fact.BoundaryCount)]);
                if (index > possibleCutoff) gaps |= CombatAssessmentGap.RandomOutcome;
                if (uncertainPosition || index > possibleCutoff || fact.Cell == null ||
                    (fact.Gaps & CombatAssessmentGap.RandomOutcome) != 0)
                {
                    if (loss < health - known) possibleBind = Math.Max(possibleBind, duration);
                    continue;
                }
                known = checked(known + Math.Min(health - known, loss));
                if (known == health)
                {
                    deathEnd = _groupEnds[(fact.Id, fact.BoundaryCount)];
                    break;
                }
                bind = Math.Max(bind, duration);
            }
            if (laterOpportunity && known < health && Math.Max(bind, possibleBind) > 0 &&
                CombatActionAdmission.IsAttackRestricted(actor))
                gaps |= CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.FutureWarning;
            bool additionalBind = laterOpportunity && known < health && Math.Max(bind, possibleBind) > 0 &&
                !KeepsBindAtNextOpportunity(actor);
            return new HazardExposure(known, possible, known == health ? 0 : bind,
                known == health ? 0 : Math.Max(bind, possibleBind), gaps, evidence.AsReadOnly(), additionalBind);
        }

        private Owner EndedSideAt(long boundaryCount) =>
            (boundaryCount - _firstBoundary) % 2 == 0 ? Owner.Enemy : Owner.Player;

        private bool KeepsBindAtNextOpportunity(CharacterCombatFacts actor)
        {
            // 재구성 중이면 다음 자기 턴 시작에 1이 줄고, 그때 0이 되면 그 턴에 행동한다. 2 이상 남았으면 다음 기회에도 움직이지 못한다.
            var reconstruction = Domain.Traits.Rules.ReconstructionTraitRule.Current(actor);
            if (reconstruction != null && reconstruction.RemainingOwnTurnStarts > 1) return true;
            var baseline = actor.StatusEffects;
            if (!AppliedStatusRules.IsRestricted(baseline, StatusEffectKey.Bind)) return false;
            // 기존 속박만 같은 종료 경계로 소비한다. 예측한 새 속박은 실제 상태에 적용하지 않는다.
            for (long offset = 0; offset <= LastBoundaryCount - _firstBoundary; offset++)
                baseline = AppliedStatusRules.EndControllerTurn(baseline, actor.Owner, EndedSideAt(_firstBoundary + offset));
            return AppliedStatusRules.IsRestricted(baseline, StatusEffectKey.Bind);
        }

        /// <summary>
        /// 예보 준비(<see cref="Builder"/>)와 그 결과가 평가 때 읽는 사실의 사본이다(적 AI 근간 개편 설계 6.8 읽기 집합).
        /// 이 목록 밖의 사실이 바뀌어도 예보와 평가 결과는 같다. 같은 진영 안에서 이 값이 모두 같으면 이전 예보를 다시 쓴다.
        /// 준비: 국면, 턴 사실(완료 누계·밤 종료 판정·제어자 단계), 목표(종류·대상), 판 크기, 기믹 상태(예정·경고·구름·토네이도),
        /// 전투 정의의 기믹 설정과 레일 칸, 메인(하나뿐인지 포함)과 목표 대상의 사실.
        /// 평가: 보관한 입력의 프로필 목록과 보유 기록(강제 이동 면역은 프로필의 행동 정의다).
        /// 기믹 규칙의 예측(<see cref="StageGimmickComposition.ForecastSteps"/>)이나 <see cref="Evaluate"/>가 읽는 값을 바꾸면 여기도 고친다.
        /// </summary>
        internal sealed class ReadSet
        {
            private readonly CombatFlowPhase _phase;
            private readonly CombatTurnFacts _turn;
            private readonly BattleObjective _objective;
            private readonly int _width;
            private readonly int _height;
            private readonly StageGimmickStateSet _gimmicks;
            private readonly bool _hasBattle;
            private readonly StageGimmickSpec[] _definitions;
            private readonly CellCoord[] _rails;
            private readonly int _mainCount;
            private readonly CharacterCombatFacts _main;
            private readonly CharacterCombatFacts _objectiveTarget;
            private readonly IReadOnlyList<CombatAttackProfile> _profiles;
            private readonly IReadOnlyList<OwnedCharacterRecord> _owned;

            private ReadSet(CombatCommandInput input)
            {
                var state = input.State;
                _phase = state.Phase;
                _turn = state.Turn;
                _objective = state.Objective;
                _width = state.BoardWidth;
                _height = state.BoardHeight;
                _gimmicks = state.Gimmicks;
                _hasBattle = input.Battle != null;
                _definitions = input.Battle?.GimmickDefinitions?.ToArray() ?? Array.Empty<StageGimmickSpec>();
                _rails = input.Battle?.RailCells?.ToArray() ?? Array.Empty<CellCoord>();
                foreach (var unit in input.Characters)
                {
                    if (unit.OwnershipRole == CharacterOwnershipRole.Main && _mainCount++ == 0) _main = unit;
                    if (unit.EntityId == state.Objective.TargetEntityId) _objectiveTarget = unit;
                }
                _profiles = input.Profiles;
                _owned = input.Owned;
            }

            internal static ReadSet Capture(CombatCommandInput input) =>
                new ReadSet(input ?? throw new ArgumentNullException(nameof(input)));

            internal bool Matches(ReadSet other) => Difference(other) == null;

            /// <summary>처음 다른 항목의 이름이다. 모두 같으면 null이다(측정이 놓친 이유를 센다).</summary>
            internal string Difference(ReadSet other)
            {
                if (other == null) return "none";
                if (_phase != other._phase || _width != other._width || _height != other._height || !_turn.SameValues(other._turn) ||
                    !_objective.SameValues(other._objective)) return "state";
                if (!_gimmicks.SameValues(other._gimmicks)) return "gimmicks";
                if (_hasBattle != other._hasBattle || _definitions.Length != other._definitions.Length || !_rails.SequenceEqual(other._rails))
                    return "battle";
                for (int i = 0; i < _definitions.Length; i++)
                    if (!ReferenceEquals(_definitions[i], other._definitions[i])) return "battle";
                if (_mainCount != other._mainCount || !EnemyReadSetValues.SameCharacter(_main, other._main)) return "main";
                if (!EnemyReadSetValues.SameCharacter(_objectiveTarget, other._objectiveTarget)) return "objective";
                if (!EnemyReadSetValues.SameProfiles(_profiles, other._profiles)) return "profiles";
                if (!EnemyReadSetValues.SameOwned(_owned, other._owned)) return "owned";
                return null;
            }
        }

        internal sealed class Builder : IDisposable
        {
            private IEnumerator<int> _steps;
            private StageHazardForecast _result;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal bool IsComplete { get { RequireActive(); return _complete; } }
            internal long WorkVersion { get { RequireActive(); return _version; } }
            internal Builder(CombatCommandInput input)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                _steps = Prepare(input).GetEnumerator();
            }
            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete) { _steps.Dispose(); _steps = null; }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = null;
                    throw;
                }
            }
            private IEnumerable<int> Prepare(CombatCommandInput input)
            {
                long current = input.State.Turn.CompletedControllerTurnCount;
                // 밤 종료가 없어 늘 다음 두 진영 종료까지 본다(밤 삭제 설계 2026-11-10).
                long last = checked(current + 2);
                var ordered = new SortedDictionary<(long Boundary, int Order), HazardCellFact>();
                int order = 0;
                yield return 0;
                CharacterCombatFacts main = null, objectiveTarget = null;
                foreach (var unit in input.Characters)
                {
                    if (unit.OwnershipRole == CharacterOwnershipRole.Main)
                    {
                        if (main != null) throw new InvalidOperationException("메인은 유일해야 합니다.");
                        main = unit;
                    }
                    if (unit.EntityId == input.State.Objective.TargetEntityId) objectiveTarget = unit;
                    yield return 0;
                }
                if (main == null) throw new InvalidOperationException("메인 사실이 필요합니다.");
                if (input.State.Objective.Kind == BattleObjectiveKind.DesignatedBossDefeated && objectiveTarget == null)
                    throw new InvalidOperationException("지정 보스 목표의 대상 사실이 필요합니다.");
                if (input.State.Phase != CombatFlowPhase.Finished)
                {
                    if (input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                        throw new InvalidOperationException("적 진영의 다음 기회까지 예측하려면 적 진영 입력이 필요합니다.");
                    foreach (var next in StageGimmickComposition.Default.ForecastSteps(input, last))
                    {
                        if (next.HasValue)
                        {
                            var fact = next.Value;
                            bool currentObservation = fact.BoundaryCount == current &&
                                (fact.TornadoSourceId.HasValue || fact.HaboobRegion != null) && fact.Cell.HasValue;
                            if ((!currentObservation && fact.BoundaryCount <= current) || fact.BoundaryCount > last ||
                                fact.Cell.HasValue && !input.State.IsInside(fact.Cell.Value) ||
                                !fact.Cell.HasValue && fact.Gaps == CombatAssessmentGap.None)
                                throw new InvalidOperationException("예측의 시간·칸·불확실성 근거가 잘못되었습니다.");
                            ordered.Add((fact.BoundaryCount, order++), fact);
                        }
                        yield return 0;
                    }
                }
                var facts = new List<HazardCellFact>();
                var cells = new Dictionary<CellCoord, List<int>>();
                var entries = new Dictionary<CellCoord, int>();
                var unknown = new List<int>();
                var damageGroups = new List<int>();
                var groupEnds = new Dictionary<(StageGimmickId Id, long Boundary), int>();
                IReadOnlyList<CellCoord> haboobRegion = null;
                long? haboobUnknownAt = null;
                foreach (var pair in ordered)
                {
                    var fact = pair.Value;
                    int index = facts.Count;
                    facts.Add(fact);
                    var group = (fact.Id, fact.BoundaryCount);
                    if (!groupEnds.ContainsKey(group) && (fact.InstantDefeat || fact.Rockfall != null) && fact.Cell.HasValue)
                        damageGroups.Add(index);
                    groupEnds[group] = index;
                    if (fact.BoundaryCount == current)
                    {
                        if (fact.TornadoSourceId.HasValue) entries.Add(fact.Cell.Value, index);
                        if (fact.HaboobRegion != null) haboobRegion = fact.HaboobRegion;
                    }
                    else if (fact.Cell is CellCoord cell)
                    {
                        if (!cells.TryGetValue(cell, out var indices)) cells.Add(cell, indices = new List<int>());
                        indices.Add(index);
                    }
                    else
                    {
                        if (fact.Key == StageGimmickKey.Haboob) haboobUnknownAt = fact.BoundaryCount;
                        unknown.Add(index);
                    }
                    yield return 0;
                }
                // 완성된 목록과 색인은 이 결과로 소유권을 옮긴 뒤 다시 변경하지 않는다.
                _result = new StageHazardForecast(input, last, facts, cells, entries, unknown, damageGroups,
                    groupEnds, main, objectiveTarget, haboobRegion, haboobUnknownAt);
                yield return 0;
            }
            internal StageHazardForecast Build()
            {
                RequireActive();
                if (!_complete) throw new InvalidOperationException("예측 준비가 끝나지 않았습니다.");
                return _result;
            }
            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Builder));
                if (_faulted) throw new InvalidOperationException("실패한 예측은 다시 진행할 수 없습니다.");
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = null;
            }
        }
    }
}
