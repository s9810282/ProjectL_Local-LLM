using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>흔적 생성은 자발적 이동에서만 연결하고, 저장·ID 할당은 기존 명령 환경에 맡깁니다.</summary>
    public static class TraitTrailCalculator
    {
        public static int MovementBonusAt(CellCoord cell, MovementEnvironmentFacts environment) =>
            GroundCompactionTraitRule.MovementBonusAt(cell, environment ?? throw new ArgumentNullException(nameof(environment)));
        /// <summary>진영을 알 수 없는 기존 조회에서 면제를 적용하지 않은 환경의 사거리 감소를 반환합니다.</summary>
        public static int RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment) =>
            DustScreenTraitRule.UnfilteredRangeReductionAt(cell, environment ?? throw new ArgumentNullException(nameof(environment)));

        public static int RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide) =>
            DustScreenTraitRule.RangeReductionAt(cell, environment ?? throw new ArgumentNullException(nameof(environment)), queryingSide);

        /// <summary>
        /// 이동자가 방금 떠난 칸에 흔적을 남깁니다. 흔적은 지나간 칸(들어갔다가 떠난 칸)에만 생기므로
        /// 출발 칸과 이동이 멈춘 칸(도착 칸)에는 생기지 않습니다(2026-11-02 사용자 결정).
        /// </summary>
        internal static void EmitOnLeave(MovementProjection projection, CellCoord left)
        {
            if (left == projection.Input.Path.Origin) return;
            if (EnvironmentTerrainRules.BlocksVoluntaryEntry(projection.Environment, projection.Bindings, left)) return;
            var actor = projection.Actor;
            if (!actor.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.GroundCompaction || t.Key == TraitKey.DustScreen)) return;
            foreach (var binding in CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(projection.Input, actor, projection.ProfileReads)))
            {
                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                var trail = binding.Rule switch
                {
                    GroundCompactionEffectRule rule => rule.Create(input, actor, left, projection.NextEnvironmentSourceId,
                        projection.Input.State.Turn.ControllerTurnSequence),
                    DustScreenEffectRule rule => rule.Create(input, actor, left, projection.NextEnvironmentSourceId),
                    _ => null
                };
                if (trail != null) projection.AddTraitTrail(trail);
            }
        }

        internal static CombatCommandState EndControllerTurn(CombatCommandState state, Owner endedSide, List<CombatEvent> events = null)
        {
            var environment = new MovementEnvironmentFacts(state.Environment.Tiles.Select(t => t is TraitTrailFact trail
                ? trail.EndControllerTurn(endedSide, state.Turn.ControllerTurnSequence) : t).ToArray());
            RecordChanges(state.Environment, environment, events);
            return environment.SameValues(state.Environment) ? state : state.With(environment: environment);
        }

        internal static MovementEnvironmentFacts Clear(MovementEnvironmentFacts environment, List<CombatEvent> events = null)
        {
            var final = new MovementEnvironmentFacts(environment.Tiles.Select(t => t is TraitTrailFact trail && trail.Active
                ? trail.WithRemainingTurnEnds(0) : t).ToArray());
            RecordChanges(environment, final, events);
            return final;
        }

        internal static void RecordCreation(TraitTrailFact trail, List<CombatEvent> events)
        {
            CombatCommandEvents.Trait(events, new EntityId(trail.Origin.OwnerInstanceId), trail.Key, TraitActivationKind.TrailCreated,
                1, cells: new[] { trail.Cell });
            events.Add(new CombatMovementSourceEvent(null, trail));
        }

        internal static void RecordChanges(MovementEnvironmentFacts before, MovementEnvironmentFacts final, List<CombatEvent> events)
        {
            if (events == null) return;
            foreach (var trail in before.Tiles.OfType<TraitTrailFact>().OrderBy(t => t.Id))
            {
                var after = final.Tiles.OfType<TraitTrailFact>().Single(t => t.Id == trail.Id);
                if (trail.SameValues(after)) continue;
                CombatCommandEvents.Trait(events, new EntityId(trail.Origin.OwnerInstanceId), trail.Key,
                    after.Active ? TraitActivationKind.TrailChanged : TraitActivationKind.TrailRemoved,
                    after.RemainingTurnEnds, cells: new[] { trail.Cell });
                events.Add(new CombatMovementSourceEvent(trail, after));
            }
        }
    }
}
