using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class StageGimmickComposition
    {
        // 등록 목록과 실행 순서는 한 곳에서 관리하며, 이동 규칙의 연결 여부로 지원 기믹이 달라지지 않는다.
        internal static StageGimmickComposition Default { get; } = Create(null);
        private readonly IStageGimmickRule[] _rules;

        internal static StageGimmickComposition WithMovementRules(IReadOnlyList<IMovementTileRule> rules)
        {
            MovementEnvironmentCalculator.RequireRules(rules);
            return Create(rules);
        }

        private static StageGimmickComposition Create(IReadOnlyList<IMovementTileRule> rules) =>
            new StageGimmickComposition(new IStageGimmickRule[] {
                new TornadoGimmickRule(rules), new RailwayGimmickRule(rules), new HaboobGimmickRule(), new RockfallGimmickRule(rules),
                new MeteorGimmickRule(rules), new LavaChainGimmickRule(rules) });

        internal StageGimmickComposition(IReadOnlyList<IStageGimmickRule> rules)
        {
            _rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
            if (_rules.Any(r => r == null || r.Key == StageGimmickKey.None || !Enum.IsDefined(typeof(StageGimmickKey), r.Key)) ||
                _rules.Select(r => r.Key).Distinct().Count() != _rules.Length)
                throw new InvalidOperationException("기믹 처리기 종류가 잘못되었거나 중복되었습니다.");
        }

        internal void ValidateDefinitions(IReadOnlyList<StageGimmickSpec> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (definitions.Any(d => d == null) || definitions.Select(d => d.GimmickKey).Distinct().Count() != definitions.Count ||
                definitions.Any(d => !_rules.Any(r => r.Key == d.GimmickKey)))
                throw new InvalidOperationException("기믹 정의가 중복되었거나 등록된 처리기가 없습니다.");
        }

        internal static CombatCommandState Initialize(CombatCommandState state, IReadOnlyList<StageGimmickSpec> definitions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            Default.ValidateDefinitions(definitions);
            var entries = new List<StageGimmickState>();
            uint random = state.EnvironmentProgress.RandomState;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var rule = Default._rules.Single(r => r.Key == definition.GimmickKey);
                var entry = rule.Initialize(state, definition, new StageGimmickId(i + 1), ref random);
                if (entry != null) entries.Add(entry);
            }
            var gimmicks = new StageGimmickStateSet(entries);
            gimmicks.ValidateDefinitions(definitions, state.BoardWidth, state.BoardHeight);
            var progress = state.EnvironmentProgress;
            return state.With(gimmicks: gimmicks,
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random));
        }

        /// <summary>
        /// 도착 반응이 있는 기믹(<see cref="IStageGimmickEntryRule"/>)을 등록 순서대로 적용한다.
        /// 이동 계산의 도착 지점은 이 함수 하나만 부르며, 새 도착 반응 기믹은 규칙에 인터페이스만 구현한다.
        /// </summary>
        internal static void ApplyEntry(MovementProjection projection, EntityId arriver)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            foreach (var (entry, definition, id) in EntryRules(projection.Input.Battle))
                entry.ApplyEntry(projection, definition, id, arriver);
        }

        /// <summary>
        /// 도착 반응 기믹이 이번 이동에서 바꾼 몫을 되돌림 영수증으로 모은다(<see cref="ApplyEntry"/>의 짝).
        /// 이동 되돌리기는 이 함수 하나만 부르며, 새 도착 반응 기믹은 같은 규칙에 영수증 인정만 더한다.
        /// </summary>
        internal static void CollectEntryReceipts(CombatCommandInput input, MovementEffectId movement,
            CharacterCombatFacts before, CharacterCombatFacts after, bool requireContact, ICollection<MovementEffectReceipt> receipts)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (receipts == null) throw new ArgumentNullException(nameof(receipts));
            foreach (var (entry, definition, id) in EntryRules(input.Battle))
                entry.CollectEntryReceipts(input, definition, id, movement, before, after, requireContact, receipts);
        }

        private static IEnumerable<(IStageGimmickEntryRule Rule, StageGimmickSpec Definition, StageGimmickId Id)> EntryRules(
            CombatBattleFacts battle)
        {
            var definitions = battle?.GimmickDefinitions;
            if (definitions == null || definitions.Count == 0) yield break;
            foreach (var rule in Default._rules)
            {
                if (rule is not IStageGimmickEntryRule entry) continue;
                for (int i = 0; i < definitions.Count; i++)
                    if (definitions[i].GimmickKey == rule.Key)
                        yield return (entry, definitions[i], new StageGimmickId(i + 1));
            }
        }

        internal IEnumerable<HazardCellFact?> ForecastSteps(CombatCommandInput input, long lastBoundaryCount)
        {
            var definitions = input.Battle?.GimmickDefinitions;
            if (definitions == null) yield break;
            ValidateDefinitions(definitions);
            foreach (var rule in _rules)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    yield return null;
                    if (definitions[i].GimmickKey != rule.Key) continue;
                    using var child = rule.EnumerateForecast(input, definitions[i], new StageGimmickId(i + 1), lastBoundaryCount);
                    while (child.MoveNext()) yield return child.Current;
                }
            }
        }

        internal CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickBoundary? boundary)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (boundary.HasValue && (boundary.Value.EndedSide != Owner.Player && boundary.Value.EndedSide != Owner.Enemy ||
                boundary.Value.CompletedControllerTurnCount < 1 || boundary.Value.CompletedControllerTurnCount != input.State.Turn.CompletedControllerTurnCount))
                throw new InvalidOperationException("기믹 종료 경계가 입력의 완료 누계와 일치해야 합니다.");
            if (input.State.Phase == CombatFlowPhase.Finished || input.Battle == null || input.Battle.GimmickDefinitions.Count == 0) return null;
            var definitions = input.Battle.GimmickDefinitions;
            ValidateDefinitions(definitions);
            if (!boundary.HasValue && definitions.Any(d => d.GimmickKey != StageGimmickKey.Railway))
                throw new InvalidOperationException("예약 기믹에는 완료 진영 경계가 필요합니다.");
            var steps = new List<CombatCommandTransition>();
            var projected = input;
            var phase = boundary?.Phase ?? StageGimmickBoundaryPhase.Scheduled;
            foreach (var rule in _rules)
            {
                if (!rule.RunsAt(phase)) continue;
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i].GimmickKey != rule.Key) continue;
                    var step = rule.TryAdvance(projected, definitions[i], new StageGimmickId(i + 1), boundary);
                    if (step == null) continue;
                    if (!step.Accepted || !ReferenceEquals(step.Input, projected) ||
                        !step.FinalState.Turn.SameValues(projected.State.Turn))
                        throw new InvalidOperationException("기믹은 원본 턴 사실을 보존한 수락 전이만 반환해야 합니다.");
                    steps.Add(step);
                    if (step.Outcome.IsStageEnded) return CombatCommandComposition.Merge(input, steps);
                    projected = CombatCommandComposition.Project(step);
                }
            }
            return steps.Count == 0 ? null : CombatCommandComposition.Merge(input, steps);
        }
    }
}
