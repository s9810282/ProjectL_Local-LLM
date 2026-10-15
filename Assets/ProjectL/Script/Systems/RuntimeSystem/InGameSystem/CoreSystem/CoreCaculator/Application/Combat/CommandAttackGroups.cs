using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 안의 공격 발생 단위(그룹) 번호와 보호가 적용된 대상·그룹 쌍만 기록한다.
    /// 명령 계산이 끝나면 버리며 Buffer나 턴 단위 보호 상태로 남기지 않는다.</summary>
    internal sealed class CommandAttackGroups
    {
        /// <summary>명령의 직접 공격은 항상 이 그룹을 쓴다. 환경 피해는 <see cref="Next"/>로 새 그룹을 받는다.</summary>
        internal const long Direct = 1;
        private readonly HashSet<(EntityId Target, long Group)> _protected = new();
        private long _last = Direct;

        internal long Next() => checked(++_last);
        internal bool IsProtected(EntityId target, long group) => _protected.Contains((target, group));
        internal void MarkProtected(EntityId target, long group) => _protected.Add((target, group));
    }
}
