using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>가시 심장을 발동시킨 아군과 원 공격·반사 피해의 그룹을 보관합니다.</summary>
    public sealed class ThornHeartReflectionContext
    {
        public EntityId StruckAllyId { get; }
        public long TriggeringAttackGroup { get; }
        public long ReflectionAttackGroup { get; }

        public ThornHeartReflectionContext(EntityId struckAllyId, long triggeringAttackGroup, long reflectionAttackGroup)
        {
            if (struckAllyId.IsEmpty)
                throw new ArgumentException("피격 아군의 식별자가 필요합니다.", nameof(struckAllyId));
            if (triggeringAttackGroup <= 0)
                throw new ArgumentOutOfRangeException(nameof(triggeringAttackGroup));
            if (reflectionAttackGroup <= 0 || reflectionAttackGroup == triggeringAttackGroup)
                throw new ArgumentOutOfRangeException(nameof(reflectionAttackGroup));

            StruckAllyId = struckAllyId;
            TriggeringAttackGroup = triggeringAttackGroup;
            ReflectionAttackGroup = reflectionAttackGroup;
        }
    }
}
