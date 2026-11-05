using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class AttackHitContactSnapshot
    {
        public int DeliveryIndex { get; }
        public EntityId OriginalTargetId { get; }
        public AttackTargetRole Role { get; }
        public int AppliedSplashPercent { get; }
        public CombatDamageOrigin Origin { get; }
        public AttackHitContactSnapshot(int deliveryIndex, EntityId originalTargetId, AttackTargetRole role, int appliedSplashPercent, CombatDamageOrigin origin)
        {
            if (deliveryIndex < 0) throw new ArgumentOutOfRangeException(nameof(deliveryIndex));
            if (originalTargetId.IsEmpty) throw new ArgumentException(nameof(originalTargetId));
            if (role == AttackTargetRole.None || !Enum.IsDefined(typeof(AttackTargetRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
            if (appliedSplashPercent < 0 || role != AttackTargetRole.Additional && appliedSplashPercent != 100) throw new ArgumentOutOfRangeException(nameof(appliedSplashPercent));
            if (origin == null || !origin.CreditedCharacterId.HasValue || origin.CreditedCharacterId.Value.IsEmpty || origin.AttackGroup <= 0)
                throw new ArgumentException("접촉에는 공격자와 양수 공격 그룹이 필요합니다.", nameof(origin));
            DeliveryIndex = deliveryIndex; OriginalTargetId = originalTargetId; Role = role; AppliedSplashPercent = appliedSplashPercent; Origin = origin;
        }
        internal static IReadOnlyList<AttackHitContactSnapshot> Copy(IReadOnlyList<AttackHitContactSnapshot> contacts)
        {
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            var copy = contacts.ToArray();
            if (copy.Any(c => c == null) || copy.Select(c => c.DeliveryIndex).Distinct().Count() != copy.Length)
                throw new ArgumentException("접촉 순번은 중복될 수 없습니다.", nameof(contacts));
            return Array.AsReadOnly(copy);
        }
    }
}
