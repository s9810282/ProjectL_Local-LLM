using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class TraitAttackNotifications
    {
        internal static void Apply(CombatCommandInput input, CommandAttackProjection world, AttackTraitScope scope)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            var actor = world.Characters[scope.ActorId];
            if (actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field)
                throw new InvalidOperationException("공격 알림에는 생존한 필드 행동자가 필요합니다.");
            var turn = world.TraitTurn;
            int ordinal = checked((actor.Owner == Owner.Player ? turn.PlayerAttackActionCount : turn.EnemyAttackActionCount) + 1);
            world.TraitTurn = actor.Owner == Owner.Player
                ? new TraitTurnFacts(ordinal, turn.EnemyAttackActionCount)
                : new TraitTurnFacts(turn.PlayerAttackActionCount, ordinal);
            var allies = world.Characters.Values.Where(c => c.Owner == actor.Owner &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId).ToArray();
            foreach (var source in allies)
            {
                if (!global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(source.Stats.TraitStatuses, TraitKey.Vanguard, TraitKey.CleanSweep)) continue;
                var profile = CombatCommandCalculator.RequiredProfile(input, source);
                foreach (var binding in CombatCommandEffects.Capture(source, profile))
                {
                    var bound = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                    TraitAttackGrant grant = binding.Rule switch
                    {
                        VanguardEffectRule rule => rule.Evaluate(bound, ordinal, source.EntityId == actor.EntityId),
                        CleanSweepEffectRule rule => rule.Evaluate(bound, ordinal, source.EntityId == actor.EntityId),
                        _ => null
                    };
                    if (grant == null) continue;
                    var recipients = (binding.Rule is VanguardEffectRule
                        ? allies.Where(c => c.EntityId != source.EntityId) : new[] { source }).ToArray();
                    if (grant.Amount > 0 && recipients.Length > 0)
                        CombatCommandEvents.Trait(world.Events, source.EntityId, (TraitKey)grant.Origin.DefinitionKey,
                            TraitActivationKind.AttackGranted, grant.Amount, recipients.Select(c => c.EntityId).ToArray());
                    foreach (var recipient in recipients)
                    {
                        var current = world.Characters[recipient.EntityId];
                        int hunter = current.OwnedId.HasValue ? input.Owned.Single(o => o.Id == current.OwnedId.Value).HunterAttackBonus : 0;
                        world.Characters[current.EntityId] = CombatEffectiveStatsCalculator.GrantTraitAttack(current, grant.ToStatus(), hunter);
                        CombatCommandEvents.TraitState(world.Events, current, world.Characters[current.EntityId]);
                    }
                }
            }
        }
    }
}
