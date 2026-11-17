using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class TornadoGimmickRule : IStageGimmickRule
    {
        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var spec = definition as TornadoGimmickSpec ?? throw new InvalidOperationException("토네이도 설정 형식이 다릅니다.");
            var group = (TornadoGimmickState)input.State.Gimmicks.Get(id);
            long current = input.State.Turn.CompletedControllerTurnCount;
            var seen = new HashSet<(long Boundary, CellCoord Cell)>();
            var unknown = new HashSet<(long Boundary, bool Contact)>();
            for (int i = 0; i < spec.Members.Count; i++)
            {
                yield return null;
                var member = spec.Members[i];
                if (!member.KnownTo.Contains(Owner.Enemy)) continue;
                var state = group.Members[i];
                if (state.SourceId != member.SourceId || state.NextBoundaryCount <= current)
                    throw new InvalidOperationException("토네이도 원본이나 예정 경계가 올바르지 않습니다.");
                foreach (var cell in TornadoGeometry.FootprintSteps(state.Center, member.SideLength))
                    yield return Contact(current, cell, member.SourceId);

                long warning = state.NextBoundaryCount;
                if (state.Phase == TornadoGimmickPhase.Warning)
                {
                    if (state.NextBoundaryCount > lastBoundaryCount) continue;
                    foreach (var center in TornadoGeometry.TravelSteps(state.Center, member.SideLength, member.DiagonalDistance,
                        input.State.BoardWidth, input.State.BoardHeight, state.LockedDirection.Value))
                        foreach (var cell in TornadoGeometry.FootprintSteps(center, member.SideLength))
                            yield return Contact(state.NextBoundaryCount, cell, member.SourceId);
                    warning = TornadoGimmickPreparationCalculator.WarningAfterTravel(member, state.NextBoundaryCount);
                }
                if (warning > lastBoundaryCount) continue;
                yield return Unpublished(warning, false, member.SourceId);
                long travel = TornadoGimmickPreparationCalculator.TravelAfterWarning(member, warning);
                if (travel <= lastBoundaryCount)
                    yield return Unpublished(travel, true, member.SourceId);
            }

            HazardCellFact? Contact(long boundary, CellCoord cell, long source) => seen.Add((boundary, cell))
                ? new HazardCellFact(id, Key, boundary, cell, gaps: StageHazardForecast.TornadoThrowGaps, tornadoSourceId: source)
                : null;

            HazardCellFact? Unpublished(long boundary, bool contact, long source) => unknown.Add((boundary, contact))
                ? new HazardCellFact(id, Key, boundary, null, gaps: CombatAssessmentGap.FutureWarning |
                    (contact ? StageHazardForecast.TornadoThrowGaps : CombatAssessmentGap.None),
                    tornadoSourceId: contact ? source : null)
                : null;
        }

        public StageGimmickKey Key => StageGimmickKey.Tornado;

        public bool RunsAt(StageGimmickBoundaryPhase phase) => phase == StageGimmickBoundaryPhase.Scheduled;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            var spec = definition as TornadoGimmickSpec ?? throw new InvalidOperationException("토네이도 설정 형식이 다릅니다.");
            return TornadoGimmickPreparationCalculator.Prepare(spec, id, initialState.BoardWidth, initialState.BoardHeight,
                ref randomState, initialState.Turn.CompletedControllerTurnCount);
        }

        private readonly IReadOnlyList<IMovementTileRule> _rules;
        internal TornadoGimmickRule(IReadOnlyList<IMovementTileRule> rules)
        {
            // 정의 검증은 이동 규칙 없이 허용하지만, 실행에는 호출자의 규칙이 필요하다.
            if (rules != null)
            {
                MovementEnvironmentCalculator.RequireRules(rules);
                _rules = Array.AsReadOnly(rules.ToArray());
            }
        }
        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var spec = definition as TornadoGimmickSpec ?? throw new InvalidOperationException("토네이도 설정 형식이 다릅니다.");
            if (_rules == null) throw new InvalidOperationException("토네이도 실행에는 IMovementTileRule 목록을 전달해야 합니다.");
            var end = boundary ?? throw new InvalidOperationException("토네이도에는 완료 진영 경계가 필요합니다.");
            if (end.CompletedControllerTurnCount != input.State.Turn.CompletedControllerTurnCount)
                throw new InvalidOperationException("토네이도 경계와 입력의 완료 누계가 다릅니다.");
            var before = (TornadoGimmickState)input.State.Gimmicks.Get(id);
            if (input.State.Turn.StagePhase != StagePhase.Combat || input.State.Phase == CombatFlowPhase.Finished ||
                end.CompletedControllerTurnCount < before.NextBoundaryCount) return null;
            if (end.CompletedControllerTurnCount != before.NextBoundaryCount)
                throw new InvalidOperationException("토네이도의 예정 경계를 건너뛸 수 없습니다.");
            input.State.Gimmicks.ValidateDefinitions(input.Battle.GimmickDefinitions, input.State.BoardWidth, input.State.BoardHeight);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>(), rules: _rules);
            if (projection.Terminal) return null;
            projection.TornadoResolution = new TornadoContactResolution();
            var advances = new List<TornadoMemberAdvance>();
            try
            {
                foreach (var member in spec.Members)
                {
                    var expected = before.Members.Single(m => m.SourceId == member.SourceId);
                    if (expected.NextBoundaryCount != end.CompletedControllerTurnCount) continue;
                    TornadoMemberState final;
                    IReadOnlyList<CellCoord> path;
                    if (expected.Phase == TornadoGimmickPhase.Cooldown)
                    {
                        uint random = projection.CurrentInput().State.EnvironmentProgress.RandomState;
                        uint originalRandom = random;
                        final = TornadoGimmickPreparationCalculator.Warn(member, expected, input.State.BoardWidth, input.State.BoardHeight, ref random);
                        projection.SetEnvironmentRandom(originalRandom, random);
                        path = new[] { expected.Center };
                    }
                    else
                    {
                        var planned = TornadoGeometry.Travel(expected.Center, member.SideLength, member.DiagonalDistance,
                            input.State.BoardWidth, input.State.BoardHeight, expected.LockedDirection.Value);
                        var reached = new List<CellCoord>();
                        for (int step = 0; step < planned.Count; step++)
                        {
                            ReplaceMember(projection, id, new TornadoMemberState(expected.SourceId, planned[step], expected.HorizontalSign,
                                expected.Phase, expected.NextBoundaryCount, expected.LockedDirection));
                            reached.Add(planned[step]);
                            TornadoCalculator.ResolveSweepCenter(projection, id, member, planned[step], step, _rules);
                            if (projection.Terminal) break;
                        }
                        path = reached.AsReadOnly();
                        if (projection.Terminal)
                        {
                            // 종료 경계에는 실제 중심만 남기고 다음 정상 주기를 예약하지 않는다.
                            final = new TornadoMemberState(expected.SourceId, reached[reached.Count - 1], expected.HorizontalSign,
                                TornadoGimmickPhase.Cooldown, expected.NextBoundaryCount, null);
                        }
                        else
                        {
                            uint random = projection.CurrentInput().State.EnvironmentProgress.RandomState;
                            uint originalRandom = random;
                            final = TornadoGimmickPreparationCalculator.CompleteTravel(member, expected,
                                input.State.BoardWidth, input.State.BoardHeight, ref random).State;
                            projection.SetEnvironmentRandom(originalRandom, random);
                        }
                    }
                    ReplaceMember(projection, id, final);
                    var advance = new TornadoMemberAdvance(expected, final, member.SideLength, path);
                    advances.Add(advance);
                    projection.Events.Add(new CombatTornadoMemberEvent(id, advance));
                    if (projection.Terminal) break;
                }
                var after = (TornadoGimmickState)projection.Gimmicks.Get(id);
                projection.Events.Insert(0, new CombatTornadoGimmickEvent(before, after, end, advances));
                return projection.Complete();
            }
            finally
            {
                projection.TornadoResolution = null;
            }
        }

        private static void ReplaceMember(MovementProjection projection, StageGimmickId id, TornadoMemberState member)
        {
            var before = (TornadoGimmickState)projection.Gimmicks.Get(id);
            projection.ReplaceGimmick(before, new TornadoGimmickState(id,
                before.Members.Select(m => m.SourceId == member.SourceId ? member : m).ToArray()));
        }
    }
}
