using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>피해의 물리적 발생 위치와 가해 캐릭터를 분리한다. 원본 소멸 뒤에도 유지되는 불변 값이다.</summary>
    public sealed class CombatDamageOrigin
    {
        public CellCoord? PhysicalCell { get; }
        public EntityId? CreditedCharacterId { get; }
        public long AttackGroup { get; }

        public CombatDamageOrigin(CellCoord? physicalCell, EntityId? creditedCharacterId, long attackGroup = 0)
        {
            if (creditedCharacterId.HasValue && creditedCharacterId.Value.IsEmpty)
                throw new ArgumentException("가해 캐릭터는 유효한 ID 또는 null이어야 합니다.", nameof(creditedCharacterId));
            PhysicalCell = physicalCell;
            CreditedCharacterId = creditedCharacterId;
            if (attackGroup < 0) throw new ArgumentOutOfRangeException(nameof(attackGroup));
            AttackGroup = attackGroup;
        }
    }
}
