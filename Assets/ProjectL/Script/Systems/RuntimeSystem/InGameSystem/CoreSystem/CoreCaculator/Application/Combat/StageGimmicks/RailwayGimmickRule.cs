using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal sealed class RailwayGimmickRule : IStageGimmickRule
    {
        // 즉사 대상에 붙은 가시 폭탄의 연쇄 넉백이 쓰는 지형 규칙이다(가시 폭탄 Phase 4 D1). 폭탄이 없으면 쓰지 않는다.
        private readonly IReadOnlyList<IMovementTileRule> _rules;
        internal RailwayGimmickRule(IReadOnlyList<IMovementTileRule> rules = null) { _rules = rules; }

        public StageGimmickKey Key => StageGimmickKey.Railway;

        public bool RunsAt(StageGimmickBoundaryPhase phase) => phase == StageGimmickBoundaryPhase.Scheduled;

        public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState)
        {
            if (definition is not RailwayGimmickSpec) throw new InvalidOperationException("기차 설정 형식이 다릅니다.");
            return null;
        }

        internal static bool IsDue(RailwayGimmickSpec spec, long completedCount) =>
            completedCount >= spec.FirstActivationAfterControllerTurns &&
            (completedCount - spec.FirstActivationAfterControllerTurns) % spec.RepeatIntervalControllerTurns == 0;

        public IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount)
        {
            var spec = definition as RailwayGimmickSpec ?? throw new InvalidOperationException("기차 설정 형식이 다릅니다.");
            var cells = input.Battle.RailCells;
            if (cells.Count == 0) throw new InvalidOperationException("기차 설정에는 레일 칸이 필요합니다.");
            var unique = new HashSet<CellCoord>();
            foreach (var cell in cells)
            {
                if (!input.State.IsInside(cell) || !unique.Add(cell))
                    throw new InvalidOperationException("기차 레일 칸이 보드 밖이거나 중복되었습니다.");
                yield return null;
            }
            long count = input.State.Turn.CompletedControllerTurnCount;
            while (count < lastBoundaryCount)
            {
                count = checked(count + 1);
                if (IsDue(spec, count))
                    foreach (var cell in cells) yield return new HazardCellFact(id, Key, count, cell, instantDefeat: true);
                else yield return null;
            }
        }

        public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition, StageGimmickId gimmickId, StageGimmickBoundary? boundary)
        {
            var railway = definition as RailwayGimmickSpec ?? throw new InvalidOperationException("기차 설정 형식이 다릅니다.");
            var cells = input.Battle.RailCells.ToArray();
            var rails = new HashSet<CellCoord>();
            if (cells.Length == 0) throw new InvalidOperationException("기차 설정에는 레일 칸이 필요합니다.");
            foreach (var cell in cells)
                if (cell.X < 0 || cell.Y < 0 || cell.X >= input.State.BoardWidth || cell.Y >= input.State.BoardHeight || !rails.Add(cell))
                    throw new InvalidOperationException("기차 레일 칸이 보드 밖이거나 중복되었습니다.");

            long completed = input.State.Turn.CompletedControllerTurnCount;
            if (!IsDue(railway, completed)) return null;

            var victims = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(rails.Contains)).OrderBy(c => c.EntityId).ToArray();
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>
            {
                new CombatGimmickEvent(new RailwayGimmickActivationSnapshot(gimmickId, cells,
                    victims.Select(c => c.EntityId).ToArray()))
            };
            var executions = new List<CombatHealthExecution>();
            var source = new CombatHealthSource(CombatHealthOperationKind.Environment, contextEvent: events[0],
                damageCause: CombatDamageCause.InstantDefeat);
            // 모든 직접 사망을 먼저 확정해 Railway 바로 뒤에 연속 기록한다. 후속 정산이 대상 사이에 끼면 접촉 재생 계약이 깨진다.
            foreach (var victim in victims)
            {
                if (!victim.Anchor.HasValue) throw new InvalidOperationException("기차 사망 대상의 필드 위치가 없습니다.");
                var dead = CombatCommandCalculator.Hurt(victim, victim.Health.CurrentHealth);
                projected[victim.EntityId] = dead;
                var evidence = CombatCommandEvents.Health(events, victim, dead);
                CombatHealthRecording.Add(executions, victim.EntityId, victim.Health, dead.Health, source, evidence,
                    loss: victim.Health.CurrentHealth);
            }
            // 즉사는 일반 피해/공격 귀속을 만들지 않는다. 공통 귀속 규칙은 출처가 없으면 명령 행위자로 돌아가므로,
            // 기차는 환경 즉사로서 그 대체 행위자도 비운다.
            // 사망·영혼·아이템·보상·승패는 낙석처럼 이동 투영의 명령 완료 정산 한 번으로 끝낸다. 그래서 즉사 대상에 붙은
            // 가시 폭탄(가시 폭탄 Phase 4 D1, 설계 P09)과 사망 특성(자폭·공포, 2026-11-17 사용자 결정으로 기차 제외를 풂)도
            // 같은 완료 정산을 탄다. 둘 다 사망 직전 위치, 곧 치이기 전 레일 칸에서 발동한다.
            // 영혼 자리도 완료 정산의 기본값인 치이기 전 위치다(옛 정산에 넘기던 사망 위치와 같다).
            var projection = MovementProjection.ForArrival(input.WithActor(EntityId.Empty), projected, events, rules: _rules);
            projection.Damage.HealthExecutions.AddRange(executions);
            // 외부 게시의 요청·처리 종류·행위자는 원본 그대로다. 내부 정산 문맥만 환경 귀속으로 바꾼다.
            return CombatCommandComposition.Merge(input, new[] { projection.Complete() });
        }
    }
}
