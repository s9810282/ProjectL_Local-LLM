using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs
{
    internal readonly struct ThornBombAttachmentCandidate
    {
        internal EntityId SourceId { get; }
        internal Owner SourceOwner { get; }
        internal EntityId TargetId { get; }
        internal CellCoord AttachmentLocalCell { get; }
        internal int DamagePercent { get; }

        internal ThornBombAttachmentCandidate(EntityId sourceId, Owner sourceOwner, EntityId targetId,
            CellCoord attachmentLocalCell, int damagePercent)
        {
            SourceId = sourceId;
            SourceOwner = sourceOwner;
            TargetId = targetId;
            AttachmentLocalCell = attachmentLocalCell;
            DamagePercent = damagePercent;
        }
    }

    internal static class ThornBombAttackApplication
    {
        internal static ThornBombAttachmentCandidate? CaptureAttachment(CommandAttackPlan plan, CommandAttackProjection world)
        {
            return CombatDamageKinds.IsAttack(plan.DamageKind)
                ? CaptureAttachment(plan.Input, plan.Actor, plan.Bindings, world.Characters.Values, plan.Selection.ReachableCells.PrimaryCell)
                : null;
        }

        internal static ThornBombAttachmentCandidate? CaptureAttachment(CombatCommandInput input, CharacterCombatFacts source,
            IReadOnlyList<EffectRuleBinding> bindings, IEnumerable<CharacterCombatFacts> characters, CellCoord? center)
        {
            if (!center.HasValue) return null;
            var binding = bindings.SingleOrDefault(b => b.Rule is IEffectThornBombRule);
            if (binding == null) return null;
            var target = characters.FirstOrDefault(c => c.EntityId != source.EntityId && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                c.OccupiedCells.Contains(center.Value));
            if (target == null) return null;
            var rule = (IEffectThornBombRule)binding.Rule;
            return new ThornBombAttachmentCandidate(source.EntityId, source.Owner, target.EntityId,
                ThornBombResolution.CaptureLocalCell(input, target, center.Value),
                rule.GetDamagePercent(new EffectRuleInput(binding.Origin, binding.ExpectedState)));
        }

        internal static IEnumerable<string> CompleteAttachmentSteps(CommandAttackPlan plan, MovementProjection projection,
            ThornBombAttachmentCandidate? candidate, ThornBombResolution bombs, IReadOnlyList<IMovementTileRule> rules)
            => CompleteAttachmentSteps(plan.AttackGroup, projection, candidate, bombs, rules);

        internal static IEnumerable<string> CompleteAttachmentSteps(long attackGroup, MovementProjection projection,
            ThornBombAttachmentCandidate? candidate, ThornBombResolution bombs, IReadOnlyList<IMovementTileRule> rules)
        {
            foreach (var phase in bombs.ResolveSteps(projection, rules)) yield return phase;
            if (!candidate.HasValue) yield break;
            var attachment = candidate.Value;
            if (projection.Terminal || !bombs.WasHit(attackGroup, attachment.TargetId) ||
                projection.Character(attachment.TargetId).Health.IsDead) yield break;
            // 희생으로 중심 대상의 구폭탄이 남아 있어도 덮어쓰지 않는다.
            if (bombs.TrySchedule(attachment.TargetId, ThornBombTrigger.Replacement))
                foreach (var phase in bombs.ResolveSteps(projection, rules)) yield return phase;
            if (projection.Terminal || projection.Character(attachment.TargetId).Health.IsDead) yield break;
            // 출처가 이 명령에서 죽었으면 사망 직전의 마지막 유효 공격력을 쓴다(설계 P04).
            var source = projection.Character(attachment.SourceId);
            if (source.Health.IsDead) source = ThornBombResolution.PreDeathFact(projection, source.EntityId) ?? source;
            var bomb = new ThornBombSnapshot(bombs.NextBombId(), attachment.SourceId, attachment.SourceOwner, attachment.TargetId,
                attachment.AttachmentLocalCell, CombatEffectiveStatsCalculator.AttackPower(source), attachment.DamagePercent,
                ThornBombRules.CalculateDueSequence(projection.Turn.ControllerTurnSequence));
            bombs.Attach(bomb);
            projection.Events.Add(new CombatThornBombAttachedEvent(bomb));
            yield return "ThornBombAttached";
        }
    }
}
