using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public sealed class RopeItemRule : IItemAttackCompletedRule
    {
        private static readonly HostilityPolicy Hostility = new HostilityPolicy();

        public ItemRuleDecision Evaluate(ItemDefinition definition, ItemAttackContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(definition.Effect is RopeItemSpec spec))
                throw new InvalidOperationException("밧줄 규칙과 효과 명세가 일치해야 합니다.");
            var holder = context.Holder;
            if (holder.Health.IsDead || holder.Items.EquippedItemKey != definition.Key)
                return ItemRuleDecision.NoActivation(context.RandomState);

            var livingEnemies = context.FinalCharacters.Where(c => !c.Health.IsDead &&
                Hostility.AreHostile(holder.Owner, c.Owner)).Select(c => c.EntityId).ToHashSet();
            var candidates = context.DirectHits.Where(c => livingEnemies.Contains(c.TargetId))
                .GroupBy(c => c.TargetId).Select(g => g.First())
                .Select(c => (Candidate: c, Distance: DistanceSquared(context.AttackerAnchorAtDelivery, c.AnchorAtDelivery)))
                .OrderBy(c => c.Distance).ThenBy(c => c.Candidate.TargetId).ToArray();
            if (candidates.Length == 0) return ItemRuleDecision.NoActivation(context.RandomState);
            int tied = candidates.TakeWhile(c => c.Distance == candidates[0].Distance).Count();
            int selected = ItemTargetRandom.NextIndex(context.RandomState, tied, out uint finalState);
            return ItemRuleDecision.ApplyEffect(candidates[selected].Candidate.TargetId,
                new ItemTemporaryEffect(definition.Key, ItemStatKind.Priority, spec.PriorityPenalty, context.PreparationIndex), finalState);
        }

        private static decimal DistanceSquared(CellCoord from, CellCoord to)
        {
            // 전체 int 좌표 범위의 차이·제곱·합도 오버플로와 반올림 없이 비교한다.
            decimal dx = (decimal)to.X - from.X;
            decimal dy = (decimal)to.Y - from.Y;
            return dx * dx + dy * dy;
        }
    }
}
