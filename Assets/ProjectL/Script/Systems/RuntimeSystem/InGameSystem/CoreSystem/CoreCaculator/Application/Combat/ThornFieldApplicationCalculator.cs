using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 가시밭 특성의 생성과 즉시 피해를 처리한다(S9). 선택한 칸을 중심으로 벽 파급을 거친 칸에만 적용하며,
    /// 가시 피해를 받은 대상은 Additional 역할로 공격 결과에 남는다.
    /// </summary>
    internal static class ThornFieldApplicationCalculator
    {
        internal static void ApplyContactStatuses(MovementProjection arrival, EntityId target, IReadOnlyList<ThornFieldCellSnapshot> fields)
        {
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
        }

        internal static void Apply(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger)
        {
            // 유닛 턴 수명의 가시 장판 생산은 재설계 전까지 비활성이다.
            if (plan == null) throw new ArgumentNullException(nameof(plan));
        }
    }
}
