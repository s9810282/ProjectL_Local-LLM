using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    /// <summary>변경 제안의 예상 상태를 값으로 비교한다.</summary>
    internal static class CharacterCombatFactsComparer
    {
        internal static bool SameIdentity(CharacterCombatFacts left, CharacterCombatFacts right)
        {
            return left.EntityId == right.EntityId &&
                   left.OwnedId == right.OwnedId &&
                   left.CharacterKey == right.CharacterKey &&
                   left.Owner == right.Owner &&
                   left.OwnershipRole == right.OwnershipRole;
        }

        internal static bool SameFacts(CharacterCombatFacts left, CharacterCombatFacts right)
        {
            if (SameIdentity(left, right) == false ||
                left.Area != right.Area ||
                left.Health.CurrentHealth != right.Health.CurrentHealth ||
                left.Health.MaxHealth != right.Health.MaxHealth ||
                SameStats(left.Stats, right.Stats) == false ||
                left.ReserveSlotIndex != right.ReserveSlotIndex ||
                left.Anchor != right.Anchor ||
                left.Rotation != right.Rotation ||
                left.MovementCheckpointAnchor != right.MovementCheckpointAnchor ||
                left.CommittedMoveDistance != right.CommittedMoveDistance ||
                left.PendingMoveActionCost != right.PendingMoveActionCost ||
                left.TemporaryAttackBonus != right.TemporaryAttackBonus ||
                left.TemporaryMaxHealthBonus != right.TemporaryMaxHealthBonus ||
                left.PendingMoveDistance != right.PendingMoveDistance ||
                left.AttackRefundEligibleMoveCost != right.AttackRefundEligibleMoveCost ||
                !left.PositionModifier.SameValues(right.PositionModifier) ||
                !left.HeldEnvironment.SameValues(right.HeldEnvironment) ||
                left.OccupiedCells.Count != right.OccupiedCells.Count)
                return false;

            for (int i = 0; i < left.OccupiedCells.Count; i++)
                if (left.OccupiedCells[i] != right.OccupiedCells[i])
                    return false;

            return true;
        }

        private static bool SameStats(UnitStatsSnapshot left, UnitStatsSnapshot right)
        {
            if (left.MoveMethod != right.MoveMethod ||
                left.MoveRange != right.MoveRange ||
                left.MoveCost != right.MoveCost ||
                left.AppointRange != right.AppointRange ||
                left.AttackPower != right.AttackPower ||
                left.SoulAttackBonus != right.SoulAttackBonus ||
                left.SoulMaxHealthBonus != right.SoulMaxHealthBonus ||
                left.ArtifactAttackBonus != right.ArtifactAttackBonus ||
                left.EffectiveAttackPower != right.EffectiveAttackPower ||
                left.PositionAttackBonus != right.PositionAttackBonus ||
                left.PhysicalDefense != right.PhysicalDefense ||
                left.MaxActionCost != right.MaxActionCost ||
                left.AttackCost != right.AttackCost ||
                left.RemainingMoveRange != right.RemainingMoveRange ||
                left.CurrentActionCost != right.CurrentActionCost ||
                left.Priority != right.Priority ||
                left.Delay != right.Delay ||
                SameCategory(left.Category, right.Category) == false ||
                left.TraitStatuses.Length != right.TraitStatuses.Length)
                return false;

            for (int i = 0; i < left.TraitStatuses.Length; i++)
                if (SameTraitStatus(left.TraitStatuses.Span[i], right.TraitStatuses.Span[i]) == false)
                    return false;

            return true;
        }

        private static bool SameCategory(CategorySnapshot left, CategorySnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;

            return left.Key == right.Key &&
                   left.RefundUsedThisTurn == right.RefundUsedThisTurn &&
                   left.DefenseFacing == right.DefenseFacing &&
                   left.MovedThisTurn == right.MovedThisTurn &&
                   left.DamageReductionPercent == right.DamageReductionPercent;
        }

        private static bool SameTraitStatus(TraitStatusSnapshot left, TraitStatusSnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Key != right.Key)
                return false;

            // 특성별 값 비교는 각 스냅샷이 소유한다. 새 특성을 추가해도 이 비교기는 바뀌지 않는다.
            return left.SameValues(right);
        }
    }
}
