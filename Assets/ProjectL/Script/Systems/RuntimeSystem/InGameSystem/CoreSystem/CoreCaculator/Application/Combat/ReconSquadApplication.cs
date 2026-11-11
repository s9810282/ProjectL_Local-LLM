using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>확정 경로의 시작·진입 지점에서 한 번씩 적을 조사합니다. 강제 이동과 취소는 이 경계를 호출하지 않습니다.</summary>
    internal static class ReconSquadApplication
    {
        internal static void Scan(MovementProjection world, CellCoord cell, ISet<EntityId> contacted)
        {
            foreach (var _ in ScanSteps(world, cell, contacted)) { }
        }

        internal static IEnumerable<int> ScanSteps(MovementProjection world, CellCoord cell, ISet<EntityId> contacted)
        {
            var actor = world.Actor;
            if (actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field || world.Terminal) yield break;
            var input = world.Input;
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor, world.ProfileReads));
            MovementEffectId? movement = actor.Owner == Owner.Player && MovementUndoPolicy.InPlayerTurn(input) &&
                input.ProcessingKind == CombatProcessingKind.Command && input.Number > 0 && input.BattleInstanceId > 0
                ? new MovementEffectId(input.BattleInstanceId, input.State.Turn.ControllerTurnSequence, actor.EntityId, input.Number) : null;
            foreach (var binding in bindings)
            {
                yield return 0;
                if (binding.Rule is not ReconSquadEffectRule rule) continue;
                foreach (var target in world.Damage.Characters.Values.OrderBy(c => c.EntityId).ToArray())
                {
                    yield return 0;
                    if (contacted.Contains(target.EntityId)) continue;
                    var grant = rule.Grant(new EffectRuleInput(binding.Origin, binding.ExpectedState), actor, target, cell, movement);
                    if (grant == null) continue;
                    contacted.Add(target.EntityId);
                    world.EffectTargetIds.Add(target.EntityId);
                    var statuses = AppliedStatusRules.Apply(target.StatusEffects, grant);
                    if (statuses.SameValues(target.StatusEffects)) continue;
                    var final = target.WithStatusEffects(statuses);
                    world.MergeCharacter(new CharacterStateChange(target, final));
                    CombatCommandEvents.Trait(world.Events, actor.EntityId, TraitKey.ReconSquad, TraitActivationKind.StatusApplied,
                        grant.IncreasePercent, new[] { target.EntityId }, target.OccupiedCells);
                    CombatCommandEvents.TraitState(world.Events, target, final);
                }
            }
        }
    }
}
