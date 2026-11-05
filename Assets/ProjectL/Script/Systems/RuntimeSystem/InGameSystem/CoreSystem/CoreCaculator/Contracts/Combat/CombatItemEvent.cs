using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum ItemActivationKind
    {
        Unspecified,
        DamageMitigation,
        Healing,
        TemporaryEffect
    }

    public enum ItemEventStage
    {
        BeforeDamage,
        AfterDamage,
        AttackCompleted,
        Equipment,
        Lifecycle
    }

    public enum ItemEventReason
    {
        Activated,
        DuplicateEffect,
        Equipped,
        Unequipped,
        Replaced,
        ReturnedToHand,
        Death,
        RoundEnded,
        BattleEnded
    }

    /// <summary>계산을 끝낸 아이템 사실입니다. 체력 피해와 회복 사건은 기존 체력 사건으로 각각 기록합니다.</summary>
    public sealed class CombatItemEvent : CombatEvent
    {
        public ItemActivationKind ActivationKind { get; }
        public int ItemKey { get; }
        public EntityId HolderId { get; }
        public EntityId? TargetId { get; }
        public ItemEventStage Stage { get; }
        public ItemEventReason Reason { get; }
        public int AppliedMagnitude { get; }
        public int? OldItemKey { get; }
        public int? NewItemKey { get; }
        public CombatDamageKind? DamageKind { get; }
        public bool Activated => Reason == ItemEventReason.Activated || Reason == ItemEventReason.DuplicateEffect;
        public bool Consumed => Activated;

        public CombatItemEvent(int itemKey, EntityId holderId, EntityId? targetId, ItemEventStage stage,
            ItemEventReason reason, int appliedMagnitude, int? oldItemKey, int? newItemKey, CombatDamageKind? damageKind = null,
            ItemActivationKind activationKind = ItemActivationKind.Unspecified)
            : base(EventKind(stage))
        {
            if (!Enum.IsDefined(typeof(ItemActivationKind), activationKind)) throw new ArgumentOutOfRangeException(nameof(activationKind));
            if (stage is ItemEventStage.Equipment or ItemEventStage.Lifecycle && activationKind != ItemActivationKind.Unspecified)
                throw new ArgumentException("장착·폐기에는 발동 효과 종류가 없습니다.", nameof(activationKind));
            ActivationKind = activationKind;
            if (itemKey <= 0) throw new ArgumentOutOfRangeException(nameof(itemKey));
            if (holderId.IsEmpty) throw new ArgumentException("소지자 ID가 필요합니다.", nameof(holderId));
            if (targetId.HasValue && targetId.Value.IsEmpty)
                throw new ArgumentException("대상 ID는 유효한 값 또는 null이어야 합니다.", nameof(targetId));
            if (!Enum.IsDefined(typeof(ItemEventReason), reason)) throw new ArgumentOutOfRangeException(nameof(reason));
            if (appliedMagnitude < 0) throw new ArgumentOutOfRangeException(nameof(appliedMagnitude));
            if (oldItemKey <= 0) throw new ArgumentOutOfRangeException(nameof(oldItemKey));
            if (newItemKey <= 0) throw new ArgumentOutOfRangeException(nameof(newItemKey));
            if (damageKind.HasValue && !Enum.IsDefined(typeof(CombatDamageKind), damageKind.Value))
                throw new ArgumentOutOfRangeException(nameof(damageKind));

            if (stage == ItemEventStage.Lifecycle)
            {
                if ((reason != ItemEventReason.Death && reason != ItemEventReason.RoundEnded && reason != ItemEventReason.BattleEnded) ||
                    oldItemKey != itemKey || newItemKey.HasValue || targetId.HasValue || appliedMagnitude != 0 || damageKind.HasValue)
                    throw new ArgumentException("폐기 사건은 기존 슬롯만 비우며 발동 효과를 포함하지 않습니다.");
            }
            else if (stage == ItemEventStage.Equipment)
            {
                bool validSlot = reason switch
                {
                    ItemEventReason.Equipped => !oldItemKey.HasValue && newItemKey == itemKey,
                    ItemEventReason.Unequipped or ItemEventReason.ReturnedToHand => oldItemKey == itemKey && !newItemKey.HasValue,
                    ItemEventReason.Replaced => oldItemKey.HasValue && newItemKey == itemKey && oldItemKey != newItemKey,
                    _ => false
                };
                if (!validSlot || targetId.HasValue || appliedMagnitude != 0 || damageKind.HasValue)
                    throw new ArgumentException("장착 사건의 사유와 이전·이후 슬롯이 일치해야 합니다.");
            }
            else
            {
                if ((reason != ItemEventReason.Activated && reason != ItemEventReason.DuplicateEffect) ||
                    !targetId.HasValue || oldItemKey != itemKey || newItemKey.HasValue ||
                    (stage == ItemEventStage.AttackCompleted ? damageKind.HasValue : !damageKind.HasValue))
                    throw new ArgumentException("발동 사건에는 대상·소모된 슬롯과 해당 단계의 피해 원인이 필요합니다.");
                if (reason == ItemEventReason.DuplicateEffect && (appliedMagnitude != 0 || stage == ItemEventStage.BeforeDamage))
                    throw new ArgumentException("중복 효과는 수치를 적용하지 않고 소모만 기록합니다.");
            }

            ItemKey = itemKey;
            HolderId = holderId;
            TargetId = targetId;
            Stage = stage;
            Reason = reason;
            AppliedMagnitude = appliedMagnitude;
            OldItemKey = oldItemKey;
            NewItemKey = newItemKey;
            DamageKind = damageKind;
        }

        private static CombatEventKind EventKind(ItemEventStage stage)
        {
            return stage switch
            {
                ItemEventStage.BeforeDamage or ItemEventStage.AfterDamage or ItemEventStage.AttackCompleted => CombatEventKind.ItemActivated,
                ItemEventStage.Equipment => CombatEventKind.ItemEquipmentChanged,
                ItemEventStage.Lifecycle => CombatEventKind.ItemDiscarded,
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
        }
    }
}
