using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>보유 상태의 순수 변경만 계산한다. 명령 안의 공격 그룹과 보호 기록은 <see cref="CommandAttackGroups"/>가 소유한다.</summary>
    public static class HeldEnvironmentEffects
    {
        /// <summary>배지는 이미 사용 가능하면 추가 보호를 주지 않는다(G3).</summary>
        public static CharacterHeldEnvironmentState WithBadge(CharacterHeldEnvironmentState held, long sourceId)
        {
            if (held == null) throw new ArgumentNullException(nameof(held));
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            return held.BadgeAvailable ? held : new CharacterHeldEnvironmentState(sourceId, true, held.SuppliesAttackBonus, held.Flag);
        }

        /// <summary>깃발은 하나만 보유한다(G5). 이미 보유 중이면 그대로 둔다.</summary>
        public static CharacterHeldEnvironmentState WithFlag(CharacterHeldEnvironmentState held, HeldFlag flag)
        {
            if (held == null) throw new ArgumentNullException(nameof(held));
            if (flag == null) throw new ArgumentNullException(nameof(flag));
            return held.Flag != null ? held : new CharacterHeldEnvironmentState(held.BadgeSourceId, held.BadgeAvailable,
                held.SuppliesAttackBonus, flag);
        }

        public static CharacterCombatFacts AcquireFlag(CharacterCombatFacts character, FlagSourceFact flag)
        {
            if (flag == null) throw new ArgumentNullException(nameof(flag));
            return Apply(character, WithFlag(character.HeldEnvironment, HeldFlag.From(flag)));
        }

        public static CharacterCombatFacts AcquireBadge(CharacterCombatFacts character, long sourceId) =>
            Apply(character, WithBadge(character.HeldEnvironment, sourceId));

        /// <summary>스케줄링이 읽는 실효 Priority다. 깃발 기여분은 보유 상태에만 저장하고 여기서 더한다.</summary>
        public static int EffectivePriority(CharacterCombatFacts character) =>
            checked(character.Stats.Priority + (character.HeldEnvironment.Flag?.PriorityBonus ?? 0));

        public static CharacterCombatFacts ClearFlag(CharacterCombatFacts character)
        {
            var held = character.HeldEnvironment;
            return held.Flag == null ? character : WithHeldState(character,
                new CharacterHeldEnvironmentState(held.BadgeSourceId, held.BadgeAvailable, held.SuppliesAttackBonus));
        }

        /// <summary>배지는 한 공격의 모든 타격을 막는다. 첫 타격에서 사용을 소모하고 같은 그룹의 이후 타격은 기록으로 막는다.</summary>
        internal static int Protect(CommandAttackGroups groups, CharacterCombatFacts character, int damage, long group,
            out CharacterCombatFacts final)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));
            if (damage < 0 || group < 1) throw new ArgumentOutOfRangeException(nameof(damage));
            final = character;
            if (groups.IsProtected(character.EntityId, group)) return 0;
            if (!character.HeldEnvironment.BadgeAvailable) return damage;
            groups.MarkProtected(character.EntityId, group);
            var held = character.HeldEnvironment;
            final = WithHeldState(character, new CharacterHeldEnvironmentState(held.BadgeSourceId, false, held.SuppliesAttackBonus, held.Flag));
            return 0;
        }

        public static CharacterCombatFacts ExpireSupplies(CharacterCombatFacts character) =>
            character.HeldEnvironment.SuppliesAttackBonus == 0 ? character : WithHeldState(character,
                new CharacterHeldEnvironmentState(character.HeldEnvironment.BadgeSourceId, character.HeldEnvironment.BadgeAvailable,
                    flag: character.HeldEnvironment.Flag));

        public static CharacterCombatFacts GrantSupplies(CharacterCombatFacts character, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var held = character.HeldEnvironment;
            return amount == 0 ? character : WithHeldState(character,
                new CharacterHeldEnvironmentState(held.BadgeSourceId, held.BadgeAvailable,
                    checked(held.SuppliesAttackBonus + amount), held.Flag));
        }

        public static CharacterCombatFacts ClearForBattleEnd(CharacterCombatFacts character) =>
            character.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty) ? character :
                WithHeldState(character, CharacterHeldEnvironmentState.Empty);

        public static CharacterCombatFacts WithHeldState(CharacterCombatFacts c, CharacterHeldEnvironmentState held) =>
            new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area, c.Health, c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.MovementCheckpointAnchor,
                c.CommittedMoveDistance, c.PendingMoveActionCost, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier, held);

        // 변경이 없으면 같은 사실을 돌려주어 호출자가 참조 비교로 변경 여부를 판단하게 한다.
        private static CharacterCombatFacts Apply(CharacterCombatFacts character, CharacterHeldEnvironmentState held) =>
            ReferenceEquals(held, character.HeldEnvironment) ? character : WithHeldState(character, held);
    }
}
