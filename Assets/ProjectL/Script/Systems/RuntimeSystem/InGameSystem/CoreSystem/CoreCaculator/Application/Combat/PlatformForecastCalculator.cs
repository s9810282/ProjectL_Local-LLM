using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공개 원본과 현재 환경 난수에서 실제 사전 이동 계산을 가상으로 실행한다.</summary>
    public static class PlatformForecastCalculator
    {
        public static PlatformForecastResult Forecast(CombatCommandInput input, long queryRevision,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (queryRevision < 0) throw new ArgumentOutOfRangeException(nameof(queryRevision));

            var state = input.State;
            var source = new PlatformForecastSource(input.BattleInstanceId, queryRevision, state);
            var turn = state.Turn;
            MovementEnvironmentFacts environment = null;
            foreach (var _ in state.Environment.VisibleSteps(Owner.Player, facts => environment = facts, preservePublic: true)) { }
            if (turn.StagePhase != StagePhase.Combat || state.Phase is CombatFlowPhase.Finished or CombatFlowPhase.None ||
                !environment.Tiles.OfType<MovingPlatformFact>().Any())
                return Empty();

            // 활성 진영 중에는 추가 행동·종료 기믹 전의 판을 가정하며, 끝난 경계를 중복 예측하지 않는다.
            if (turn.ControllerPhase is CombatControllerPhase.Player or CombatControllerPhase.Enemy)
            {
                bool player = turn.ControllerPhase == CombatControllerPhase.Player;
                turn = new CombatTurnFacts(StagePhase.Combat, player ? CombatControllerPhase.StartingEnemy : CombatControllerPhase.StartingPlayer,
                    turn.ControllerTurnSequence, checked(turn.CompletedPlayerTurnCount + (player ? 1 : 0)),
                    turn.EnemyOrigins, turn.Summons, checked(turn.CompletedControllerTurnCount + 1));
            }
            var projectedState = state.With(phase: CombatFlowPhase.Automatic, turn: turn, environment: environment);
            // 실제 계산과 같은 시작 조건으로 판정해 끝난 경계를 중복 예측하지 않는다.
            if (!PlatformMotionRules.IsPreludeDue(projectedState, environment.Tiles.OfType<MovingPlatformFact>().Count()))
                return Empty();

            var ids = environment.Tiles.Select(tile => tile.Id).ToHashSet();
            var bindings = input.EnvironmentBindings;
            var knownBindings = new EnvironmentSemanticBindings(bindings.TerrainSourceIds.Where(ids.Contains).ToArray(),
                bindings.ObjectSourceIds.Where(ids.Contains).ToArray(), bindings.ObstacleEntityIds);
            var knownInput = input.WithWorld(projectedState, input.Characters, input.Main, input.SoulCounts, input.Souls, knownBindings);
            var prediction = PlatformMotionCalculator.TryAdvanceBeforeTurn(knownInput, rules, Owner.Player);
            if (prediction == null) return Empty();
            // 숨은 원본 유무를 신호로 공개하지 않는다. 경로 응답은 항상 공개 정보 기준 조건부다.
            return new PlatformForecastResult(source, turn.CompletedControllerTurnCount,
                PlatformForecastCoverage.KnownInformationConditional, prediction.Events.OfType<CombatPlatformMotionEvent>());

            PlatformForecastResult Empty() => new PlatformForecastResult(source, null,
                PlatformForecastCoverage.Exact, Array.Empty<CombatPlatformMotionEvent>());
        }
    }
}
