using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>완료된 유닛 턴 경계에서만 실행되는 명령 내부 지형 계산이다.</summary>
    public static class TornadoCalculator
    {
        public static bool IsDue(long completedUnitTurns, long nextDueCompletedTurn) => completedUnitTurns >= nextDueCompletedTurn;

        internal static void ResolveDue(MovementProjection projection, IReadOnlyList<IMovementTileRule> rules)
        {
            long completed = projection.CurrentInput().State.Turn.CompletedUnitTurnCount;
            var due = projection.Environment.Tiles.OfType<TornadoSourceFact>()
                .Where(t => t.Active && IsDue(completed, t.NextDueCompletedTurn)).OrderBy(t => t.Id).ToArray();
            foreach (var source in due)
            {
                if (projection.Terminal) return;
                var current = projection.CurrentInput();
                var occupant = current.Characters.SingleOrDefault(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                    c.OccupiedCells.Contains(source.Cell));
                if (occupant != null)
                {
                    if (occupant.OccupiedCells.Count != 1)
                        throw new NotSupportedException("다중 셀 유닛의 Tornado 이동에는 접촉 정책이 필요합니다.");
                    var candidates = UnitCandidates(current, source.Cell);
                    if (candidates.Count > 0)
                    {
                        var target = candidates[projection.ChooseRandom(candidates.Count)];
                        var moved = CombatMovementCalculator.SealPendingMovement(CombatMovementCalculator.MoveTo(occupant, target), target);
                        projection.MergeCharacter(new CharacterStateChange(occupant, moved));
                        projection.Events.Add(new CombatCharacterEvent(CombatEventKind.Relocated, occupant, moved, tornadoSourceId: source.Id));
                        // 위치 기여분을 교체한 뒤 진입 피해를 계산한다.
                        projection.ReconcilePresence(new[] { source.Cell, target });
                        MovementEnvironmentCalculator.ResolveEntry(projection, target, rules, occupant.EntityId);
                        projection.ResolveForcedArrival(occupant.EntityId);
                        projection.ResolveDeathBoundary();
                    }
                }
                if (projection.Terminal) return;
                var respawn = SourceCandidates(projection.CurrentInput(), source);
                var final = source.AfterActivation(respawn.Count == 0 ? source.Cell : respawn[projection.ChooseRandom(respawn.Count)]);
                projection.ChangeTornado(source, final);
                projection.Events.Add(new CombatTornadoEvent(source, final));
                projection.ReconcilePresence(new[] { source.Cell, final.Cell });
            }
        }

        internal static IReadOnlyList<CellCoord> UnitCandidates(CombatCommandInput input, CellCoord origin)
        {
            var blocked = input.State.Structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells)
                .Concat(input.Characters.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field).SelectMany(c => c.OccupiedCells)).ToHashSet();
            blocked.Add(origin);
            return Cells(input.State).Where(cell => !blocked.Contains(cell) &&
                !EnvironmentTerrainRules.IsWallOccupied(input.State.Environment, input.EnvironmentBindings, cell)).ToArray();
        }

        internal static IReadOnlyList<CellCoord> SourceCandidates(CombatCommandInput input, TornadoSourceFact source)
        {
            var blocked = input.State.Structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells)
                .Concat(input.State.Environment.Tiles.Where(t => t.Active && input.EnvironmentBindings.TerrainSourceIds.Contains(t.Id))
                    .Select(t => t.Cell)).ToHashSet();
            blocked.Add(source.Cell);
            return Cells(input.State).Where(cell => !blocked.Contains(cell)).ToArray();
        }

        private static IEnumerable<CellCoord> Cells(CombatCommandState state)
        {
            for (int y = 0; y < state.BoardHeight; y++)
                for (int x = 0; x < state.BoardWidth; x++) yield return new CellCoord(x, y);
        }
    }
}
