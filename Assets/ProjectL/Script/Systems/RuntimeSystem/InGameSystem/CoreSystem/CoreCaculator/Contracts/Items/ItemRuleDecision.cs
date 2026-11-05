using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    /// <summary>한 단계의 순수 결정입니다. 0 회복도 유효한 발동이며 실제 적용량은 적용 경계가 기록합니다.</summary>
    public sealed class ItemRuleDecision
    {
        public bool Activated { get; }
        public bool Consume { get; }
        public EntityId? TargetId { get; }
        public int? RemainingDamage { get; }
        public int HealAmount { get; }
        public ItemTemporaryEffect TemporaryEffect { get; }
        public uint? FinalRandomState { get; }

        public ItemRuleDecision(
            bool activated,
            bool consume,
            EntityId? targetId = null,
            int? remainingDamage = null,
            int healAmount = 0,
            ItemTemporaryEffect temporaryEffect = null,
            uint? finalRandomState = null)
        {
            if (remainingDamage < 0) throw new ArgumentOutOfRangeException(nameof(remainingDamage));
            if (healAmount < 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
            if (targetId.HasValue && targetId.Value.IsEmpty)
                throw new ArgumentException("대상 ID는 유효한 값 또는 null이어야 합니다.", nameof(targetId));
            if (!activated)
            {
                if (consume || targetId.HasValue || remainingDamage.HasValue || healAmount != 0 || temporaryEffect != null)
                    throw new ArgumentException("미발동 결정에는 소모 또는 효과가 있을 수 없습니다.");
            }
            else
            {
                if (!consume || !targetId.HasValue)
                    throw new ArgumentException("발동 결정에는 소모와 대상이 필요합니다.");
                if ((remainingDamage.HasValue && (healAmount != 0 || temporaryEffect != null)) ||
                    (temporaryEffect != null && healAmount != 0))
                    throw new ArgumentException("한 결정에는 한 종류의 효과만 지정할 수 있습니다.");
                if (finalRandomState.HasValue && temporaryEffect == null)
                    throw new ArgumentException("피해 감소 또는 회복 결정은 난수 상태를 변경할 수 없습니다.");
            }
            Activated = activated;
            Consume = consume;
            TargetId = targetId;
            RemainingDamage = remainingDamage;
            HealAmount = healAmount;
            TemporaryEffect = temporaryEffect;
            FinalRandomState = finalRandomState;
        }

        public static ItemRuleDecision NoActivation(uint? randomState = null) =>
            new ItemRuleDecision(false, false, finalRandomState: randomState);

        public static ItemRuleDecision Mitigation(EntityId targetId, int remainingDamage) =>
            new ItemRuleDecision(true, true, targetId, remainingDamage);

        public static ItemRuleDecision Heal(EntityId targetId, int healAmount) =>
            new ItemRuleDecision(true, true, targetId, healAmount: healAmount);

        public static ItemRuleDecision ApplyEffect(EntityId targetId, ItemTemporaryEffect effect, uint? randomState = null) =>
            new ItemRuleDecision(true, true, targetId,
                temporaryEffect: effect ?? throw new ArgumentNullException(nameof(effect)), finalRandomState: randomState);
    }
}
