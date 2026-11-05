using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 포효 피격 집계의 원인 하나다. 같은 원인 안에서 한 대상은 한 번만 센다(D7).
    /// 공격 명령은 공격과 그 파생 피해 전체가 원인 하나를 공유하고, 독립 환경 발동은 발동마다 새 원인을 만든다.
    /// 방어 적용 그룹(<see cref="CommandAttackGroups"/>)과는 별개이며 명령 계산이 끝나면 버린다.
    /// </summary>
    internal sealed class CombatHitCause
    {
        private readonly HashSet<EntityId> _counted = new();

        /// <summary>이 원인에서 처음 맞은 대상이면 true다.</summary>
        internal bool TryCount(EntityId target) => _counted.Add(target);
    }
}
