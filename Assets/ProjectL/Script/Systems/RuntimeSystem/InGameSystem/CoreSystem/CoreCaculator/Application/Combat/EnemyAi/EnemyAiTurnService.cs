using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{

    internal static class EnemyAiTurnService
    {
        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics = null, bool immediateCompatibility = true, EnemyDecisionSideCache sideCache = null)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyTurn);
            var pending = BossRoarCalculator.PendingBosses(input);
            if (pending.Count > 0) return BossRoarCalculator.TryRoar(input, pending[0], rules);

            using var session = immediateCompatibility
                ? EnemyDecisionSession.BeginImmediateCompatibility(input, rules, EnemyDecisionSettings.Default, diagnostics)
                : new EnemyDecisionSession(input, rules, EnemyDecisionSettings.Default, diagnostics, sideCache);
            while (true)
            {
                while (!session.IsReady) session.Step();
                var decision = session.Decision;
                var result = Execute(input, decision, rules);
                if (result.Accepted || decision.Kind == EnemyDecisionKind.EndEnemyPhase)
                {
                    if (result.Accepted && decision.Kind == EnemyDecisionKind.Attack)
                        session.Diagnostics.SelectedAim = decision.Aim;
                    return result;
                }
                session.RejectSelected(result.Reason);
            }
        }

        /// <summary>선택된 행동의 실제 정산과 도발 사건 순서를 기존 명령 소유자에게 맡긴다.</summary>
        internal static CombatCommandTransition Execute(CombatCommandInput input, EnemyDecision decision,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (decision.Kind == EnemyDecisionKind.EndEnemyPhase)
                return CombatTurnCalculator.EndEnemyPhase(input, rules);
            var kind = decision.Kind == EnemyDecisionKind.Attack ? CombatCommandKind.Attack : CombatCommandKind.Move;
            var actorInput = CombatCommandComposition.Intent(input.WithActor(decision.ActorId), kind, decision.Aim, path: decision.Path);
            CombatCommandTransition transition;
            using (CoreProfiling.Sample(CoreProfileSection.EnemyActionExecution))
                transition = CombatCommandCalculator.Calculate(actorInput, rules);
            if (!transition.Accepted) return transition;
            var actor = input.Characters.Single(c => c.EntityId == decision.ActorId);
            return CombatCommandComposition.Merge(input, new[] { RecordTaunt(transition, actor, decision.TauntSourceId) });
        }

        private static CombatCommandTransition RecordTaunt(CombatCommandTransition transition, CharacterCombatFacts actor, EntityId source)
        {
            if (source.IsEmpty) return transition;
            var events = new List<CombatEvent>();
            CombatCommandEvents.Trait(events, source, TraitKey.Taunt, TraitActivationKind.TauntSelected, 0,
                new[] { actor.EntityId }, actor.OccupiedCells, CombatCommandEvents.FindOrigin(transition.Events, actor.EntityId));
            events.AddRange(transition.Events);
            return CombatCommandComposition.WithEvents(transition, events);
        }
    }
}
